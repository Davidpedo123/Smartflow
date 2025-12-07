using Smartflow.Infrastructure;
using Smartflow.Infrastructure.Config;
using Smartflow.Domain.Enums;
using Xunit;
namespace Smartflow.Tests;

public class UnitTest2
{

        [Fact]
        public void PruebaDeConfiguracion()
        {
            
            Assert.True(true);
        }

       



}



public class ConfigurationManagerTests
{
    [Fact]

    public void Load_Configuration_if_filepath_isnull()
    {
        
        string configPath = "rutaErronea";

        
        ConfigurationManager.Instance.Load(configPath);

      
        var config = ConfigurationManager.Instance.GetConfiguration();

        
        Assert.Equal(4, config.MaxThreads); 
        Assert.Equal(1000, config.BlockSize); 
        Assert.Equal(ResolveProjectPath("data/input/"), config.InputPath); 
        Assert.Equal(ResolveProjectPath("data/output/"), config.OutputPath); 
        Assert.Equal(ParallelizationStrategy.DATA_DECOMPOSITION, config.Strategy);



        Assert.Equal(70, config.Thresholds.Noise); 
        Assert.Equal(80, config.Thresholds.Traffic); 
        Assert.Equal(400, config.Thresholds.CO2); 

        
        Assert.Equal(15, config.Thresholds.Temperature.Min); 
        Assert.Equal(35, config.Thresholds.Temperature.Max); 
    }

    [Fact]
    public void Load_Configuration_IfFile_exists()
    {
        
        string configPath = ResolveProjectPath("data/config/settings.json");

        
        ConfigurationManager.Instance.Load(configPath);

      
        var config = ConfigurationManager.Instance.GetConfiguration();

        
        Assert.Equal(8, config.MaxThreads); 
        Assert.Equal(2000, config.BlockSize); 
        Assert.Equal(ResolveProjectPath("data/input/"), config.InputPath); 
        Assert.Equal(ResolveProjectPath("data/output/"), config.OutputPath); 
        Assert.Equal(ParallelizationStrategy.DATA_DECOMPOSITION, config.Strategy);



        Assert.Equal(70, config.Thresholds.Noise); 
        Assert.Equal(80, config.Thresholds.Traffic); 
        Assert.Equal(400, config.Thresholds.CO2); 

        
        Assert.Equal(15, config.Thresholds.Temperature.Min); 
        Assert.Equal(35, config.Thresholds.Temperature.Max); 
    }

    private static string ResolveProjectPath(string relativePath)
    {
      var root = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent;

      return Path.Combine(root!.FullName, relativePath);
    }
}