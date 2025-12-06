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
    return data.Type == SensorType.TEMPERATURE && (data.Value < _min || data.Value > _max);
  }

  public Alert GenerateAlert(SensorData data)
  {
    return new Alert
    {
      Type = "TEMP_OUT_OF_RANGE",
      Severity = AlertSeverity.WARNING,
      Value = data.Value,
      Threshold = new ThresholdInfo
      {
        Min = _min,
        Max = _max
      },
      Timestamp = data.Timestamp
    };
  }
}

