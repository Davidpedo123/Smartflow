using Smartflow.Domain.Models;

namespace Smartflow.Domain.Interfaces;

public interface IRule
{
  bool Evaluate(SensorData data);
  Alert GenerateAlert(SensorData data);
}
