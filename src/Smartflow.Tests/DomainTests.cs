using Smartflow.Domain;
using Smartflow.Domain.Enums;
using Smartflow.Domain.Models;
using Xunit;
namespace Smartflow.Tests;


/*

La mayoría de las pruebas unitarias siguen el patrón AAA (Arrange-Act-Assert):

Arrange: Preparar los datos y condiciones necesarias para la prueba.
Act: Ejecutar la funcionalidad que queremos probar.
Assert: Verificar que el resultado es el esperado.


public class MiClaseTests
{
    [Fact]
    public void MiMetodo_Escenario_ResultadoEsperado()
    {
        // Arrange - Preparar
        
        // Act - Actuar
        
        // Assert - Verificar
    }
}



*/
public class UnitTest1
{

        [Fact]
        public void PruebaDeConfiguracion()
        {
            
            Assert.True(true);
        }

       



}
public class DomainTestsSensorData
   
{
    SensorData sensor = new SensorData
            {
                SensorId = "S123",           
                Timestamp = DateTime.UtcNow,  
                Type = SensorType.TEMPERATURE,
                Value = -25.5,                 
                Longitude = 74,         
                Latitude = -40.7128            
            };
    
    
    [Fact]

    //Prueba si el metodo IsValid actua de la forma correcta cuando algo sale bien
    public void TestIsValid()
    {
        
        bool result = sensor.IsValid();
        Assert.True(result);
    }


    [Fact]

    //Prueba si el metodo IsValid actua de la forma correcta cuando algo sale mal
    public void TestIsValid_SensorIdVacio()
        {
            // Arrange
            SensorData sensor = new SensorData
            {
                SensorId = "",   
                Timestamp = DateTime.UtcNow,
                Type = SensorType.TEMPERATURE,
                Value = 25.5,
                Longitude = -74.0060,
                Latitude = 40.7128
            };

            // Act
            bool result = sensor.IsValid();

            // Assert
            Assert.False(result);  
        }


    // Prueba 1: Clonación de un objeto válido
        [Fact]
        public void TestClone_Valido()
        {
            // Arrange
            SensorData original = new SensorData
            {
                SensorId = "S123",           
                Timestamp = DateTime.UtcNow,  
                Type = SensorType.TEMPERATURE,
                Value = 25.5,                 
                Longitude = -74.0060,         
                Latitude = 40.7128            
            };

            // Act
            SensorData clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);  
            Assert.Equal(original.SensorId, clone.SensorId);  
            Assert.Equal(original.Timestamp, clone.Timestamp);  
            Assert.Equal(original.Value, clone.Value); 
            Assert.Equal(original.Longitude, clone.Longitude);  
            Assert.Equal(original.Latitude, clone.Latitude);  
        }

        // Prueba 2: Clonación de un objeto con valores modificados
        [Fact]
        public void TestClone_Modificado()
        {
            // Arrange
            SensorData original = new SensorData
            {
                SensorId = "S123",           
                Timestamp = DateTime.UtcNow,  
                Type = SensorType.TEMPERATURE,
                Value = 25.5,                 
                Longitude = -74.0060,         
                Latitude = 40.7128            
            };

            // Act
            SensorData clone = original.Clone();

            
            clone.SensorId = "S124";
            clone.Value = 30.0;

            // Assert
            Assert.NotEqual(original.SensorId, clone.SensorId);  
            Assert.NotEqual(original.Value, clone.Value);  
            Assert.Equal(original.Timestamp, clone.Timestamp);  
            Assert.Equal(original.Longitude, clone.Longitude);  
            Assert.Equal(original.Latitude, clone.Latitude);  
        }

    

    

}


public class ProcessDataTest
{

    

    [Fact]
    public void GetMaxTest()
    {



        var processeddata = new ProcessedData();
        processeddata.Statistics["max"] = 100.0;

        double result = processeddata.GetMax();

        Assert.Equal(100.0, result);
    }


    [Fact]
    public void GetMax_ReturnsZero_WhenMaxDoesNotExist()
        {
            
            var processedData = new ProcessedData(); 

            
            double result = processedData.GetMax();

            
            Assert.Equal(0, result); 
        }

    [Fact]
        public void GetMin_ReturnsCorrectValue_WhenMinExists()
        {
            
            var processedData = new ProcessedData();
            processedData.Statistics["min"] = -50.0;

           
            double result = processedData.GetMin();

            
            Assert.Equal(-50.0, result); 
        }
    
    [Fact]
        public void GetAverage_ReturnsCorrectValue()
        {
            
            var processedData = new ProcessedData();
            processedData.Statistics["avg"] = 75.5;

            
            double result = processedData.GetAverage();

            
            Assert.Equal(75.5, result); 
        }

    

    
}

public class MetricsTests
{
    Metrics metrics = new Metrics
    
    {
        SequentialTime = 1000,
        ParallelTime = 500
    };

    

    [Fact]
    public void CalculateTest()
    {
        metrics.Calculate();
        Console.WriteLine(metrics.Efficiency);
        Console.WriteLine(metrics.Speedup);
        Assert.Equal(2,metrics.Speedup);
        Assert.Equal(2, metrics.Efficiency);
    }


    [Fact]

    
    public void ToJsonTest()
    {

        Metrics metricsJson = new Metrics
        {
            SequentialTime = 1000,
            ParallelTime = 1000,
            Speedup = 1,
            Efficiency = 1
        };
        metricsJson.ToJson();
        string json = metricsJson.ToJson();
        Assert.Contains("\"SequentialTime\": 1000", json);  
        Assert.Contains("\"ParallelTime\": 1000", json);   
        Assert.Contains("\"Speedup\": 1", json);           
        Assert.Contains("\"Efficiency\": 1", json); 

    } 
}

public class ConfigurationModelTest
{
    
    [Fact]

    
    public void ValidateFalseTest()
    {
        Configuration conf = new Configuration
        {
            MaxThreads = -1,
            BlockSize = -1,
            InputPath = "",
            OutputPath = "",
        
        };

        bool resul = conf.Validate();
        Assert.False(resul);


    }

    [Fact]
    public void ValidateTrueTest()
    {
        Configuration conf = new Configuration
        {
            MaxThreads = 1,
            BlockSize = 1,
            InputPath = "/",
            OutputPath = "/",
        
        };

        bool resul = conf.Validate();
        Assert.True(resul);


    }

    [Fact]

    public void ThresholdsFalseValidateTest()
    {
        TemperatureThreshold temp = new TemperatureThreshold
        {
            Min = 20,
            Max = 30
        };

        Thresholds th = new Thresholds
        {
            Noise = -1,
            Traffic = -1,
            CO2 = -1,
            Temperature = temp
        };

        bool result = th.Validate();
        Assert.False(result);

    }

    [Fact]
    public void ThresholdsTrueValidateTest()
    {
        TemperatureThreshold temp = new TemperatureThreshold
        {
            Min = 20,
            Max = 30
        };

        Thresholds th = new Thresholds
        {
            Noise = 10,
            Traffic = 10,
            CO2 = 10,
            Temperature = temp
        };

        bool result = th.Validate();
        Assert.True(result);

    }
}