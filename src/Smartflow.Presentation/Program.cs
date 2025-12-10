using Smartflow.Business.ETL;
using Smartflow.Business.Metrics;
using Smartflow.Business.Rules;
using Smartflow.Business.Validation;
using Smartflow.DataAccess.Extractors;
using Smartflow.Infrastructure.Config;
using Smartflow.Infrastructure.Loaders;
using Smartflow.Presentation.ConsoleUI;

class Program
{
    static void Main(string[] args)
    {
        // UI
        var ui = new ConsoleUI();
        bool exit = false;


        var configManager = ConfigurationManager.Instance;
        var configPath = Path.GetFullPath("../../data/config/settings.json");
        configManager.Load(configPath);

        var config = configManager.GetConfiguration();
        var validator = new RuleValidator();

        validator.AddRule("Noise", new NoiseRule(100));
        validator.AddRule("Temperature", new TemperatureRule(10, 40));
        validator.AddRule("Humidity", new HumidityRule(30, 75));
        validator.AddRule("Traffic", new TrafficRule(120));
        validator.AddRule("Pullution", new PollutionRule(200));

        ETLCoordinator coordinator = new ETLCoordinator(
            new TxtExtractor(),
            new JsonLoader(),
            validator,
            config
            );

        string inputDirectory = config.InputPath;

        if (!Directory.Exists(inputDirectory))
        {
            Console.WriteLine($"Directorio no encontrado: {inputDirectory}");
            return;
        }

        var metricsCalculator = new MetricsCalculator(coordinator);
        var files = Directory.GetFiles(inputDirectory, "*.txt").ToList();
        Console.WriteLine($"Files {files.Count}");

        while (!exit)
        {
            ui.DisplayMenu();
            int option = ui.ProcessUserInput();

            switch (option)
            {
                case 1:
                    Console.WriteLine("Procesar datos (Secuencial)");
                    var sequentialMetrics = metricsCalculator.MeasureSequential(files);
                    Console.ReadKey();
                    break;

                case 2:
                    Console.WriteLine("Procesar datos (Paralelo)");
                    var sequentialParallel = metricsCalculator.MeasureParallel(files);
                    Console.ReadKey();
                    break;

                case 3:
                    Console.WriteLine("Comparar rendimiento");
                    var compareParallel = metricsCalculator.ComparePerformance(files, config.MaxThreads);
                    Console.ReadKey();
                    break;

                case 4:
                    Console.WriteLine("Saliendo...");
                    exit = true;
                    break;

            }
        }
    }
}