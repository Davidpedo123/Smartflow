## Generador de Datos de Sensores

Este script simula la generación de datos de sensores ambientales (ruido, temperatura, humedad, contaminación, tráfico) y los guarda en archivos de texto. Utiliza concurrencia para mejorar el rendimiento, dividiendo el trabajo de generación de datos entre múltiples procesos y escribiéndolos asíncronamente.

## Requisitos

## Instalar los siguientes paquetes de Python:

`pip install aiofiles faker asyncio`

## Descripción

 Generación de Datos: Se crean registros aleatorios con marca de tiempo, tipo de sensor, valor y coordenadas geográficas.

## Los datos se guardan en archivos de texto con el siguiente formato:

`timestamp,sensor_id,tipo,value,latitude,longitude`

Ejecución

Ejecuta el script desde la línea de comandos:

`python generate_txt.py`


## CUIDADO

`por defult el ejecuta el script usando todos los nucleos logicos disponibles si hay mas datos que nucleso`

para cambiar eso puede elegir manualmente en:

```python

async def pipeline(total):
    workers = min(cpu_count(), total)  #Quitando cpu_count() y agregandole un valor fijo
    datos_por_worker = max(1, total // workers)

```

```python

async def pipeline(total):
    workers = min(4, total)   
    datos_por_worker = max(1, total // workers)

```

Por defecto, generará 500 registros por ejecución, 30 veces.

## Ajustes

Estos son los tipos de sensores 

```python
SENSOR_TYPES = ["NOISE", "TEMP", "HUMIDITY", "POLLUTION", "TRAFFIC"]
```

Aqui se definen los valores random

```python

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
```
Para cambiar los rangos seria cambiar los valores de random.unifomr, ejemplo random.uniform(-50, 1000) dice que los rangos creados seran desde el -50 hasta el 1000

Cantidad de datos

```python

if __name__ == "__main__":
    for i in range(30):
        asyncio.run(pipeline(500)) 

```

el valor que le pasamos a pipeline() es la cantidad de registros que crea por archivos, y el range(30) es la cantidad es la cantidad de veces que ejecutamos la funcion la funcion se ejecuta por cantidad de worker, ejemplo si el total de cpu logico es 16, el script genera 16 archivos de 500 registros

### Archivos de Salida

Los archivos se guardan en el directorio data con el siguiente formato de nombre:

climate_data_YYYYMMDD_HHMMSS_microseconds.txt

```txt
2006-06-05T02:07:17,S028,TEMP,98.88,18.540631,-69.865302
1993-05-16T20:56:34,S882,TEMP,79.16,18.429684,-69.881991
2018-12-05T23:26:26,S790,NOISE,45.75,18.427107,-69.874223
1984-03-01T13:04:04,S363,NOISE,63.83,18.471433,-69.9301
1985-10-08T17:29:06,S044,NOISE,14.36,18.518834,-69.895357
1975-03-11T05:48:02,S123,TEMP,36.77,18.465606,-69.872775
2001-05-20T15:12:59,S820,NOISE,26.47,18.405888,-69.879473
2002-07-27T16:30:44,S358,HUMIDITY,42.09,18.457092,-69.916922
2015-08-15T20:51:03,S311,TEMP,17.3,18.413164,-69.892562

```