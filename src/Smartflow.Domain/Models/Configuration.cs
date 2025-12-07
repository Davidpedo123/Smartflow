using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Smartflow.Domain.Enums;

namespace Smartflow.Domain.Models
{
  public class Configuration
  {
    public int MaxThreads { get; set; }
    public int BlockSize { get; set; }
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public ParallelizationStrategy Strategy { get; set; }

    public Thresholds Thresholds { get; set; } = new Thresholds();

    public bool Validate()
    {
      if (MaxThreads <= 0) return false;
      if (BlockSize <= 0) return false;
      if (string.IsNullOrWhiteSpace(InputPath)) return false;
      if (string.IsNullOrWhiteSpace(OutputPath)) return false;
      if (!Enum.IsDefined<ParallelizationStrategy>(Strategy)) return false;

      if (Thresholds == null || !Thresholds.Validate()) return false;

      return true;
    }
  }

  public class Thresholds
  {
    public double Noise { get; set; }
    public double Traffic { get; set; }
    public double CO2 { get; set; }
    public TemperatureThreshold Temperature { get; set; } = new TemperatureThreshold();

    public bool Validate()
    {
      if (Noise < 0) return false;
      if (Traffic < 0) return false;
      if (CO2 < 0) return false;
      if (Temperature == null || !Temperature.Validate()) return false;

      return true;
    }
  }

  public class TemperatureThreshold
  {
    public double Min { get; set; }
    public double Max { get; set; }

    public bool Validate()
    {
      return Min <= Max;
    }
  }
}
