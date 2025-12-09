using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  public interface IDataLoader
  {
    void Load(ProcessedData data, string outputPath);
    void LoadBatch(List<ProcessedData> dataList, string outputPath);
  }
}
