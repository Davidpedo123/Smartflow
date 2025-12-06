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
      if (!File.Exists(configPath))
        throw new FileNotFoundException($"No se encontro: {configPath}");

      var json = File.ReadAllText(configPath);
      var loaded = JsonSerializer.Deserialize<Configuration>(json)
        ?? throw new Exception("Error deserializando configuracion.");

      ApplyDefaults(loaded);

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

    private static void ApplyDefaults(Configuration cfg)
    {

      if (cfg.MaxThreads is <= 0 or > 32)
        cfg.MaxThreads = 4;

      if (cfg.BlockSize <= 0)
        cfg.BlockSize = 25_000;

      if (string.IsNullOrWhiteSpace(cfg.InputPath))
        cfg.InputPath = ResolveProjectPath("data/input/");

      if (string.IsNullOrWhiteSpace(cfg.OutputPath))
        cfg.OutputPath = ResolveProjectPath("data/output/result.json");

      if (!Enum.IsDefined<ParallelizationStrategy>(cfg.Strategy))
      {
        cfg.Strategy = ParallelizationStrategy.DATA_DECOMPOSITION;
      }
    }

    private static string ResolveProjectPath(string relativePath)
    {
      var root = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent;

      return Path.Combine(root!.FullName, relativePath);
    }
  }
}
