using System;
using System.Windows.Forms;

namespace Conglomerate
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Konfiguracja globalnego przechwytywania i logowania wyjątków
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => LogException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogException(e.ExceptionObject as Exception);

            if (args.Length > 0 && args[0] == "--run-tests")
            {
                Conglomerate.Financials.Tests.FinancialSystemTests.RunTests();
                return;
            }

            // Konfiguracja Windows Forms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Uruchomienie głównego okna gry (ekran logowania jest wbudowany w MainForm)
            Application.Run(new MainForm());
        }

        private static void LogException(Exception? ex)
        {
            if (ex == null) return;

            string logFile = "Nieznana ścieżka logu";
            try
            {
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logDir = System.IO.Path.Combine(docPath, "ConglomerateTycoon", "Logs");
                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                logFile = System.IO.Path.Combine(logDir, "error_log.txt");
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] BŁĄD: {ex.Message}\nTyp wyjątku: {ex.GetType().FullName}\nStack Trace:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    logMessage += $"Wyjątek wewnętrzny: {ex.InnerException.Message}\nStack Trace:\n{ex.InnerException.StackTrace}\n";
                }
                logMessage += new string('=', 60) + "\n\n";

                System.IO.File.AppendAllText(logFile, logMessage);

                using (var errForm = new ErrorDetailsForm(ex, logFile))
                {
                    errForm.ShowDialog();
                }
            }
            catch (Exception writeEx)
            {
                using (var errForm = new ErrorDetailsForm(ex, $"[Błąd zapisu pliku logu: {writeEx.Message}]"))
                {
                    errForm.ShowDialog();
                }
            }
        }
    }
}
