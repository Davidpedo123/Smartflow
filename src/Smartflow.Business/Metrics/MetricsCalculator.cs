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

  //TODO: implementar MesasureParallel y ComparePerformance methods
}
