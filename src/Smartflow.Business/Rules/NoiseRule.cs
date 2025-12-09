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
      SensorId = data.SensorId,
      Type = "NOISE_HIGH",
      Severity = DetermineSeverity(data.Value),
      Value = data.Value,
      Threshold = new ThresholdInfo
      {
        Limit = _threshold
      },
      Timestamp = data.Timestamp
    };
  }

  private AlertSeverity DetermineSeverity(double value)
  {
    if (value > 100)
      return AlertSeverity.CRITICAL;

    if (value > 85)
      return AlertSeverity.HIGH;

    return AlertSeverity.WARNING;
  }
}
