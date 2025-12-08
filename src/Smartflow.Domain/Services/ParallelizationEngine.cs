using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smartflow.Domain.Services
{
    public class ParallelizationEngine
    {
        private readonly Configuration _config;
        private ParallelizationStrategy _strategy;
        private readonly DataProcessor _processor;

        public ParallelizationEngine(Configuration config)
        {
            _config = config;
            _strategy = config.Strategy;
            _processor = new DataProcessor(config);
        }

        //Procesador de datos 
        public List<ProcessedData> ProcessInParallel(List<SensorData> data)
        {
            var partitions = PartitionData(data);

            var partialResults = new ConcurrentBag<ProcessedData>();
            var countdown = new CountdownEvent(partitions.Count);

            // Sincronización entre fases
            var barrier = new Barrier(partitions.Count);

            List<Task> tasks = new();

            int blockId = 0;

            foreach (var block in partitions)
            {
                int capturedId = blockId++;

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Esperar a que todos lleguen al punto inicial
                        barrier.SignalAndWait();

                        var result = ProcessBlock(block, capturedId);

                        partialResults.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Error en bloque {capturedId}: {ex.Message}");
                    }
                    finally
                    {
                        countdown.Signal();
                    }
                }));
            }

            countdown.Wait(); // Esperar a que todos terminen

            return AggregateResults(partialResults.ToList());
        }

        // particion de datos 
        public List<List<SensorData>> PartitionData(List<SensorData> data)
        {
            int blockSize = data.Count / _config.MaxThreads;
            var partitions = new List<List<SensorData>>();

            for (int i = 0; i < _config.MaxThreads; i++)
            {
                var partition = data
                    .Skip(i * blockSize)
                    .Take(blockSize)
                    .ToList();

                partitions.Add(partition);
            }

            // Balanceo adicional si sobran elementos
            int remainder = data.Count % _config.MaxThreads;
            if (remainder > 0)
                partitions.Last().AddRange(data.TakeLast(remainder));

            return partitions;
        }

        // Esto procesa un bloque 
        public ProcessedData ProcessBlock(List<SensorData> block, int blockId)
        {
            Console.WriteLine($"Procesando bloque {blockId} ({block.Count} registros)");

            // Usamos DataProcessor para transformaciones reales
            var processed = _processor.Process(block);

            // Calcular estadísticas parciales
            double max = processed.Max(p => p.Value);
            double min = processed.Min(p => p.Value);
            double avg = processed.Average(p => p.Value);

            return new ProcessedData
            {
                Zone = $"Block-{blockId}",
                ProcessedAt = DateTime.UtcNow,
                RecordCount = processed.Count,
                Statistics = new Dictionary<string, double>
                {
                    {"max", max},
                    {"min", min},
                    {"avg", avg}
                },
                Alerts = processed
                    .Where(p => p.Value > 100) // Ejemplo
                    .Select(p => new Alert { Message = "High value", Timestamp = p.Timestamp })
                    .ToList()
            };
        }

        
        // Esto agrega los resultados
        public List<ProcessedData> AggregateResults(List<ProcessedData> results)
        {
            double globalMax = results.Max(r => r.GetMax());
            double globalMin = results.Min(r => r.GetMin());
            double globalAvg =
                results.Sum(r => r.GetAverage() * r.RecordCount) /
                results.Sum(r => r.RecordCount);

            return new List<ProcessedData>
            {
                new ProcessedData
                {
                    Zone = "GLOBAL",
                    ProcessedAt = DateTime.UtcNow,
                    RecordCount = results.Sum(r => r.RecordCount),
                    Statistics = new Dictionary<string, double>
                    {
                        {"max", globalMax},
                        {"min", globalMin},
                        {"avg", globalAvg}
                    },
                    Alerts = results.SelectMany(r => r.Alerts).ToList()
                }
            };
        }

        // Esto hace la configuracion de la estrategia
        public void ConfigureStrategy(ParallelizationStrategy strategy)
        {
            _strategy = strategy;
            Console.WriteLine($"Estrategia de paralelización configurada: {strategy}");
        }
    }
}
