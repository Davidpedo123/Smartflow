namespace Smartflow.Business.ETL;

public class ETLResult
{
    public string Mode { get; set; } = "";
    public int RecordsProcessed { get; set; }
    public long DurationMs { get; set; }
}