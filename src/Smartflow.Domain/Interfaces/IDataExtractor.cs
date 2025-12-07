using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  public interface IDataExtractor
  {
    List<SensorData> Extract(string path);
    Task<List<SensorData>> ExtractAsync(string path);
  }
}
