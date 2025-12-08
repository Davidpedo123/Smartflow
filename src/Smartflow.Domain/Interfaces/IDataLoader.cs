using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces
{
  public interface IDataLoader
  {
    void Load(ProcessedData data, string path);
    // Task LoadAsync(ProcessedData data, string path);
  }
}
