using System.Collections.Concurrent;
using Smartflow.Business.Validation;
using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;

namespace Smartflow.Business.Parallelization;

public class ParallelizationEngine
{
  private readonly Configuration _config;
  private readonly RuleValidator _validator;

  public ParallelizationEngine(Configuration config, RuleValidator validator)
  {
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _validator = validator ?? throw new ArgumentNullException(nameof(validator));
  }

  public List<ProcessedData> ProcessInParallel(List<SensorData> data)
  {
    if (data == null || data.Count == 0)
      throw new ArgumentException("La lista de datos no puede estar vacía.", nameof(data));

    Console.WriteLine($"\n[PARALLEL] Estrategia: {_config.Strategy}");
    Console.WriteLine($"[PARALLEL] Threads: {_config.MaxThreads}");
    Console.WriteLine($"[PARALLEL] Registros: {data.Count}");
    Console.WriteLine($"[PARALLEL] BlockSize: {_config.BlockSize}");
    Console.WriteLine(new string('=', 60));

    List<ProcessedData> results = _config.Strategy switch
    {
      ParallelizationStrategy.DATA_DECOMPOSITION => ProcessWithDataDecomposition(data),
      _ => throw new InvalidOperationException($"Estrategia no soportada: {_config.Strategy}")
    };

    Console.WriteLine("[PARALLEL] Procesamiento completado");
    Console.WriteLine($"[PARALLEL] Zonas generadas: {results.Count}");
    Console.WriteLine(new string('=', 60));

    return results;
  }

  public void ConfigureStrategy(ParallelizationStrategy strategy)
  {
    if (!Enum.IsDefined(typeof(ParallelizationStrategy), strategy))
      throw new ArgumentException("Estrategia inválida", nameof(strategy));

    _config.Strategy = strategy;
    Console.WriteLine($"[PARALLEL] Estrategia cambiada a: {strategy}");
  }

  private List<ProcessedData> ProcessWithDataDecomposition(List<SensorData> data)
  {
    Console.WriteLine("[PARALLEL] DESCOMPOSICIÓN DE DATOS");

    // FASE 1: LIMPIEZA PARALELA CON COLECCIONES CONCURRENTES
    var cleanedData = CleanDataParallel(data);
    Console.WriteLine($"[PARALLEL] Limpiados: {cleanedData.Count}/{data.Count}");

    if (cleanedData.Count == 0)
    {
      Console.WriteLine("[PARALLEL] No hay datos válidos para procesar");
      return new List<ProcessedData>();
    }

    // FASE 2: AGRUPAR POR ZONA (PRIMERO) - ESTO REDUCE LA CONTENCIÓN
    var groupedByZone = GroupByZoneParallel(cleanedData);
    Console.WriteLine($"[PARALLEL] Zonas identificadas: {groupedByZone.Count}");

    // FASE 3: VALIDAR Y CALCULAR ESTADÍSTICAS POR ZONA EN PARALELO
    var results = ProcessZonesParallel(groupedByZone);

    return results;
  }

  private List<SensorData> CleanDataParallel(List<SensorData> data)
  {
    // Usar ConcurrentBag para evitar locks en agregación
    var cleanedBag = new ConcurrentBag<SensorData>();
    var seenIds = new ConcurrentDictionary<string, byte>();

    // Particionar datos para reducir overhead
    var partitioner = Partitioner.Create(0, data.Count, _config.BlockSize);

    Parallel.ForEach(partitioner,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        range =>
        {
          for (int i = range.Item1; i < range.Item2; i++)
          {
            var sensor = data[i];

            if (sensor == null || !sensor.IsValid())
              continue;

            sensor.SensorId = sensor.SensorId?.Trim() ?? string.Empty;
            string uniqueKey = $"{sensor.SensorId}_{sensor.Timestamp:yyyyMMddHHmmss}";

            // TryAdd es atómico - no necesita lock
            if (seenIds.TryAdd(uniqueKey, 0))
            {
              sensor.Value = NormalizeValue(sensor.Value);
              cleanedBag.Add(sensor);
            }
          }
        });

    return cleanedBag.ToList();
  }

  private ConcurrentDictionary<string, ConcurrentBag<SensorData>> GroupByZoneParallel(List<SensorData> data)
  {
    const double GRID_SIZE = 0.40;

    var zones = new ConcurrentDictionary<string, ConcurrentBag<SensorData>>();

    // Particionar para mejor rendimiento
    var partitioner = Partitioner.Create(0, data.Count, _config.BlockSize);

    Parallel.ForEach(partitioner,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        range =>
        {
          for (int i = range.Item1; i < range.Item2; i++)
          {
            var sensor = data[i];

            double roundedLat = Math.Round(sensor.Latitude, 2);
            double roundedLon = Math.Round(sensor.Longitude, 2);

            int latGrid = (int)Math.Floor(roundedLat / GRID_SIZE);
            int lonGrid = (int)Math.Floor(roundedLon / GRID_SIZE);

            string zoneKey = $"Zone_{latGrid}_{lonGrid}";

            // GetOrAdd es thread-safe
            var zoneBag = zones.GetOrAdd(zoneKey, _ => new ConcurrentBag<SensorData>());
            zoneBag.Add(sensor);
          }
        });

    return zones;
  }

  private List<ProcessedData> ProcessZonesParallel(
      ConcurrentDictionary<string, ConcurrentBag<SensorData>> groupedByZone)
  {
    var resultsBag = new ConcurrentBag<ProcessedData>();

    // Procesar cada zona en paralelo
    Parallel.ForEach(groupedByZone,
        new ParallelOptions { MaxDegreeOfParallelism = _config.MaxThreads },
        kvp =>
        {
          string zoneName = kvp.Key;
          var zoneData = kvp.Value.ToList();

          // Validar datos de la zona
          var alerts = new List<Alert>();
          foreach (var sensor in zoneData)
          {
            var sensorAlerts = _validator.ValidateData(sensor);
            alerts.AddRange(sensorAlerts);
          }

          // Calcular estadísticas
          var processedData = CalculateStatistics(zoneData, zoneName);
          processedData.Alerts = alerts;

          resultsBag.Add(processedData);
        });

    return resultsBag.ToList();
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

    var statistics = new Dictionary<string, double>
    {
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
}
