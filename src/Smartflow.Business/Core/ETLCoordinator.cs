using Smartflow.Domain.Interfaces;
using Smartflow.Domain.Models;
using Smartflow.Business.Validation;
using Smartflow.Infrastructure.Config;
using System.Diagnostics;
using Smartflow.Domain.Enums;
namespace Smartflow.Business;


// Plantilla Base para el Coordinador..
public class ETLCoordinator
{
    private readonly IDataExtractor _extractor;
    private readonly IDataLoader _loader;
    private readonly RuleValidator _validator;
    private readonly ConfigurationManager _configManager;

    public ETLCoordinator(
        IDataExtractor extractor,
        IDataLoader loader,
        RuleValidator validator,
        ConfigurationManager configManager)
    {
        _extractor = extractor;
        _loader = loader;
        _validator = validator;
        _configManager = configManager;
    }

    
    public Metrics ProcessData(string inputPath)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Console.WriteLine("=== Iniciando Proceso ETL Secuencial ===\n");
            
            // 1. EXTRACT
            
            Console.WriteLine("1. Extrayendo datos...");
            var rawData = _extractor.Extract(inputPath);
            Console.WriteLine($"   {rawData.Count} registros extraídos\n");
            
            
            // 2. TRANSFORM
            Console.WriteLine("2. Transformando datos...");
            
            
            
            // 2.1 Limpiar
            var cleanData = CleanData(rawData);
            Console.WriteLine($"   ✓ {cleanData.Count} registros limpios");
            
            
            
            
            // 2.2 Validar reglas
            var validatedData = new List<SensorData>();
            foreach (var data in cleanData)
                {
                    var alerts = _validator.ValidateData(data);
                    if (alerts.Count == 0 || alerts.All(a => a.Severity != AlertSeverity.CRITICAL))
                    {
                        validatedData.Add(data);
                    }
                }
            Console.WriteLine($"   ✓ {validatedData.Count} registros validados");
            
            
            
            var groupedByZone = GroupByZone(validatedData);
            Console.WriteLine($"   ✓ {groupedByZone.Count} zonas identificadas\n");
            
            // 3. LOAD
            Console.WriteLine("3. Cargando datos...");
            foreach (var zone in groupedByZone)
            {
                var stats = CalculateStatistics(zone.Value, zone.Key);
                var processedData = new ProcessedData
                {
                    Zone = zone.Key,
                    ProcessedAt = DateTime.Now,
                    RecordCount = zone.Value.Count,
                    Statistics = stats,
                    Alerts = new List<Alert>()
                };
                
                _loader.Load(processedData, $"output/zone_{zone.Key}.json");
            }
            Console.WriteLine($"   ✓ Datos cargados correctamente\n");
            
            stopwatch.Stop();
            
            Console.WriteLine($"=== Proceso completado en {stopwatch.ElapsedMilliseconds}ms ===\n");
            
            return new Metrics
            {
                SequentialTime = stopwatch.Elapsed.TotalMilliseconds,
                
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error en proceso ETL: {ex.Message}");
            throw;
        }
    }

    // Limpiar datos
    private List<SensorData> CleanData(List<SensorData> data)
    {
        var cleaned = data
            .Where(d => d.IsValid())                   
            .GroupBy(d => d.SensorId + d.Timestamp)     
            .Select(g => g.First())                     
            .ToList();
        
        return cleaned;
    }

    // Calcular estadísticas por zona
    private Dictionary<string, double> CalculateStatistics(List<SensorData> data, string zone)
    {
        if (!data.Any()) return new Dictionary<string, double>();
        
        var stats = new Dictionary<string, double>
        {
            ["Average"] = data.Average(d => d.Value),
            ["Max"] = data.Max(d => d.Value),
            ["Min"] = data.Min(d => d.Value),
            ["Count"] = data.Count
        };
        
        return stats;
    }

    
    private Dictionary<string, List<SensorData>> GroupByZone(List<SensorData> data)
    {
        // Agrupar por coordenadas redondeadas
        var grouped = data
            .GroupBy(d => $"{Math.Round(d.Latitude, 1)}_{Math.Round(d.Longitude, 1)}")
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
        
        return grouped;
    }
}