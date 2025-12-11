using Smartflow.Business.Metrics;
using Smartflow.Domain;
using Smartflow.Infrastructure.Config;
using Smartflow.Infrastructure.Loaders;
using Smartflow.Domain.Enums;
using Smartflow.Domain.Models;
using Smartflow.Business.Metrics;
using Smartflow.Business.Validation;
using Smartflow.Business.Rules;
using Smartflow.DataAccess.Extractors;
using System.Diagnostics;
using Xunit.Abstractions;
using Xunit;
using Smartflow.Business.ETL;

public class ETLParallel
{
    private readonly ITestOutputHelper output;
    public ETLParallel(ITestOutputHelper output)
    {
        this.output = output;
    }


    [Fact]
    public void MetricsValue()


    {


        ConfigurationManager configManager = ConfigurationManager.Instance;

        string configPath = Path.GetFullPath("../../data/config/settings.json");

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

        var MetricsCalculator = new MetricsCalculator(coordinator);
        var files = Directory.GetFiles(config.InputPath, "*.txt").ToList();
        //var stopwatch = Stopwatch.StartNew();
        var result = MetricsCalculator.MeasureParallel(files);
        //var resultSec = MetricsCalculator.MeasureSequential(files);





        output.WriteLine($"EL -----------------------ParallelTime: {result.ParallelTime:F3}");
        //output.WriteLine($"-------------Eficiencia {}")
        //output.WriteLine($"El------------Secuencia : {}")
        //output.WriteLine($"EL -----------------------Secuencial: {resultSec.SequentialTime:F3}");
        //Assert.Equal('',)

        Assert.IsType<double>(result.ParallelTime);



    }



}