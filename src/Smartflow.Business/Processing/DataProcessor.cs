using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Smartflow.Business.Processing.Interfaces;


namespace Smartflow.Business.Processing
{
    public class DataProcessor : IDataProcessor
    {
        private readonly Configuration _config;

        public DataProcessor(Configuration config)
        {
            _config = config;
        }



        // Esto Procesa el bloque completo

        public List<SensorData> Process(List<SensorData> block)
        {
            List<SensorData> cleaned = Clean(block);
            List<SensorData> validated = Validate(cleaned);
            List<SensorData> transformed = Transform(validated);

            return transformed;
        }


        // 1. Esta hace limpieza

        private List<SensorData> Clean(List<SensorData> block)
        {
            return block
                .Where(s => s != null)
                .Where(s => !double.IsNaN(s.Value) && !double.IsInfinity(s.Value))
                .ToList();
        }

        // 2. Esto hace la validacion segun el metodo IsValid()
        private List<SensorData> Validate(List<SensorData> block)
        {
            return block
                .Where(s => s.IsValid())
                .ToList();
        }


        // 3. Esto hace las transformaciones y las reglas de negocio

        private List<SensorData> Transform(List<SensorData> block)
        {
            List<SensorData> output = new();

            foreach (var sensor in block)
            {
                var clone = sensor.Clone();

                clone.Value = ApplyRules(clone);

                output.Add(clone);
            }

            return output;
        }


        // Esto aplica las reglas del thresholds

        private double ApplyRules(SensorData sensor)
        {
            double val = sensor.Value;

            switch (sensor.Type)
            {
                case SensorType.NOISE:
                    if (val > _config.Thresholds.Noise)
                        val = Limit(val, _config.Thresholds.Noise);
                    break;

                case SensorType.TRAFFIC:
                    if (val > _config.Thresholds.Traffic)
                        val = Limit(val, _config.Thresholds.Traffic);
                    break;

                case SensorType.POLLUTION:
                    if (val > _config.Thresholds.CO2)
                        val = Limit(val, _config.Thresholds.CO2);
                    break;

                case SensorType.TEMPERATURE:
                    val = Math.Clamp(
                        val,
                        _config.Thresholds.Temperature.Min,
                        _config.Thresholds.Temperature.Max
                    );
                    break;
            }

            return val;
        }



        // Esta aplica los limites

        private double Limit(double value, double max)
        {
            return value > max ? max : value;
        }
    }
}







