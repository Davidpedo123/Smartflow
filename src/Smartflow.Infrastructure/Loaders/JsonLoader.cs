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

        private string SerializeData(ProcessedData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return JsonSerializer.Serialize(data, _options);
        }

        private void EnsureDirectory(string outputPath)
        {
            string? directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Load(ProcessedData data, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path no puede ser vacío.", nameof(outputPath));

            EnsureDirectory(outputPath);

            string json = SerializeData(data);

            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[JsonLoader] Archivo generado: {outputPath}");
        }

        public void LoadBatch(List<ProcessedData> dataList, string outputPath)
        {
            if (dataList == null || dataList.Count == 0)
                throw new ArgumentException("La lista no puede estar vacía.", nameof(dataList));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path no puede ser vacío.", nameof(outputPath));

            EnsureDirectory(outputPath);

            string json = JsonSerializer.Serialize(dataList, _options);

            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[JsonLoader] Archivo batch generado: {outputPath}");
        }
    }
}
