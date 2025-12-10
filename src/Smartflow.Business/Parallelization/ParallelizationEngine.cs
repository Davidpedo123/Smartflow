using Smartflow.Business.Validation;
using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;

namespace Smartflow.Business.Parallelization;

public class ParallelizationEngine
{
  private readonly Configuration _config;
  private readonly List<Task> _activeTasks;
  private readonly RuleValidator _validator;

  private readonly object _lockClean = new object();
  private readonly object _lockAlerts = new object();
  private readonly object _lockZones = new object();
  private readonly object _lockResults = new object();

  public ParallelizationEngine(Configuration config, RuleValidator validator)
  {
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    _activeTasks = new List<Task>();
  }

  public List<ProcessedData> ProcessedInParallel(List<SensorData> data)
  {
    if (data == null || data.Count == 0)
      throw new ArgumentException("La lista de datos puede estar vacia.", nameof(data));

    Console.WriteLine($"\n[PARALLEL] Estrategia: {_config.Strategy}");
    Console.WriteLine($"[PARALLEL] Threads: {_config.MaxThreads}");
    Console.WriteLine($"[PARALLEL] Registros: {data.Count}");
    Console.WriteLine(new string('=', 60));

    _activeTasks.Clear();

    List<ProcessedData> results = _config.Strategy switch
    {
      ParallelizationStrategy.DATA_DECOMPOSITION => ProcessWithDataDescomposition(data),
      // ParallelizationStrategy.TASK_PARALLELISM => ProcessWithTaskParallelism(data),
      _ => throw new InvalidOperationException($"Estrategia no soportada: {_config.Strategy}");
    };

    Console.WriteLine("[PARALLEL] Procesamiento completado");
    Console.WriteLine($"[PARALLEL] Zonas generadas: {results.Count}");
    Console.WriteLine(new string('=', 60));

    return results;
  }

  public void ConfigureStategy(ParallelizationStrategy strategy)
  {
    if (!Enum.IsDefined(typeof(ParallelizationStrategy), strategy))
      throw new ArgumentException("Estrategia invalida", nameof(strategy));

    _config.Strategy = strategy;
    Console.WriteLine($"[PARALLEL] Estrategia cambiada a: {strategy}");
  }

  private List<List<SensorData>> PartitionData(List<SensorData> data)
  {
    if (data == null || data.Count == 0)
      return new List<List<SensorData>>();

    var partitions = new List<List<SensorData>>();
    int blockSize = _config.BlockSize;

    if (blockSize <= 0 || blockSize >= data.Count)
    {
      partitions.Add(data);
      return partitions;
    }

    for (int i = 0; i < data.Count; i += blockSize)
    {
      int remaining = Math.Min(blockSize, data.Count - i);
      var partition = data.GetRange(i, remaining);
      partitions.Add(partition);
    }

    Console.WriteLine($"[PARALLEL] Datos particiones en {partitions.Count} bloques");
    return partitions;
  }


  private List<ProcessedData> ProcessWithDataDescomposition(List<SensorData> data)
  {
    Console.WriteLine("[PARALLEL] DATA DESCOMPOSITION");

    var cleanedData = CleanDataParallel(data);
    Console.WriteLine($"[PARALLEL] Limpiados: {cleanedData.Count}");

    var allAlerts = ValidateDataParallel(cleanedData);
    Console.WriteLine($"[PARALLEL] Alertas: {allAlerts.Count}");

    var groupedByZone = GroupByZoneParallel(cleanedData);
    Console.WriteLine($"[PARALLEL] Zonas: {groupedByZone.Count}");

    var results = CalculateStatisticsParallel(groupedByZone, allAlerts);

    return results;
  }

  private List<SensorData> CleanDataParallel(List<SensorData> data)
  {
    var cleaned = new List<SensorData>();
    var seenIds = new HashSet<string>();

    Parallel.ForEach(data,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        sensor =>
        {
          if (sensor == null || !sensor.IsValid())
            return;

          sensor.SensorId = sensor.SensorId?.Trim() ?? string.Empty;
          string uniqueKey = $"{sensor.SensorId}_{sensor.Timestamp:yyyyMMddHHmmss}";

          lock (_lockClean)
          {
            if (!seenIds.Contains(uniqueKey))
            {
              sensor.Value = NormalizeValue(sensor.Value);
              seenIds.Add(uniqueKey);
              cleaned.Add(sensor);
            }
          }
        });

    return cleaned;
  }

  private List<Alert> ValidateDataParallel(List<SensorData> dataList)
  {
    var allAlerts = new List<Alert>();

    Parallel.ForEach(dataList,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        sensorData =>
        {
          var alerts = _validator.ValidateData(sensorData);

          if (alerts.Count > 0)
          {
            lock (_lockAlerts)
            {
              allAlerts.AddRange(alerts);
            }
          }
        });

    return allAlerts;
  }

  private Dictionary<string, List<SensorData>> GroupByZoneParallel(List<SensorData> data)
  {
    const double GRID_SIZE = 0.40;

    var zones = new Dictionary<string, List<SensorData>>();

    Parallel.ForEach(data,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        sensor =>
        {
          double roundedLat = Math.Round(sensor.Latitude, 2);
          double roundedLon = Math.Round(sensor.Longitude, 2);

          int latGrid = (int)Math.Floor(roundedLat / GRID_SIZE);
          int lonGrid = (int)Math.Floor(roundedLon / GRID_SIZE);

          string zoneKey = $"Zone_{latGrid}_{lonGrid}";

          lock (_lockZones)
          {
            if (!zones.ContainsKey(zoneKey))
            {
              zones[zoneKey] = new List<SensorData>();
            }

            zones[zoneKey].Add(sensor);
          }
        });

    return zones;
  }

  private List<ProcessedData> CalculateStatisticsParallel(
      Dictionary<string, List<SensorData>> groupByZone,
      List<Alert> allAlerts
      )
  {
    var results = new List<ProcessedData>();

    Parallel.ForEach(groupByZone,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        kvp =>
        {
          string zoneName = kvp.Key;
          var zoneData = kvp.Value;

          var processedData = CalculateStatistics(zoneData, zoneName);

          var zoneSensorIds = new HashSet<string>(zoneData.Select(s => s.SensorId));
          processedData.Alerts = allAlerts.Where(a => zoneSensorIds.Contains(a.SensorId)).ToList();

          lock (_lockResults)
          {
            results.Add(processedData);
          }

        });

    return results;
  }

  private double NormalizeValue(double value)
  {
    const double MAX_VALUE = 10_000;
    const double MIN_VALUE = -100;

    if (value > MAX_VALUE) return MAX_VALUE;
    if (value < MIN_VALUE) return MIN_VALUE;

    return value;
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

}


