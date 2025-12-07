using System.Text.Json;
using System.Text.Json.Serialization;
using Smartflow.Domain.Models;

namespace Smartflow.Business.Export;

public class AlertExporter
{
  public static string Export(List<Alert> alerts, string outputDirectory)
  {
    if (!Directory.Exists(outputDirectory))
      Directory.CreateDirectory(outputDirectory);

    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    string fileName = $"alerts_{timestamp}.json";
    string fullPath = Path.Combine(outputDirectory, fileName);

    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    var wrapper = new { alerts };

    string json = JsonSerializer.Serialize(wrapper, options);
    File.WriteAllText(fullPath, json);

    return fullPath;
  }
}
