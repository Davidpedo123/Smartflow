using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
using Smartflow.Business.Validation;
using Smartflow.Infrastructure.Config;
using System.Diagnostics;
using Smartflow.Domain.Enums;
using Smartflow.Business.Parallelization;
using System.Collections.Concurrent;

namespace Smartflow.Business.ETL;

public class ETLCoordinator
{
    private readonly IDataExtractor _extractor;
    private readonly IDataLoader _loader;
    private readonly RuleValidator _validator;
    private readonly Configuration _config;
    private ParallelizationEngine? _parallelEngine;

    public ETLCoordinator(
        IDataExtractor extractor,
        IDataLoader loader,
        RuleValidator validator,
        Configuration config)
    {
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Procesa TODOS los archivos y mide el tiempo total del proceso ETL completo (SECUENCIAL)
    /// </summary>
    public void ProcessAllData(List<string> inputPaths)
    {
        if (inputPaths == null || inputPaths.Count == 0)
            throw new ArgumentException("La lista de archivos no puede estar vacía.", nameof(inputPaths));

        int totalRecords = 0;
        int totalAlerts = 0;
        int totalZones = 0;

        try
        {
            Console.WriteLine("=== Iniciando Proceso ETL Completo (Secuencial) ===");
            Console.WriteLine($"[ETL] Archivos a procesar: {inputPaths.Count}");
            Console.WriteLine($"[ETL] Fecha y hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(new string('=', 60));

            foreach (var inputPath in inputPaths)
            {
                Console.WriteLine($"\n[ETL] Procesando: {Path.GetFileName(inputPath)}");

                var result = ProcessSingleFile(inputPath);

                totalRecords += result.RecordCount;
                totalAlerts += result.AlertCount;
                totalZones += result.ZoneCount;
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"[ETL] ✓ Proceso ETL COMPLETO finalizado exitosamente");
            Console.WriteLine($"[ETL] Archivos procesados: {inputPaths.Count}");
            Console.WriteLine($"[ETL] Registros totales: {totalRecords}");
            Console.WriteLine($"[ETL] Alertas totales: {totalAlerts}");
            Console.WriteLine($"[ETL] Zonas totales: {totalZones}");
            Console.WriteLine(new string('=', 60));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ETL] ERROR: {ex.Message}");
            Console.WriteLine($"[ETL] Traza de error: {ex.StackTrace}");
            throw new InvalidOperationException("El proceso ETL completo falló", ex);
        }
    }

    /// <summary>
    /// NUEVA VERSIÓN MEJORADA: Procesa archivos en paralelo usando Streaming (IEnumerable)
    /// Esto reduce drásticamente el uso de memoria y mejora la eficiencia.
    /// </summary>
    public void ProcessAllDataParallel(List<string> inputPaths)
    {
        if (inputPaths == null || !inputPaths.Any()) return;

        Console.WriteLine("=== ETL PARALELO OPTIMIZADO (STREAMING) ===");
        
        var allProcessedData = new ConcurrentBag<ProcessedData>();

        // PARALELISMO: Procesamos varios archivos a la vez (Descomposición de Datos a nivel de archivo)
        Parallel.ForEach(inputPaths, 
            new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads }, 
            inputPath =>
        {
            try
            {
                // 1. EXTRACT (STREAMING): Obtenemos el flujo, no una lista gigante
                var dataStream = _extractor.ExtractStream(inputPath);

                // 2. TRANSFORM (EN FLUJO):
                // Procesamos dato a dato sin crear listas intermedias
                var validData = new List<SensorData>();
                var localAlerts = new List<Alert>();
                var seenIds = new HashSet<string>();

                foreach (var sensor in dataStream)
                {
                    // Limpieza al vuelo
                    if (sensor == null || !sensor.IsValid()) continue;
                    
                    string key = $"{sensor.SensorId}_{sensor.Timestamp.Ticks}";
                    if (seenIds.Contains(key)) continue;
                    seenIds.Add(key);

                    sensor.Value = NormalizeValue(sensor.Value);
                    var alerts = _validator.ValidateData(sensor);
                    if (alerts.Any()) localAlerts.AddRange(alerts);

                    validData.Add(sensor); // Solo guardamos en memoria los válidos ya limpios
                }

                // Agrupación y Estadísticas (Esto necesita los datos limpios en memoria para agrupar)
                var groupedByZone = GroupByZone(validData);
                var stats = CalculateStatisticsForZones(groupedByZone, localAlerts);

                foreach (var stat in stats) allProcessedData.Add(stat);
                
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Archivo procesado: {Path.GetFileName(inputPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en {inputPath}: {ex.Message}");
            }
        });

        // 3. LOAD: Guardamos todo junto al final
        var outputPath = GenerateOutputPath(_config.OutputPath, "resultado_final_paralelo");
        _loader.LoadBatch(allProcessedData.ToList(), outputPath);
        
        Console.WriteLine($"\n[ETL] Datos cargados en: {outputPath}");
        Console.WriteLine(new string('=', 60));
    }

    /// <summary>
    /// Procesa un único archivo (Modo Secuencial)
    /// </summary>
    private (int RecordCount, int AlertCount, int ZoneCount) ProcessSingleFile(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("El input path no puede ser nulo o vacío.", nameof(inputPath));

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input path no encontrado: {inputPath}");

        try
        {
            // 1. EXTRACT
            var rawData = ExtractData(inputPath);
            Console.WriteLine($"[ETL]   Extraídos {rawData.Count} registros");

            if (rawData.Count == 0)
            {
                Console.WriteLine("[ETL]   No hay datos para procesar.");
                return (0, 0, 0);
            }

            // 2. TRANSFORM
          
            var cleanedData = CleanDataStream(rawData).ToList(); 
            Console.WriteLine($"[ETL]   Limpiados: {cleanedData.Count}/{rawData.Count} registros válidos");

            // Ahora cleanedData es una List<SensorData>, compatible con ValidateData(List<...>)
            var allAlerts = _validator.ValidateData(cleanedData); 
            Console.WriteLine($"[ETL]   Generadas {allAlerts.Count} alertas");

            var groupedByZone = GroupByZone(cleanedData);
            Console.WriteLine($"[ETL]   Creadas {groupedByZone.Count} zonas geográficas");

            var processedDataList = CalculateStatisticsForZones(groupedByZone, allAlerts);

            // 3. LOAD
            var outputPath = GenerateOutputPath(_config.OutputPath, inputPath);

            if (processedDataList.Count == 1)
            {
                _loader.Load(processedDataList[0], outputPath);
            }
            else
            {
                _loader.LoadBatch(processedDataList, outputPath);
            }

            Console.WriteLine($"[ETL]   Datos cargados en: {outputPath}");

            return (cleanedData.Count, allAlerts.Count, processedDataList.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ETL] Error procesando {inputPath}: {ex.Message}");
            throw;
        }
    }

    private List<SensorData> ExtractData(string inputPath)
    {
        try
        {
            return _extractor.Extract(inputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ETL] Fallo en la extracción: {ex.Message}");
            throw;
        }
    }

    // Este método usa Streaming para filtrar duplicados y limpiar datos sin cargar todo en memoria extra
    private IEnumerable<SensorData> CleanDataStream(IEnumerable<SensorData> data)
    {
        var seenIds = new HashSet<string>(); 
        
        foreach (var sensor in data)
        {
            if (sensor != null && sensor.IsValid())
            {
                sensor.Value = NormalizeValue(sensor.Value);

                // Lógica de duplicados
                string uniqueKey = $"{sensor.SensorId}_{sensor.Timestamp:yyyyMMddHHmmss}";
                
                if (seenIds.Contains(uniqueKey))
                {
                    continue; // Es duplicado, lo saltamos
                }

                seenIds.Add(uniqueKey);
                
                // Entregamos el dato limpio al siguiente paso inmediatamente
                yield return sensor; 
            }
        }
    }

    // Mantenemos CleanData original por si alguna prueba lo llama, pero redirige al Stream
    private List<SensorData> CleanData(List<SensorData> data)
    {
        return CleanDataStream(data).ToList();
    }

    private double NormalizeValue(double value)
    {
        const double MAX_VALUE = 10_000;
        const double MIN_VALUE = -100;

        if (value > MAX_VALUE) return MAX_VALUE;
        if (value < MIN_VALUE) return MIN_VALUE;

        return value;
    }

    private List<ProcessedData> CalculateStatisticsForZones(
        Dictionary<string, List<SensorData>> groupedData,
        List<Alert> allAlerts)
    {
        var result = new List<ProcessedData>();

        foreach (var kvp in groupedData)
        {
            string zoneName = kvp.Key;
            var zoneData = kvp.Value;

            var processedData = CalculateStatistics(zoneData, zoneName);

            var zoneSensorIds = new HashSet<string>(zoneData.Select(s => s.SensorId));
            processedData.Alerts = allAlerts.Where(a => zoneSensorIds.Contains(a.SensorId)).ToList();

            result.Add(processedData);
        }

        return result;
    }

    private ProcessedData CalculateStatistics(List<SensorData> data, string zone)
    {
        if (data == null || data.Count == 0)
        {
            return new ProcessedData
            {
                Zone = zone,
                ProcessedAt = DateTime.Now,
                RecordCount = 0
            };
        }

        var values = data.Select(d => d.Value).ToList();

        double avg = values.Average();
        double max = values.Max();
        double min = values.Min();

        var statistics = new Dictionary<string, double>{
            {"avg", Math.Round(avg, 2)},
            {"max", Math.Round(max, 2)},
            {"min", Math.Round(min, 2)},
            {"count", data.Count}
        };

        var groupedByType = data.GroupBy(d => d.Type);

        foreach (var group in groupedByType)
        {
            var typeValues = group.Select(g => g.Value).ToList();
            statistics[$"{group.Key}_avg"] = Math.Round(typeValues.Average(), 2);
            statistics[$"{group.Key}_max"] = Math.Round(typeValues.Max(), 2);
            statistics[$"{group.Key}_min"] = Math.Round(typeValues.Min(), 2);
            statistics[$"{group.Key}_count"] = typeValues.Count;
        }

        return new ProcessedData
        {
            Zone = zone,
            ProcessedAt = DateTime.Now,
            Statistics = statistics,
            RecordCount = data.Count,
            Alerts = new List<Alert>()
        };
    }

    private Dictionary<string, List<SensorData>> GroupByZone(List<SensorData> data)
    {
        if (data == null || data.Count == 0)
            return new Dictionary<string, List<SensorData>>();

        var zones = new Dictionary<string, List<SensorData>>();
        const double GRID_SIZE = 0.40;

        foreach (var sensor in data)
        {
            double roundedLat = Math.Round(sensor.Latitude, 2);
            double roundedLon = Math.Round(sensor.Longitude, 2);

            int latGrid = (int)Math.Floor(roundedLat / GRID_SIZE);
            int lonGrid = (int)Math.Floor(roundedLon / GRID_SIZE);

            string zoneKey = $"Zone_{latGrid}_{lonGrid}";

            if (!zones.ContainsKey(zoneKey))
            {
                zones[zoneKey] = new List<SensorData>();
            }

            zones[zoneKey].Add(sensor);
        }

        return zones;
    }

    private string GenerateOutputPath(string outputDirectory, string inputPath)
    {
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        string inputFileName = Path.GetFileNameWithoutExtension(inputPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFileName = $"{inputFileName}_processed_{timestamp}.json";

        return Path.Combine(outputDirectory, outputFileName);
    }
}
