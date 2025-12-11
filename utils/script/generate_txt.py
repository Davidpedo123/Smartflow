import asyncio
import datetime
import os
import random
from multiprocessing import Process, Queue, cpu_count
import aiofiles
from faker import Faker

fake = Faker()
SENSOR_TYPES = ["NOISE", "TEMPERATURE", "HUMIDITY", "POLLUTION", "TRAFFIC"]

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA_DIR = os.path.join(PROJECT_ROOT, "data", "input")
os.makedirs(DATA_DIR, exist_ok=True)

def generar_valor_realista(sensor_type):
    if sensor_type == "NOISE":
        return round(random.uniform(30, 120), 2)
    elif sensor_type == "TEMPERATURE":
        return round(random.uniform(-10, 45), 2)
    elif sensor_type == "HUMIDITY":
        return round(random.uniform(20, 100), 2)
    elif sensor_type == "POLLUTION":
        return round(random.uniform(50, 600), 2)
    elif sensor_type == "TRAFFIC":
        return round(random.uniform(0, 200), 2)
    return 0


def generar_dato_invalido():
    tipo_error = random.choice([
        'sensor_id_vacio',
        'timestamp_futuro',
        'value_nan',
        'value_infinity',
        'coordenadas_invalidas',
        'tipo_invalido',
        'duplicado',
        'sensor_id_espacios',
        'latitud_fuera_rango',
        'longitud_fuera_rango'
    ])
    
    if tipo_error == 'sensor_id_vacio':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": "",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'sensor_id_espacios':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": "   ",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'timestamp_futuro':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": (datetime.datetime.now() + datetime.timedelta(days=365)).isoformat(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'value_nan':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": float('nan'),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'value_infinity':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": float('inf'),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'coordenadas_invalidas':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": 999.0,
            "longitude": -999.0,
        }
    
    elif tipo_error == 'latitud_fuera_rango':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": random.choice([95.0, -95.0]),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    elif tipo_error == 'longitud_fuera_rango':
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": random.choice([200.0, -200.0]),
        }
    
    elif tipo_error == 'tipo_invalido':
        return {
            "timestamp": fake.iso8601(),
            "sensor_id": f"S{fake.random_int(1,999):03d}",
            "tipo": "INVALID_TYPE",
            "value": round(random.uniform(0, 100), 2),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }
    
    else:
        sensor_type = random.choice(SENSOR_TYPES)
        return {
            "timestamp": "2024-01-01T00:00:00",
            "sensor_id": "S001",
            "tipo": sensor_type,
            "value": generar_valor_realista(sensor_type),
            "latitude": round(random.uniform(-18.55, -18.40), 6),
            "longitude": round(random.uniform(-69.95, -69.85), 6),
        }


def generar_datos_worker(n, queue, worker_id):
    datos = []
    
    for _ in range(n):
        if random.random() < 0.08:
            dato = generar_dato_invalido()
        else:
            sensor_type = random.choice(SENSOR_TYPES)
            dato = {
                "timestamp": fake.iso8601(),
                "sensor_id": f"S{fake.random_int(1, 999):03d}",
                "tipo": sensor_type,
                "value": generar_valor_realista(sensor_type),
                "latitude": round(random.uniform(-18.55, -18.40), 6),
                "longitude": round(random.uniform(-69.95, -69.85), 6),
            }
        datos.append(dato)
    
    queue.put(datos)
    queue.put("DONE")


async def escritor_async(queue, total_workers, filename):
    datos_totales = []
    workers_completados = 0
    
    while workers_completados < total_workers:
        item = await asyncio.to_thread(queue.get)
        if item == "DONE":
            workers_completados += 1
        else:
            datos_totales.extend(item)
    
    filepath = os.path.join(DATA_DIR, filename)
    async with aiofiles.open(filepath, "w") as f:
        for dato in datos_totales:
            if random.random() < 0.02:
                contenido = "CORRUPTED,DATA,LINE,MISSING,FIELDS\n"
            else:
                contenido = (
                    f"{dato['timestamp']},"
                    f"{dato['sensor_id']},"
                    f"{dato['tipo']},"
                    f"{dato['value']},"
                    f"{dato['latitude']},"
                    f"{dato['longitude']}\n"
                )
            await f.write(contenido)
    
    return len(datos_totales)


async def pipeline(total_registros, archivo_numero):
    workers = min(cpu_count(), 4)
    registros_por_worker = max(1, total_registros // workers)
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    filename = f"climate_data_{timestamp}_{archivo_numero:03d}.txt"
    
    queue = Queue()
    procesos = []
    
    for i in range(workers):
        p = Process(target=generar_datos_worker, args=(registros_por_worker, queue, i))
        procesos.append(p)
        p.start()
    
    total_escritos = await escritor_async(queue, workers, filename)
    
    for p in procesos:
        p.join()
    
    return total_escritos


async def main():
    TOTAL_ARCHIVOS = 50
    REGISTROS_POR_ARCHIVO = 10000
    
    print("=" * 60)
    print(f"Iniciando generación de {TOTAL_ARCHIVOS} archivos")
    print(f"Registros por archivo: {REGISTROS_POR_ARCHIVO}")
    print("=" * 60)
    
    inicio = datetime.datetime.now()
    total_generados = 0
    
    for i in range(TOTAL_ARCHIVOS):
        print(f"\n[{i+1}/{TOTAL_ARCHIVOS}] Generando archivo {i+1}...", end=" ")
        registros = await pipeline(REGISTROS_POR_ARCHIVO, i+1)
        total_generados += registros
        print(f"{registros} registros")
        await asyncio.sleep(0.1)
    
    fin = datetime.datetime.now()
    duracion = (fin - inicio).total_seconds()
    
    print("\n" + "=" * 60)
    print(f"✓ COMPLETADO")
    print(f"Total de archivos: {TOTAL_ARCHIVOS}")
    print(f"Total de registros: {total_generados}")
    print(f"Tiempo total: {duracion:.2f} segundos")
    print(f"Registros por segundo: {total_generados/duracion:.0f}")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
