using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Smartflow.Domain
{
    public enum ParallelizationStrategy
    {
        DEFAULT
    }

    public class Configuration
    {
        public int MaxThreads { get; set; }
        public int BlockSize { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public ParallelizationStrategy Strategy { get; set; }

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"No se encontró el archivo de configuración: {path}");

            string json = File.ReadAllText(path);

         
            var loaded = JsonSerializer.Deserialize<Configuration>(json);

            if (loaded == null)
                throw new Exception("Error al cargar el archivo de configuración.");

            MaxThreads = loaded.MaxThreads;
            BlockSize = loaded.BlockSize;
            InputPath = loaded.InputPath;
            OutputPath = loaded.OutputPath;
            Strategy = loaded.Strategy;

        }

        public void Validate()
        {
            Console.WriteLine("Es válido");
        }
    }
}