using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  public interface IDataTransformer
  {
    ProcessedData Transform(List<SensorData> data);
    ProcessedData TransformBatch(List<SensorData> data, int blockId);
  }
}
