namespace Smartflow.Business.ETL;

public class PerformanceComparison
{
    public ETLResult Sequential { get; set; } = new();
    public ETLResult Parallel { get; set; } = new();
    public long ImprovementMs { get; set; }
    public double ImprovementPercent { get; set; }
}