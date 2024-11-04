
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonVSCSharp
{
    public class DataProcessor
    {
        private SqlHandler _sqlHandler;
        private Translator _translator;
        private int _maxThreads;  // Máximo de hilos
        private Queue<List<dynamic>> _chunksQueue;  // Cola para manejar chunks pendientes
        private SemaphoreSlim _semaphore;  // Semáforo para limitar el número de hilos concurrentes
        private int _totalRowsProcessed;  // Contador total de filas procesadas

        public DataProcessor(SqlHandler sqlHandler, Translator translator, int maxThreads = 4)
        {
            _sqlHandler = sqlHandler;
            _translator = translator;
            _maxThreads = maxThreads;
            _chunksQueue = new Queue<List<dynamic>>();  // Cola para los chunks
            _semaphore = new SemaphoreSlim(maxThreads);  // Controla el número de hilos concurrentes
            _totalRowsProcessed = 0;  // Inicializar el contador de filas procesadas
        }

        // Método para procesar el archivo CSV y cargar en SQL Server
        public async Task ProcessAndUploadAsync(string filePath, int chunkSize = 2000, char delimiter = ',')  // Reducir tamaño del chunk a 2000
        {
            _sqlHandler.CreateTableIfNotExists();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter.ToString(), // Delimitador definido
                HasHeaderRecord = true, // Indica que hay un encabezado en el archivo CSV
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                var records = new List<dynamic>();

                while (csv.Read())
                {
                    var record = csv.GetRecord<dynamic>();
                    records.Add(record);

                    if (records.Count >= chunkSize) // Cuando alcanza el tamaño del chunk
                    {
                        var chunkToProcess = new List<dynamic>(records);  // Copiar los registros del chunk
                        records.Clear();  // Limpiar la lista para el siguiente chunk

                        EnqueueChunk(chunkToProcess);  // Enviar chunk a la cola
                    }
                }

                // Procesar cualquier chunk restante
                if (records.Count > 0)
                {
                    EnqueueChunk(records);
                }
            }

            // Esperar a que todos los hilos terminen antes de continuar
            await WaitForAllTasksToFinishAsync();
        }

        // Método que envía un chunk a la cola y procesa el chunk en un nuevo hilo
        private void EnqueueChunk(List<dynamic> chunk)
        {
            lock (_chunksQueue)  // Bloquear la cola para evitar problemas de concurrencia
            {
                _chunksQueue.Enqueue(chunk);  // Añadir chunk a la cola
            }

            // Lanzar el procesamiento del chunk
            ProcessNextChunk();
        }

        // Método para procesar el siguiente chunk de la cola
        private void ProcessNextChunk()
        {
            // Controlar el número de hilos en ejecución
            _semaphore.Wait();  // Esperar hasta que haya capacidad disponible para otro hilo

            Task.Run(() =>
            {
                List<dynamic> chunkToProcess;

                lock (_chunksQueue)  // Bloquear la cola para evitar problemas de concurrencia
                {
                    if (_chunksQueue.Count == 0)
                    {
                        _semaphore.Release();  // Liberar el semáforo si no quedan más chunks
                        return;
                    }

                    chunkToProcess = _chunksQueue.Dequeue();  // Obtener el siguiente chunk
                }

                try
                {
                    ProcessChunk(chunkToProcess);  // Procesar el chunk
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar el chunk: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();  // Liberar el semáforo para que otro hilo pueda comenzar
                }

                // Si quedan más chunks, continuar procesando
                lock (_chunksQueue)
                {
                    if (_chunksQueue.Count > 0)
                    {
                        ProcessNextChunk();  // Lanzar otro hilo para el siguiente chunk
                    }
                }
            });
        }

        // Método para procesar cada chunk
        private void ProcessChunk(List<dynamic> records)
        {
            DataTable chunkTable = ConvertToDataTable(records);
            chunkTable = _translator.ProcessChunk(chunkTable);

            try
            {
                _sqlHandler.UploadToSql(chunkTable);
                Interlocked.Add(ref _totalRowsProcessed, records.Count);  // Contar cuántas filas se han procesado de manera thread-safe
                Console.WriteLine($"Chunk procesado. Total filas procesadas hasta ahora: {_totalRowsProcessed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el chunk en SQL Server: {ex.Message}");
            }
        }

        // Método que espera a que todos los hilos terminen antes de continuar
        private async Task WaitForAllTasksToFinishAsync()
        {
            for (int i = 0; i < _maxThreads; i++)
            {
                await _semaphore.WaitAsync();  // Esperar a que se liberen todos los semáforos (todos los hilos terminan)
            }
        }

        // Conversión de dinámicos a DataTable
        private DataTable ConvertToDataTable(List<dynamic> records)
        {
            DataTable dataTable = new DataTable();

            if (records == null || records.Count == 0)
                return dataTable;

            // Obtener las propiedades del primer objeto para definir las columnas del DataTable
            var firstRecord = records[0];
            var properties = ((IDictionary<string, object>)firstRecord).Keys;

            // Crear columnas en el DataTable
            foreach (var property in properties)
            {
                dataTable.Columns.Add(property, typeof(string)); // Asume que los datos son string
            }

            // Llenar el DataTable con los datos
            foreach (var record in records)
            {
                var row = dataTable.NewRow();
                var recordProperties = (IDictionary<string, object>)record;

                foreach (var property in properties)
                {
                    row[property] = recordProperties[property] != null ? recordProperties[property].ToString() : DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }

}
