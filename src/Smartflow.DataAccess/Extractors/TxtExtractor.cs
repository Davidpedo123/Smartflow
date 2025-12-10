using Smartflow.Domain.Enums;
using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;

namespace Smartflow.DataAccess.Extractors;

public class TxtExtractor : IDataExtractor
{
  // Mantén tu método Extract original si quieres, pero agrega este:
  public IEnumerable<SensorData> ExtractStream(string filePath)
  {
    if (!File.Exists(filePath)) yield break;

    // StreamReader lee el disco poco a poco, no carga todo en RAM
    using var reader = new StreamReader(filePath);
    while (!reader.EndOfStream)
    {
      var line = reader.ReadLine();
      if (string.IsNullOrWhiteSpace(line)) continue;

      SensorData? data = null;
      try 
      {
        // Reutilizamos tu lógica de parseo existente
        data = ParseLine(line); 
      }
      catch 
      { 
        // Log simple para no romper el flujo
        continue; 
      }

      if (data != null)
        yield return data; // <--- ¡AQUÍ ESTÁ EL TRUCO! Entrega el dato y sigue
    }
  }

  // ... (Mantén tu método ParseLine privado igual que antes) ...
  public List<SensorData> Extract(string filePath) => ExtractStream(filePath).ToList();
  public List<SensorData> ExtractBatch(List<string> filePaths) => throw new NotImplementedException(); 
  
  private SensorData ParseLine(string line)
  {
      var parts = line.Split(',');
      if (parts.Length != 6) throw new FormatException("Formato incorrecto");

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
