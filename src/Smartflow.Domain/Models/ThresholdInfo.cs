namespace Smartflow.Domain.Models;

public class ThresholdInfo
{
  public double? Min { get; set; }
  public double? Max { get; set; }
  public double? Limit { get; set; } //INFO: this is for simple umbrals just like Noise, C02, Traffic
}
