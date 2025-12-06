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

    public bool Validate()
    {
      if (MaxThreads <= 0) return false;
      if (BlockSize <= 0) return false;
      if (string.IsNullOrWhiteSpace(InputPath)) return false;
      if (string.IsNullOrWhiteSpace(OutputPath)) return false;
      if (!Enum.IsDefined<ParallelizationStrategy>(Strategy)) return false;

      return true;
    }
  }
}
