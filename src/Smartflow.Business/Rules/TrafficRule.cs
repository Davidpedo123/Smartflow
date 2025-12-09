using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Rules;

public class TrafficRule : IRule
{
  private readonly int _threshold;

  public TrafficRule(int threshold)
  {
    _threshold = threshold;
  }

  public bool Evaluate(SensorData data)
  {
    return data.Type == SensorType.TRAFFIC && data.Value > _threshold;
  }

  public Alert GenerateAlert(SensorData data)
  {
    return new Alert
    {
      SensorId = data.SensorId,
      Type = "TRAFFIC_HIGH",
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
    if (value > 160)
      return AlertSeverity.CRITICAL;

    if (value > 130)
      return AlertSeverity.HIGH;

    return AlertSeverity.WARNING;
  }
}
