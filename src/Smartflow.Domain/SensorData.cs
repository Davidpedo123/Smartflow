using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartflow.Domain
{
    public enum SensorType
    {
        NOISE,
        TEMP,
        HUMIDITY,
        TRAFFIC,
        POLLUTION
    }
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


            SensorId = SensorId?.Trim() ?? string.Empty;

            if (Timestamp == DateTime.MinValue) return false;
            if (string.IsNullOrEmpty(SensorId)) return false;
            if (!Enum.IsDefined(typeof(SensorType), Type)) return false;
            if (Latitude < -90 || Latitude > 90) return false;
            if (Longitude < -180 || Longitude > 180) return false;

            if (double.IsNaN(Value) || double.IsInfinity(Value)) return false;

            switch(Type)
            {


           
                case SensorType.TEMP:
                    
                    if (Value < -50 || Value > 60) return false;

                    
                    if (Value < 15 || Value > 35)
                    {
                        Console.WriteLine("ALERTA: Temperatura Anómala");
                    }
                    break;

                case SensorType.NOISE:
                    
                    if (Value < 0 || Value > 140) return false;

                    
                    if (Value > 70)
                    {
                        Console.WriteLine("ALERTA: Ruido Elevado");
                    }
                    break;

                case SensorType.HUMIDITY:
                    
                    if (Value < 0 || Value > 100) return false;
                    break;

                case SensorType.POLLUTION:
                
                    if (Value < 0 || Value > 50000) return false;

             
                    if (Value > 400)
                    {
                        Console.WriteLine("ALERTA: Contaminación Alta");
                    }
                    break;

                case SensorType.TRAFFIC:
         
                    if (Value < 0 || Value > 100000) return false;

            
                    if (Value > 80)
                    {
                        Console.WriteLine("ALERTA: Congestión Alta");
                    }
                    break;

                default:
                    return false;

            }
            return true;


        }

        public void clone()
        {
            var sensorData = new SensorData
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
    

        




