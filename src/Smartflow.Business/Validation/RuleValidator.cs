using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Validation;

public class RuleValidator
{

  private readonly Dictionary<string, IRule> _rules;

  public RuleValidator()
  {
    _rules = new Dictionary<string, IRule>();
  }

  public void AddRule(string name, IRule rule)
  {
    if (!_rules.ContainsKey(name))
    {
      _rules.Add(name, rule);
    }
  }


  public List<Alert> ValidateData(SensorData data)
  {
    List<Alert> alerts = new();

    foreach (var rule in _rules.Values)
    {
      if (rule.Evaluate(data))
      {
        alerts.Add(rule.GenerateAlert(data));
      }
    }

    return alerts;
  }

  public Alert? ApplyRule(string ruleName, SensorData data)
  {
    if (_rules.TryGetValue(ruleName, out var rule))
    {
      if (rule.Evaluate(data))
      {
        return rule.GenerateAlert(data);
      }
    }

    return null;
  }

}
