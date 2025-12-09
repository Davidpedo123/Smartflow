using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
using Smartflow.Business.Validation;
using Smartflow.Infrastructure.Config;
using Smartflow.Domain.Enums;
using Smartflow.Business.Processing.Interfaces;
using System.Diagnostics;

namespace Smartflow.Business
{
    public class ETLCoordinator
    {
        public bool UseParallel { get; set; } = false;

        private readonly IDataExtractor _extractor;
        private readonly IDataLoader _loader;
        private readonly RuleValidator _ruleValidator;
        private readonly ConfigurationManager _configManager;
        private readonly IParallelizationEngine _parallelEngine;

        public ETLCoordinator(
           IDataExtractor extractor,
           IDataLoader loader,
           RuleValidator validator,
           ConfigurationManager configManager,
           IParallelizationEngine parallelEngine)
        {
            _extractor = extractor;
            _loader = loader;
            _ruleValidator = validator;
            _configManager = configManager;
            _parallelEngine = parallelEngine;
        }


        //  Modo secuencial
        public Metrics ProcessDataSequential(string inputPath)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("=== Iniciando Proceso ETL SECUENCIAL ===\n");

            // 1. EXTRACT
            Console.WriteLine("1. Extrayendo datos...");
            var rawData = _extractor.Extract(inputPath);
            Console.WriteLine($"   {rawData.Count} registros extraídos\n");

            // 2. TRANSFORM
            Console.WriteLine("2. Transformando datos...");

            // 2.1 Limpiar
            var cleanData = CleanData(rawData);
            Console.WriteLine($"   ✓ {cleanData.Count} registros limpios");

            // 2.2 Validar reglas
            var validatedData = new List<SensorData>();
            foreach (var data in cleanData)
            {
                var alerts = _ruleValidator.ValidateData(data);
                if (alerts.Count == 0 || alerts.All(a => a.Severity != AlertSeverity.CRITICAL))
                {
                    validatedData.Add(data);
                }
            }
            Console.WriteLine($"   ✓ {validatedData.Count} registros validados");

            var groupedByZone = GroupByZone(validatedData);
            Console.WriteLine($"   ✓ {groupedByZone.Count} zonas identificadas\n");

            // 3. LOAD
            Console.WriteLine("3. Cargando datos...");
            foreach (var zone in groupedByZone)
            {
                var stats = CalculateStatistics(zone.Value, zone.Key);
                var processedData = new ProcessedData
                {
                    Zone = zone.Key,
                    ProcessedAt = DateTime.Now,
                    RecordCount = zone.Value.Count,
                    Statistics = stats,
                    Alerts = new List<Alert>()
                };

                _loader.Load(processedData, $"output/zone_{zone.Key}.json");
            }

            Console.WriteLine("   ✓ Datos cargados correctamente\n");

            stopwatch.Stop();
            Console.WriteLine($"=== Proceso completado en {stopwatch.ElapsedMilliseconds}ms ===\n");

            return new Metrics
            {
                SequentialTime = stopwatch.ElapsedMilliseconds
            };
        }

        // Modo Paralelo
        public Metrics ProcessDataParallel(string inputPath)
        {
            Console.WriteLine("Ejecutando ETL en modo PARALELO...");

            var stopwatch = Stopwatch.StartNew();

            var rawData = _extractor.Extract(inputPath);

            var results = _parallelEngine.ProcessInParallel(rawData);

            foreach (var result in results)
            {
                _loader.Load(result, $"output/zone_{result.Zone}_parallel.json");
            }

            stopwatch.Stop();

            Console.WriteLine($"Tiempo PARALELO: {stopwatch.ElapsedMilliseconds} ms\n");

            return new Metrics
            {
                ParallelTime = stopwatch.ElapsedMilliseconds
            };
        }

        // Comparacion de rendimiento
        public Metrics ComparePerformance(string inputPath)
        {
            Console.WriteLine("Comparando rendimiento (Secuencial vs Paralelo)...\n");

            // --- Secuencial ---
            var sw1 = Stopwatch.StartNew();
            ProcessDataSequential(inputPath);
            sw1.Stop();
            long seqTime = sw1.ElapsedMilliseconds;

            // --- Paralelo ---
            var sw2 = Stopwatch.StartNew();
            ProcessDataParallel(inputPath);
            sw2.Stop();
            long parTime = sw2.ElapsedMilliseconds;

            var metrics = new Metrics
            {
                SequentialTime = seqTime,
                ParallelTime = parTime,
                Speedup = Math.Round((double)seqTime / parTime, 2),
                Efficiency = Math.Round(
                    (double)seqTime / (parTime * Environment.ProcessorCount), 4)
            };

            PrintVisualReport(metrics);
            return metrics;
        }

        // Reporte visual de métricas
        private void PrintVisualReport(Metrics m)
        {
            Console.WriteLine("\n================= Reporte Visual de metricas =================");

            Console.WriteLine($"Secuencial: {m.SequentialTime} ms");
            Console.WriteLine($"Paralelo:   {m.ParallelTime} ms");
            Console.WriteLine($"SpeedUp:    {m.Speedup}x");
            Console.WriteLine($"Eficiencia: {m.Efficiency}");

            Console.WriteLine("=========================================================\n");
        }

        // Ejecuta basado en la propiedad UseParallel
        public Metrics Execute(string inputPath)
        {
            if (UseParallel)
            {
                Console.WriteLine("(Encendido) Ejecutando en modo Paralelo…");
                return ProcessDataParallel(inputPath);
            }

            Console.WriteLine("(Apagado) Ejecutando en modo Secuencial…");
            return ProcessDataSequential(inputPath);
        }

        // ===============================
        // FUNCIONES INTERNAS (del viejo)
        // ===============================

        private List<SensorData> CleanData(List<SensorData> data)
        {
            return data
                .Where(d => d.IsValid())
                .GroupBy(d => d.SensorId + d.Timestamp)
                .Select(g => g.First())
                .ToList();
        }

        private Dictionary<string, double> CalculateStatistics(List<SensorData> data, string zone)
        {
            if (!data.Any()) return new Dictionary<string, double>();

            return new Dictionary<string, double>
            {
                ["Average"] = data.Average(d => d.Value),
                ["Max"] = data.Max(d => d.Value),
                ["Min"] = data.Min(d => d.Value),
                ["Count"] = data.Count
            };
        }

        private Dictionary<string, List<SensorData>> GroupByZone(List<SensorData> data)
        {
            return data
                .GroupBy(d => $"{Math.Round(d.Latitude, 1)}_{Math.Round(d.Longitude, 1)}")
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );
        }
    }
}
