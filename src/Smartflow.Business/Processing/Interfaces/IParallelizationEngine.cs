using System.Collections.Generic;
using Smartflow.Domain.Models;
using Smartflow.Domain.Enums;

namespace Smartflow.Business.Processing.Interfaces
{
    public interface IParallelizationEngine
    {
        /// Procesa los datos en paralelo usando la estrategia configurada.
        List<ProcessedData> ProcessInParallel(List<SensorData> data);

        /// Particiona los datos en bloques para procesamiento paralelo.
        List<List<SensorData>> PartitionData(List<SensorData> data);

        /// Procesa un bloque individual.
        ProcessedData ProcessBlock(List<SensorData> block, int blockId);

        /// Agrega los resultados parciales en un resultado global.
        List<ProcessedData> AggregateResults(List<ProcessedData> results);

        /// Cambia la estrategia de paralelización en tiempo de ejecución.
        void ConfigureStrategy(ParallelizationStrategy strategy);
    }
}
