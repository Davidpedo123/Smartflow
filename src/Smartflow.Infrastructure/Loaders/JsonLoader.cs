using System.Text.Json;
using Smartflow.Domain.Models;
using Smartflow.Domain.Interfaces;

namespace Smartflow.Infrastructure.Loaders
{
    public class JsonLoader : IDataLoader
    {
        private readonly JsonSerializerOptions _options;

        public JsonLoader()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,                 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };
        }

        public string SerializeData(ProcessedData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return JsonSerializer.Serialize(data, _options);
        }

        public void Load(ProcessedData data, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path no puede ser vacío.", nameof(outputPath));

            string json = SerializeData(data);

            string? directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[JsonLoader] Archivo generado: {outputPath}");
        }

        public void LoadBatch(List<ProcessedData> dataList, string outputPath)
        {
            if (dataList == null || dataList.Count == 0)
                throw new ArgumentException("La lista no puede estar vacía.", nameof(dataList));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path no puede ser vacío.", nameof(outputPath));

            string json = JsonSerializer.Serialize(dataList, _options);

            string? directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[JsonLoader] Archivo batch generado: {outputPath}");
        }
    }
}
