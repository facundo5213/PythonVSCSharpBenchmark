using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.MobileControls;
using System.Web.UI.WebControls;
using System.Windows;

namespace PthonVSCsharp
{
    public class AppGui : Form
    {
        private Button _pauseButton;
        private Button _resumeButton;
        private ProgressHandler _progressHandler;

        public AppGui(ProgressHandler progressHandler)
        {
            _progressHandler = progressHandler;

            _pauseButton = new Button { Text = "Pausar", Top = 20, Width = 100 };
            _resumeButton = new Button { Text = "Reanudar", Top = 60, Width = 100 };

            _pauseButton.Click += (sender, e) => _progressHandler.Pause();
            _resumeButton.Click += (sender, e) => _progressHandler.Resume();

            Controls.Add(_pauseButton);
            Controls.Add(_resumeButton);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            var sqlHandler = new SqlHandler("DESKTOP-7U12N0I", "Python", "nombre_de_tu_tabla");
            var translator = new Translator();
            var progressHandler = new ProgressHandler(350000000);
            var dataProcessor = new DataProcessor(sqlHandler, translator, progressHandler);

            // Iniciar la interfaz gráfica
            Application.Run(new AppGui(progressHandler));

            // Procesar datos en segundo plano
            Task.Run(() => dataProcessor.ProcessAndUpload("wikipedia_collection.csv"));
        }
    }
}
