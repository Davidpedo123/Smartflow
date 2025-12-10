using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  public interface IDataExtractor
  {
    List<SensorData> Extract(string path);
    List<SensorData> ExtractBatch(List<string> filePaths);
    // Task<List<SensorData>> ExtractAsync(string path);
  }
}
