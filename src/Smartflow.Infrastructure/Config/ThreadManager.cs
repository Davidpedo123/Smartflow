using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Smartflow.Domain.Models;



// Template base para el ThreadManager Falta por arreglar la metrica del porcentaje de hilos.
namespace Smartflow.Infrastructure
{
 public class ThreadManager
{
    private readonly Configuration _config;
    private readonly SemaphoreSlim _semaphore;
    private int _activeThreads;
    private readonly object _lock = new object();

    public ThreadManager(Configuration config)
    {
        _config = config;
        _semaphore = new SemaphoreSlim(config.MaxThreads, config.MaxThreads);
        _activeThreads = 0;
    }

    
    public List<Task> CreateTasks(List<Action> actions)
    {
        var tasks = new List<Task>();

        foreach (var action in actions)
        {
            var task = Task.Run(async () =>
            {
                await _semaphore.WaitAsync();  
                try
                {
                    IncrementActiveThreads(); 
                    action();
                }
                finally
                {
                    DecrementActiveThreads(); 
                    _semaphore.Release(); 
                }
            });

            tasks.Add(task);
        }

        return tasks;
    }

    
    public void WaitAllManager(List<Task> tasks)
    {
        try
        {
            Task.WhenAll(tasks.ToArray()).Wait(); 
        }
        catch (AggregateException ae)
        {
           
            Console.WriteLine($"Se produjeron {ae.InnerExceptions.Count} excepciones:");
            foreach (var ex in ae.InnerExceptions)
            {
                Console.WriteLine($"  - {ex.Message}");
            }
            throw;
        }
    }


    public Dictionary<string, object> MetricsThread()
    {
        
        return new Dictionary<string, object>
        {
            ["ActiveThreads"] = _activeThreads,
            ["MaxThreads"] = _config.MaxThreads,
            ["AvailableThreads"] = _semaphore.CurrentCount,
            ["ThreadUtilization"] = (_activeThreads / (double)_config.MaxThreads) * 100
        };
    }


    private void IncrementActiveThreads()
    {
        lock (_lock)
        {
            _activeThreads++;
        }
    }

    private void DecrementActiveThreads()
    {
        lock (_lock)
        {
            _activeThreads--;
        }
    }


    public int GetActiveThreads()
    {
        lock (_lock)
        {
            return _activeThreads;
        }
    }

}

}