import asyncio
import aiofiles
from multiprocessing import Process, Queue, cpu_count
from faker import Faker
import random
import datetime
import os

fake = Faker()
SENSOR_TYPES = ["NOISE", "TEMPERATURE", "HUMIDITY", "POLLUTION", "TRAFFIC"]

def generar_datos_worker(n, queue):
    """Genera n datos y los pone en la cola."""
    print(f"[WORKER PID {os.getpid()}] Iniciando worker para generar {n} datos")

    datos = []  

    for _ in range(n):
        dato = {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": random.choice(SENSOR_TYPES),
            "value": round(random.uniform(-50, 1000), 2),
            "latitude": round(random.uniform(-18.40, -18.55), 6),
            "longitude": round(random.uniform(69.95, -69.85), 6)
        }
        datos.append(dato)

    queue.put(datos)  
    queue.put("DONE")  

async def escritor_async(queue, total_workers):
    print("[WRITER] Iniciando escritor async...")

    terminados = 0
    os.makedirs("data", exist_ok=True)

    while terminados < total_workers:
        datos = queue.get()
        print("[WRITER] Recibiendo datos...")

        if datos == "DONE":
            terminados += 1
            continue

        
        filename = f"data/climate_data_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S_%f')}.txt"

        
        async with aiofiles.open(filename, "w") as f:
            for dato in datos:
                contenido = (
                    f"{dato['timestamp']},"
                    f"{dato['sensor_id']},"
                    f"{dato['tipo']},"
                    f"{dato['value']},"
                    f"{dato['latitude']},"
                    f"{dato['longitude']}\n"
                )
                await f.write(contenido)

    print("[WRITER] Finalizado")

async def pipeline(total):
    workers = min(cpu_count(), total)   
    datos_por_worker = max(1, total // workers)

    print(f"[MAIN] total={total}, workers={workers}, datos_por_worker={datos_por_worker}")

    queue = Queue()

    procesos = [
        Process(target=generar_datos_worker, args=(datos_por_worker, queue))
        for _ in range(workers)
    ]

    for p in procesos:
        p.start()

    await escritor_async(queue, workers)

    for p in procesos:
        p.join()

    print(f"Generados y guardados: {total} archivos")

if __name__ == "__main__":
    for i in range(30):
        asyncio.run(pipeline(500)) 
