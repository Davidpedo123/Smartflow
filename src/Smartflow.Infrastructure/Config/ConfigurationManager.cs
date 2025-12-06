using System.Text.Json;
using Smartflow.Domain.Enums;
using Smartflow.Domain.Models;

namespace Smartflow.Infrastructure.Config
{
  public class ConfigurationManager
  {
    private static ConfigurationManager? _instance;
    public Configuration? Config { get; private set; }

    private ConfigurationManager() { }

    public static ConfigurationManager Instance => _instance ??= new ConfigurationManager();

    public void Load(string configPath)
    {
      var dir = Path.GetDirectoryName(configPath);

      if (dir is null or "")
        dir = Directory.GetCurrentDirectory();

      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);


      configPath = Path.Combine(dir, Path.GetFileName(configPath));

      if (!File.Exists(configPath))
      {
        var defaultConfig = new Configuration
        {
          MaxThreads = 4,
          BlockSize = 1000,
          InputPath = ResolveProjectPath("data/input/"),
          OutputPath = ResolveProjectPath("data/output/"),
          Strategy = ParallelizationStrategy.DATA_DECOMPOSITION
        };

        var jsonDefault = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
        {
          WriteIndented = true
        });

        File.WriteAllText(configPath, jsonDefault);
        Config = defaultConfig;
        return;
      }

      var json = File.ReadAllText(configPath);
      var loaded = JsonSerializer.Deserialize<Configuration>(json)
        ?? throw new Exception("Error deserializando configuracion.");

      if (!loaded.Validate())
        throw new Exception("Configuracion invalida.");

      Config = loaded;

      Save(configPath, loaded);
    }

    public Configuration GetConfiguration()
    {
      return Config ?? throw new InvalidOperationException(
          "ConfigurationManager.Load() debe ser llamado antes de solicitar la configuracion."
          );
    }

    private static void Save(string path, Configuration cfg)
    {
      var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions
      {
        WriteIndented = true
      });

      File.WriteAllText(path, json);
    }

    private static string ResolveProjectPath(string relativePath)
    {
      var root = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent;

      return Path.Combine(root!.FullName, relativePath);
    }
  }
}
