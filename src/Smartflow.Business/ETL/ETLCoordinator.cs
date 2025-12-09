using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
using Smartflow.Business.Validation;
using Smartflow.Infrastructure.Config;
using System.Diagnostics;
using Smartflow.Domain.Enums;
namespace Smartflow.Business.ETL;


public class ETLCoordinator
{
  private readonly IDataExtractor _extractor;
  private readonly IDataLoader _loader;
  private readonly RuleValidator _validator;
  private readonly Configuration _config;

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


  public Metrics ProcessData(string inputPath)
  {

    if (string.IsNullOrWhiteSpace(inputPath))
      throw new ArgumentException("El input path no puede ser nulo o vacio.", nameof(inputPath));

    if (!File.Exists(inputPath))
      throw new FileNotFoundException($"Input path no encontrado: {inputPath}");

    var metrics = new Metrics();
    var stopwatch = Stopwatch.StartNew();

    try
    {
      Console.WriteLine("=== Iniciando Proceso ETL Secuencial ===\n");

      Console.WriteLine($"[ETL] Iniciando Proceso ETL para: {inputPath}");
      Console.WriteLine($"[ETL] Fecha y hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
      Console.WriteLine(new string('=', 60));

      // 1. EXTRACT
      Console.WriteLine("[ETL] Fase 1: EXTRACCION");
      var rawData = ExtractData(inputPath);
      Console.WriteLine($"[ETL] Extraidos {rawData.Count} registros");

      if (rawData.Count == 0)
      {
        Console.WriteLine("[ETL] No hay datos para procesar. Abortando.");
        stopwatch.Stop();
        metrics.SequentialTime = stopwatch.Elapsed.TotalSeconds;
        return metrics;
      }

      // 2. TRANSFORM
      Console.WriteLine("\n ETL Fase 2: TRANSFORMACION");

      Console.WriteLine("[ETL] Paso 2.1: Limpiando dato...");
      var cleanedData = CleanData(rawData);
      Console.WriteLine($"[ETL] Limpiados: {cleanedData.Count}/{rawData.Count} registros validos");

      Console.WriteLine("[ETL] Paso 2.2: Validando reglas...");
      var allAlerts = _validator.ValidateData(cleanedData);
      Console.WriteLine($"[ETL] Generadas {allAlerts.Count} alertas");

      Console.WriteLine("[ETL] Paso 2.3: Agrupando por zona...");
      var groupedByZone = GroupByZone(cleanedData);
      Console.WriteLine($"[ETL] Creadas {groupedByZone.Count} zonas geograficas");

      Console.WriteLine("[ETL] Paso 2.4: Calculando estadisticas...");
      var processedDataList = CalculateStatisticsForZones(groupedByZone, allAlerts);
      Console.WriteLine($"[ETL] Estadisticas calculadas para todas las zonas");

      // 3. LOAD
      Console.WriteLine("\n[ETL] Fase 3: CARGA");
      var outputPath = GenerateOutputPath(_config.OutputPath, inputPath);

      if (processedDataList.Count == 1)
      {
        _loader.Load(processedDataList[0], outputPath);
      }
      else
      {
        _loader.LoadBatch(processedDataList, outputPath);
      }

      Console.WriteLine($"[ETL] Data cargados en: {outputPath}");

      stopwatch.Stop();
      metrics.SequentialTime = stopwatch.Elapsed.TotalSeconds;

      Console.WriteLine(new string('=', 60));
      Console.WriteLine($"[ETL] Proceso completado exitosamente");
      Console.WriteLine($"[ETL] Tiempo total de ejecucion: {metrics.SequentialTime:F3} segundos");
      Console.WriteLine($"[ETL] Registros procesados: {cleanedData.Count}");
      Console.WriteLine($"[ETL] Alertas generadas: {allAlerts.Count}");
      Console.WriteLine($"[ETL] Zonas creadas: {processedDataList.Count}");

      return metrics;

    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Console.WriteLine($"\n[ETL] ERROR: {ex.Message}");
      Console.WriteLine($"[ETL] Traza de error: {ex.StackTrace}");
      throw new InvalidOperationException("El proceso ETL fallo", ex);
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
      Console.WriteLine($"[ETL] Fallo en la extraccion: {ex.Message}");
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
    const double GRID_SIZE = 0.01;

    foreach (var sensor in data)
    {
      int latGrid = (int)Math.Floor(sensor.Latitude / GRID_SIZE);
      int lonGrid = (int)Math.Floor(sensor.Longitude / GRID_SIZE);

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
