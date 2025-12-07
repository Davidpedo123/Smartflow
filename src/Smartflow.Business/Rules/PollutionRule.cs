using System.Text.Json;
using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Rules;

public class PollutionRule : IRule
{

  private readonly double _threshold;

  public PollutionRule(double threshold)
  {
    _threshold = threshold;
  }

  public bool Evaluate(SensorData data)
  {
    return data.Type == SensorType.POLLUTION && data.Value > _threshold;
  }

  public Alert GenerateAlert(SensorData data)
  {
    return new Alert
    {
      Type = "CO2_HIGH",
      Severity = AlertSeverity.CRITICAL,
      Value = data.Value,
      Threshold = new ThresholdInfo
      {
        Limit = _threshold
      },
      Timestamp = data.Timestamp
    };
  }
}

