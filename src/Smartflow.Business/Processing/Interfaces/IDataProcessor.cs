using Smartflow.Domain.Models;
using System.Collections.Generic;

namespace Smartflow.Business.Processing.Interfaces
{
    public interface IDataProcessor
    {

        List<SensorData> Process(List<SensorData> data);
    }
}
