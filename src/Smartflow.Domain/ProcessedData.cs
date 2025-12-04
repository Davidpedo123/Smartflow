using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartflow.Domain
{
    public class ProcessedData
    {
        public string Zone { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public Dictionary<string, double> Statistics { get; set; } = new Dictionary<string, double>();

        public List<Alert> Alerts { get; set; } = new List<Alert>();
        
        public int RecordCount { get; set; }

        

        public double GetMax()
        {
            if (Statistics.ContainsKey("Maximo"))
                return Statistics["Maximo"];
            return 0;
        }

        public double GetMin()
        {
            if (Statistics.ContainsKey("Minimo"))
                return Statistics["Minimo"];
            return 0;
        }

        public double GetAverage()
        {
            if (Statistics.ContainsKey("Promedio"))
                return Statistics["Promedio"];
            return 0;
        }

    }
}
    
    
