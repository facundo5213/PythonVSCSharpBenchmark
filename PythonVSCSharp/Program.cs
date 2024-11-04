using PythonVSCSharp;
using System.Diagnostics;

public class ConsoleApp
{
    private SqlHandler _sqlHandler;
    private Translator _translator;
    private DataProcessor _dataProcessor;

    public ConsoleApp(int maxThreads)
    {
        _sqlHandler = new SqlHandler("testDB");
        _translator = new Translator();
        _dataProcessor = new DataProcessor(_sqlHandler, _translator, maxThreads);
    }

    public async Task RunAsync(string filePath, int chunkSize, char delimiter)
    {
        var stopwatch = new Stopwatch();  // Inicializar el temporizador
        stopwatch.Start();  // Iniciar el cronómetro

        await _dataProcessor.ProcessAndUploadAsync(filePath, chunkSize, delimiter);

        stopwatch.Stop();  // Detener el cronómetro
        Console.WriteLine($"Proceso completado. Tiempo total: {stopwatch.Elapsed.TotalSeconds} segundos");
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Iniciando aplicación de procesamiento de datos...");
        string filePath = "C:\\Users\\JUAN_\\Downloads\\wikipedia_collection\\wikipedia_collection_corrected.csv"; // Archivo CSV de ejemplo
        int chunkSize = 2000; // Tamaño del chunk
        int maxThreads = 4; // Número máximo de hilos
        char delimiter = ';'; // Delimitador de CSV

        var app = new ConsoleApp(maxThreads);
        await app.RunAsync(filePath, chunkSize, delimiter);
    }
}