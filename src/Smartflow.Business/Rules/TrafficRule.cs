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
      Type = "TRAFFIC_HIGH",
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
