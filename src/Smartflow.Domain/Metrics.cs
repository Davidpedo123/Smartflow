using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartflow.Domain
{
    public class Metrics
    {
        public double SequentialTime { get; set; }

        public double ParallelTime { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }
        public int ThreadCount { get; set; }
        public int RecordCount { get; set; }
        public Dictionary<string, long> DetailedMetrics { get; set; } = new Dictionary<string, long>();

        public void Calculate()
        {
            Speedup = ParallelTime > 0 ? SequentialTime / ParallelTime : 0;
            Efficiency = ThreadCount > 0 ? Speedup / ThreadCount : 0;

            DetailedMetrics.Clear();
        }

        public string ToJson()
        {
            Console.WriteLine("Converting Metrics to JSON");
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
