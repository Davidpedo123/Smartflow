using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
namespace Smartflow.DataAccess.Extractors;

public class TxtExtractor : IDataExtractor
{

  public List<SensorData> Extract(string filePath)
  {
    var result = new List<SensorData>();

    if (!File.Exists(filePath))
      throw new FileNotFoundException($"El archivo no existe: {filePath}");

    var lines = File.ReadAllLines(filePath);

    foreach (var line in lines)
    {
      if (string.IsNullOrWhiteSpace(line))
        continue;

      try
      {
        var data = ParseLine(line);
        result.Add(data);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[WARN] Registro invalido: {line}");
        Console.WriteLine($"Motivo: {ex.Message}");
      }
    }

    return result;
  }

  public List<SensorData> ExtractBatch(List<string> filePaths)
  {
    var allSensors = new List<SensorData>();
    foreach (var path in filePaths)
    {
      allSensors.AddRange(Extract(path));
    }
    return allSensors;
  }

  private SensorData ParseLine(string line)
  {
    var parts = line.Split(',');

    if (parts.Length != 6)
      throw new FormatException("La linea no tiene 6 columnas");

    return new SensorData
    {
      Timestamp = DateTime.Parse(parts[0]),
      SensorId = parts[1],
      Type = Enum.Parse<SensorType>(parts[2], ignoreCase: true),
      Value = double.Parse(parts[3]),
      Latitude = double.Parse(parts[4]),
      Longitude = double.Parse(parts[5])
    };
  }
}
