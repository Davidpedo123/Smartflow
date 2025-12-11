using System.Diagnostics;
using Smartflow.Business.ETL;

namespace Smartflow.Business.Metrics;

public class MetricsCalculator
{
  private readonly ETLCoordinator _coordinator;

  public MetricsCalculator(ETLCoordinator coordinator)
  {
    _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
  }

  public Domain.Models.Metrics MeasureSequential(List<string> inputPaths)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    _coordinator.ProcessAllData(inputPaths);
    stopwatch.Stop();

    var metrics = new Domain.Models.Metrics
    {
      SequentialTime = stopwatch.Elapsed.TotalSeconds
    };

    Console.WriteLine($"Tiempo Secuencial: {metrics.SequentialTime}");

    return metrics;
  }
 
  public Domain.Models.Metrics MeasureParallel(List<string> inputPaths)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    _coordinator.ProcessAllDataParallel(inputPaths);
    stopwatch.Stop();

    var metrics = new Domain.Models.Metrics
    {
      ParallelTime = stopwatch.Elapsed.TotalSeconds
    };



    Console.WriteLine($"Tiempo paralelo: {metrics.ParallelTime}");
    Console.WriteLine($"Speedup: {metrics.Speedup}");
    Console.WriteLine($"Eficiencia: {metrics.Efficiency}");
    Console.WriteLine($"Sequentialtime: {metrics.SequentialTime}");

    return metrics;
  }

  public Domain.Models.Metrics ComparePerformance(List<string> inputPaths, int threadCount)
{
    Console.WriteLine("\n==========================================");
    Console.WriteLine("   INICIANDO COMPARACIÓN DE RENDIMIENTO   ");
    Console.WriteLine("==========================================\n");
 
    Console.WriteLine(">>> Ejecutando Modo Secuencial...");
    var seqResult = MeasureSequential(inputPaths);

    Console.WriteLine("\n>>> Ejecutando Modo Paralelo...");
    var parResult = MeasureParallel(inputPaths);

    var finalMetrics = new Domain.Models.Metrics
    {
        SequentialTime = seqResult.SequentialTime,
        ParallelTime = parResult.ParallelTime
    };

    finalMetrics.Calculate(threadCount);
    finalMetrics.ToJson();

    Console.WriteLine("\n==========================================");
    Console.WriteLine("          RESULTADOS FINALES              ");
    Console.WriteLine("==========================================");
    Console.WriteLine($"Tiempo Secuencial: {finalMetrics.SequentialTime:F4} s");
    Console.WriteLine($"Tiempo Paralelo:   {finalMetrics.ParallelTime:F4} s");
    Console.WriteLine($"------------------------------------------");
    Console.WriteLine($"Speedup:           {finalMetrics.Speedup:F2}x  (Veces más rápido)");
    Console.WriteLine($"Eficiencia:        {finalMetrics.Efficiency:P2} (Uso de recursos)");
    Console.WriteLine("==========================================\n");

    return finalMetrics;
}
}
