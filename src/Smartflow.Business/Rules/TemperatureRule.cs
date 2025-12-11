using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Rules;

public class TemperatureRule : IRule
{
  private readonly double _min;
  private readonly double _max;

  public TemperatureRule(double min, double max)
  {
    _min = min;
    _max = max;
  }

  public bool Evaluate(SensorData data)
  {
    return data.Type == SensorType.TEMPERATURE &&
           (data.Value < _min || data.Value > _max);
  }

  public Alert GenerateAlert(SensorData data)
  {
    bool isTooLow = data.Value < _min;

    return new Alert
    {
      SensorId = data.SensorId,
      Type = isTooLow ? "TEMP_TOO_LOW" : "TEMP_TOO_HIGH",
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
    if (value < 0 || value > 42)
      return AlertSeverity.CRITICAL;

    if (value < 5 || value > 40)
      return AlertSeverity.HIGH;

    return AlertSeverity.WARNING;
  }
}
