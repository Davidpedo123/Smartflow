using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Smartflow.Domain.Models
{
  public class Metrics
  {
    public double SequentialTime { get; set; }
    public double ParallelTime { get; set; }
    public double Speedup { get; set; }
    public double Efficiency { get; set; }


    // <summary>
    // Calculates Speedup and Efficiency based on times.
    // </summary>
    public void Calculate(int threadCount = 1)
    {
      if (threadCount <= 0) return;

      Speedup = SequentialTime / ParallelTime;
      Efficiency = Speedup / threadCount;
    }

    public string ToJson()
    {
      Console.WriteLine("Converting Metrics to JSON");
      return JsonSerializer.Serialize(this, new JsonSerializerOptions
      {
        WriteIndented = true
      });
    }
  }
}
