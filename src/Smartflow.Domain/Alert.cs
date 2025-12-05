using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartflow.Domain.Enums;

namespace Smartflow.Domain
{
  public class Alert
  {
    public string Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public double Value { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
    //WARNING: member names can be the same as their enclosing   
    // public void Alert()
    // {
    //     Console.WriteLine("Incluir logica de alerta aqui");
    // }
  }
}
