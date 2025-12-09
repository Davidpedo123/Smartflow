using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Rules;

public class HumidityRule : IRule
{
  private readonly double _min;
  private readonly double _max;

  public HumidityRule(double min, double max)
  {
    _min = min;
    _max = max;
  }

  public bool Evaluate(SensorData data)
  {
    return data.Type == SensorType.HUMIDITY &&
           (data.Value < _min || data.Value > _max);
  }

  public Alert GenerateAlert(SensorData data)
  {
    bool isTooLow = data.Value < _min;

    return new Alert
    {
      SensorId = data.SensorId,
      Type = isTooLow ? "LOW_HUMIDITY" : "HIGH_HUMIDITY",
      Severity = DetermineSeverity(data.Value),
      Value = data.Value,
      Threshold = new ThresholdInfo
      {
        Min = _min,
        Max = _max
      },
      Timestamp = data.Timestamp
    };
  }

  private AlertSeverity DetermineSeverity(double value)
  {
    if (value < 20 || value > 90)
      return AlertSeverity.CRITICAL;

    if (value < 25 || value > 85)
      return AlertSeverity.HIGH;

    return AlertSeverity.WARNING;
  }
}
