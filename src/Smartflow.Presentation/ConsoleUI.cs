using System;
using Smartflow.Domain.Models;
using System.Threading;

namespace Smartflow.Presentation.ConsoleUI
{
  public class ConsoleUI
  {
    private readonly int _progressBarWidth = 30;

    public void DisplayMenu()
    {
      Console.Clear();
      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine("===========================");
      Console.WriteLine("   SISTEMA ETL - SMARTFLOW   ");
      Console.WriteLine("===========================");
      Console.ResetColor();

      Console.WriteLine("1. Procesar datos (Secuencial)");
      Console.WriteLine("2. Procesar datos (Paralelo)");
      Console.WriteLine("3. Comparar rendimiento");
      Console.WriteLine("4. Salir\n");
      Console.Write("Seleccione una opción: ");
    }

    public int ProcessUserInput()
    {
      while (true)
      {
        string? input = Console.ReadLine();

        if (int.TryParse(input, out int option) && option >= 1 && option <= 5)
        {
          return option;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Entrada inválida. Por favor, ingrese un número entre 1 y 5.");
        Console.ResetColor();
        Console.WriteLine("Seleccione una opción: ");
      }
    }

    public void DisplayResults(ProcessedData data)
    {
      Console.Clear();
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("==== RESULTADOS DEL PROCESAMIENTO ====");
      Console.ResetColor();

      Console.WriteLine($"Zona procesada: {data.Zone}");
      Console.WriteLine($"Fecha de procesamiento: {data.ProcessedAt}");
      Console.WriteLine($"Cantidad de registros: {data.RecordCount}");
      Console.WriteLine();
      Console.WriteLine("Estadísticas:");
      Console.WriteLine($"  - Máximo: {data.GetMax()}");
      Console.WriteLine($"  - Mínimo: {data.GetMin()}");
      Console.WriteLine($"  - Promedio: {data.GetAverage()}");

      if (data.Alerts.Count > 0)
      {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nAlertas generadas:");
        Console.ResetColor();

        foreach (var alert in data.Alerts)
        {
          Console.WriteLine($" * [{alert.Type}] {alert.Severity}");
        }
      }
      else
      {
        Console.WriteLine("\nNo se generaron alertas.");
      }

      Console.WriteLine("\nPresione cualquier tecla para continuar...");
      Console.ReadKey();
    }


    // -- Eliminado por Cristhopher Guerrero --  
    // -- Utilidades innecesarias --
  }
}
