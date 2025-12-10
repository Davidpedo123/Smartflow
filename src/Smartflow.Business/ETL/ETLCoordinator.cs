using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
using Smartflow.Business.Validation;
using Smartflow.Infrastructure.Config;
using System.Diagnostics;
using Smartflow.Domain.Enums;
using Smartflow.Business.Parallelization;
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
  /// Procesa TODOS los archivos y mide el tiempo total del proceso ETL completo
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

  public void ProcessAllDataParallel(List<string> inputPaths)
  {
    if (inputPaths == null || inputPaths.Count == 0)
      throw new ArgumentException("La lista de archivos no puede estar vacía.", nameof(inputPaths));

    try
    {
      Console.WriteLine("=== Iniciando Proceso ETL (Paralelo) ===");
      Console.WriteLine($"[ETL] Archivos a procesar: {inputPaths.Count}");
      Console.WriteLine($"[ETL] Estrategia: {_config.Strategy}");
      Console.WriteLine($"[ETL] Max Threads: {_config.MaxThreads}");
      Console.WriteLine($"[ETL] Fecha y hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
      Console.WriteLine(new string('=', 60));

      // 1. EXTRACT 
      Console.WriteLine("\n[ETL] FASE 1: EXTRACCIÓN");
      var allData = new List<SensorData>();

      foreach (var inputPath in inputPaths)
      {
        Console.WriteLine($"[ETL] Extrayendo: {Path.GetFileName(inputPath)}");
        var fileData = _extractor.Extract(inputPath);
        allData.AddRange(fileData);
      }

      Console.WriteLine($"[ETL] Total registros extraídos: {allData.Count}");
      if (allData.Count == 0)
      {
        Console.WriteLine("[ETL] No hay datos para procesar.");
        return;
      }


      Console.WriteLine("\n[ETL] FASE 2: TRANSFORMACIÓN PARALELA");

      _parallelEngine = new ParallelizationEngine(_config, _validator);

      // FIX: Corregir nombre del método
      var processedDataList = _parallelEngine.ProcessInParallel(allData);

      Console.WriteLine($"[ETL] Zonas procesadas: {processedDataList.Count}");
      Console.WriteLine($"[ETL] Alertas totales: {processedDataList.Sum(p => p.Alerts.Count)}");

      Console.WriteLine("\n[ETL] FASE 3: CARGA");
      var outputPath = GenerateOutputPath(_config.OutputPath, "parallel_result");

      if (processedDataList.Count == 1)
      {
        _loader.Load(processedDataList[0], outputPath);
      }
      else
      {
        _loader.LoadBatch(processedDataList, outputPath);
      }

      Console.WriteLine($"[ETL] Datos cargados en: {outputPath}");

      Console.WriteLine("\n[ETL] ✓ Proceso paralelo completado");
      Console.WriteLine($"[ETL] Archivos procesados: {inputPaths.Count}");
      Console.WriteLine($"[ETL] Registros procesados: {allData.Count}");
      Console.WriteLine($"[ETL] Zonas generadas: {processedDataList.Count}");
      Console.WriteLine(new string('=', 60));
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\n[ETL] Error en proceso paralelo: {ex.Message}");
      Console.WriteLine($"[ETL] Traza: {ex.StackTrace}");
      throw;
    }
  }


  /// <summary>
  /// Procesa un único archivo (método auxiliar privado)
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
      var cleanedData = CleanData(rawData);
      Console.WriteLine($"[ETL]   Limpiados: {cleanedData.Count}/{rawData.Count} registros válidos");

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

  // Limpiar datos
  private List<SensorData> CleanData(List<SensorData> data)
  {
    if (data == null || data.Count == 0)
      return new List<SensorData>();

    var cleaned = new List<SensorData>();
    var seenIds = new HashSet<string>();

    int duplicates = 0;
    int invalid = 0;

    foreach (var sensor in data)
    {
      if (sensor == null)
      {
        invalid++;
        continue;
      }

      sensor.SensorId = sensor.SensorId?.Trim() ?? string.Empty;

      string uniqueKey = $"{sensor.SensorId}_{sensor.Timestamp:yyyyMMddHHmmss}";

      if (seenIds.Contains(uniqueKey))
      {
        duplicates++;
        continue;
      }

      if (!sensor.IsValid())
      {
        invalid++;
        continue;
      }

      sensor.Value = NormalizeValue(sensor.Value);

      seenIds.Add(uniqueKey);
      cleaned.Add(sensor);
    }

    return cleaned;
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
      Alerts = new List<Alert>() // will be populated later
    };
  }

  private Dictionary<string, List<SensorData>> GroupByZone(List<SensorData> data)
  {
    if (data == null || data.Count == 0)
      return new Dictionary<string, List<SensorData>>();

    var zones = new Dictionary<string, List<SensorData>>();

    //(approx 0.01 grados ~ 1km)
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
