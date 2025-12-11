using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartflow.Domain.Enums;

namespace Smartflow.Domain.Models
{

  public class SensorData
  {
    public DateTime Timestamp { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public SensorType Type { get; set; }
    public double Value { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public bool IsValid()
    {
      return !string.IsNullOrWhiteSpace(this.SensorId)
        && Timestamp <= DateTime.UtcNow
        && !double.IsNaN(Value)
        && !double.IsInfinity(Value)
        && Latitude >= -90 && Latitude <= 90
        && Longitude >= -180 && Longitude <= 180
        && Enum.IsDefined<SensorType>(Type);
    }

    public SensorData Clone()
    {
      return new SensorData
      {
        Timestamp = this.Timestamp,
        SensorId = this.SensorId,
        Value = this.Value,
        Longitude = this.Longitude,
        Latitude = this.Latitude
      };
    }

  }
}







