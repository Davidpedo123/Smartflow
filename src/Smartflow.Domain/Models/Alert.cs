using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartflow.Domain.Enums;

namespace Smartflow.Domain.Models
{
  public class Alert
  {
    public string Type { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.INFO;
    public double Value { get; set; }
    public ThresholdInfo Threshold { get; set; } = new();
    public DateTime Timestamp { get; set; }

    //WARNING: member names can be the same as their enclosing   
    // public void Alert()
    // {
    //     Console.WriteLine("Incluir logica de alerta aqui");
    // }
  }
}
