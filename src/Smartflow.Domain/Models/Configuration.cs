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

    public static Configuration Load(string path)
    {
      if (!File.Exists(path))
        throw new FileNotFoundException($"No se encontró el archivo de configuración: {path}");

      var json = File.ReadAllText(path);
      return JsonSerializer.Deserialize<Configuration>(json) ?? throw new Exception("Error al cargar archivo de configuracion.");

      // if (loaded == null)
      //   throw new Exception("Error al cargar el archivo de configuración.");
      //
      // Console.WriteLine(loaded.BlockSize);
      //

      // MaxThreads = loaded.MaxThreads;
      // BlockSize = loaded.BlockSize;
      // InputPath = loaded.InputPath;
      // OutputPath = loaded.OutputPath;
      // Strategy = loaded.Strategy;

    }

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
