using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  interface IDataExtractor
  {
    List<SensorData> Extract(string path);
    // Task<List<SensorData>> ExtractAsync(string path);
  }
}
