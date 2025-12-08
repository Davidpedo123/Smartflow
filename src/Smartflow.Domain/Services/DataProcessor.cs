using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartflow.Domain.Services
{
    public class DataProcessor
    {
        private readonly Configuration _config;

        public DataProcessor(Configuration config)
        {
            _config = config;
        }

        // ============================================================
        // PROCESAR BLOQUE COMPLETO
        // ============================================================
        public List<SensorData> Process(List<SensorData> block)
        {
            List<SensorData> cleaned = Clean(block);
            List<SensorData> validated = Validate(cleaned);
            List<SensorData> transformed = Transform(validated);

            return transformed;
        }

        // ============================================================
        // 1. LIMPIEZA DE DATOS
        // ============================================================
        private List<SensorData> Clean(List<SensorData> block)
        {
            return block
                .Where(s => s != null)
                .Where(s => !double.IsNaN(s.Value) && !double.IsInfinity(s.Value))
                .ToList();
        }

        // ============================================================
        // 2. VALIDACIÓN SEGÚN TU MÉTODO IsValid()
        // ============================================================
        private List<SensorData> Validate(List<SensorData> block)
        {
            return block
                .Where(s => s.IsValid())
                .ToList();
        }

        // ============================================================
        // 3. TRANSFORMACIONES Y REGLAS DE NEGOCIO
        // ============================================================
        private List<SensorData> Transform(List<SensorData> block)
        {
            List<SensorData> output = new();

            foreach (var sensor in block)
            {
                var clone = sensor.Clone();

                // Ejemplo: normalización dependiendo del tipo
                clone.Value = ApplyRules(clone);

                output.Add(clone);
            }

            return output;
        }

        // ============================================================
        // REGLAS SEGÚN THRESHOLDS
        // ============================================================
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

        // ============================================================
        // UTILIDAD PARA LIMITES
        // ============================================================
        private double Limit(double value, double max)
        {
            if (value > max)
                return max;

            return value;
        }
    }
}
