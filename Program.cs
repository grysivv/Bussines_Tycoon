using System;
using System.Windows.Forms;

namespace Conglomerate
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
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
    }
}
