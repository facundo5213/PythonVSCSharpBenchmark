using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonVSCSharp
{
    public class ProgressHandler
    {
        private int _totalRows;
        private int _processedRows;
        private bool _isPaused = false;
        private Stopwatch _stopwatch;

        public ProgressHandler(int totalRows)
        {
            _totalRows = totalRows;
            _processedRows = 0;
            _stopwatch = new Stopwatch();
        }

        public void StartProgress()
        {
            _stopwatch.Start();
            while (_processedRows < _totalRows)
            {
                if (!_isPaused)
                {
                    var percentComplete = (double)_processedRows / _totalRows * 100;
                    Console.WriteLine($"Tiempo transcurrido: {_stopwatch.Elapsed.TotalSeconds:F2}s | Progreso: {percentComplete:F2}% completado");
                }
                Thread.Sleep(1000); // Actualiza cada segundo
            }
        }

        public void UpdateProcessedRows(int processed)
        {
            _processedRows += processed;
        }

        public void Pause()
        {
            _isPaused = true;
            Console.WriteLine("Proceso pausado.");
        }

        public void Resume()
        {
            _isPaused = false;
            Console.WriteLine("Proceso reanudado.");
        }
    }
}
