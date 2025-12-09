using Smartflow.Presentation.ConsoleUI;

class Program
{
    static void Main(string[] args)
    {
        var ui = new ConsoleUI();
        bool exit = false;

        while (!exit)
        {
            ui.DisplayMenu();
            int option = ui.ProcessUserInput();

            switch (option)
            {
                case 1:
                    Console.WriteLine("Procesar datos (Secuencial)");
                    Console.ReadKey();
                    break;

                case 2:
                    Console.WriteLine("Procesar datos (Paralelo)");
                    Console.ReadKey();
                    break;

                case 3:
                    Console.WriteLine("Comparar rendimiento");
                    Console.ReadKey();
                    break;

                case 4:
                    Console.WriteLine("Ver métricas");
                    Console.ReadKey();
                    break;

                case 5:
                    exit = true;
                    Console.WriteLine("Saliendo...");
                    break;
            }
        }
    }
}
