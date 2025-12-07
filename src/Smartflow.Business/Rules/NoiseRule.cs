using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Rules;

public class NoiseRule : IRule
{
  private readonly double _threshold;

  public NoiseRule(double threshold)
  {
    _threshold = threshold;
  }

  public bool Evaluate(SensorData data)
  {
    return data.Type == SensorType.NOISE && data.Value > _threshold;
  }

  public Alert GenerateAlert(SensorData data)
  {
    return new Alert
    {
      Type = "NOISE_HIGH",
      Severity = AlertSeverity.WARNING,
      Value = data.Value,
      Threshold = new ThresholdInfo
      {
        Limit = _threshold
      },
      Timestamp = data.Timestamp
    };
  }
}
