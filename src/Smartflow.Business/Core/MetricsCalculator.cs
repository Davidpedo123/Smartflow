using Smartflow.Domain.Models;

namespace Smartflow.Business;


// Plantilla base para el MetricsCalculator..
public class MetricsCalculator
{
    
    public Metrics CalculateMetrics(double seqTime, double parTime, int threadCount)
    {
        var metrics = new Metrics
        {
            SequentialTime = seqTime,
            ParallelTime = parTime,
            //ThreadCount = threadCount
        };
        
        metrics.Calculate(threadCount);
        
        return metrics;
    }

   
    public Metrics CompareExecution()
    {
        throw new NotImplementedException("Pendiente: implementar cuando ETL esté listo");
    }

    
    public List<Metrics> MeasureScalability(List<int> threadCounts)
    {
        throw new NotImplementedException("Pendiente: implementar cuando ParallelizationEngine esté listo");
    }

    
    public string IdentifyBottlenecks()
    {
        throw new NotImplementedException("Pendiente: implementar análisis de fases");
    }

    
    public void ExportMetrics(Metrics metrics, string path)
    {
        var json = metrics.ToJson();
        File.WriteAllText(path, json);
    }

    
    public string GenerateReport()
    {
        throw new NotImplementedException("Pendiente: implementar generación de reporte");
    }
}