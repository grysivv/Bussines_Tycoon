using System;
using System.Drawing;
using System.Windows.Forms;

namespace Conglomerate
{
    public class ErrorDetailsForm : Form
    {
        private Exception _exception;
        private string _logFilePath;

        public ErrorDetailsForm(Exception exception, string logFilePath)
        {
            _exception = exception;
            _logFilePath = logFilePath;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Krytyczny Błąd Gry";
            this.Size = new Size(650, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 24, 24);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9);

            // Górny panel nagłówka
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(32, 32, 32)
            };
            this.Controls.Add(pnlHeader);

            Label lblWarningIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 80, 80),
                Location = new Point(15, 8),
                Size = new Size(40, 45),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblWarningIcon);

            Label lblHeaderTitle = new Label
            {
                Text = "WYSTĄPIŁ NIEOCZEKIWANY BŁĄD GRY",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 80, 80),
                Location = new Point(60, 10),
                Size = new Size(550, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            Label lblHeaderSubtitle = new Label
            {
                Text = "Gra napotkała problem, który uniemożliwił jej poprawne działanie.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.DarkGray,
                Location = new Point(60, 32),
                Size = new Size(550, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblHeaderSubtitle);

            // Główna zawartość
            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            this.Controls.Add(pnlContent);

            Label lblSummary = new Label
            {
                Text = $"Typ: {_exception.GetType().Name}\nKomunikat: {_exception.Message}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 230, 230),
                Location = new Point(15, 75),
                Size = new Size(605, 40),
                AutoEllipsis = true
            };
            this.Controls.Add(lblSummary);

            Label lblLogInfo = new Label
            {
                Text = $"Szczegóły zostały zapisane w logu:\n{_logFilePath}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 220, 120),
                Location = new Point(15, 120),
                Size = new Size(605, 35)
            };
            this.Controls.Add(lblLogInfo);

            Label lblStackTraceHeader = new Label
            {
                Text = "Stos wywołań (Stack Trace):",
                Font = new Font("Segoe UI Semibold", 9),
                ForeColor = Color.Gray,
                Location = new Point(15, 160),
                Size = new Size(300, 18)
            };
            this.Controls.Add(lblStackTraceHeader);

            TextBox txtStackTrace = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(16, 16, 16),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 8.5f),
                Location = new Point(15, 180),
                Size = new Size(605, 200),
                Text = _exception.ToString(),
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(txtStackTrace);

            // Panel dolny przycisków
            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(32, 32, 32)
            };
            this.Controls.Add(pnlButtons);

            Button btnCopy = new Button
            {
                Text = "Kopiuj błąd",
                Size = new Size(120, 28),
                Location = new Point(15, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(this.GetFullErrorText());
                btnCopy.Text = "Skopiowano!";
                System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 2000 };
                t.Tick += (s2, e2) => { btnCopy.Text = "Kopiuj błąd"; t.Stop(); t.Dispose(); };
                t.Start();
            };
            pnlButtons.Controls.Add(btnCopy);

            Button btnContinue = new Button
            {
                Text = "Kontynuuj grę",
                Size = new Size(140, 28),
                Location = new Point(325, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 100, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlButtons.Controls.Add(btnContinue);

            Button btnExit = new Button
            {
                Text = "Zamknij grę",
                Size = new Size(140, 28),
                Location = new Point(480, 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(140, 40, 40),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) =>
            {
                Environment.Exit(0);
            };
            pnlButtons.Controls.Add(btnExit);
        }

        private string GetFullErrorText()
        {
            return $"--- BŁĄD GRY ---\n" +
                   $"Typ: {_exception.GetType().FullName}\n" +
                   $"Wiadomość: {_exception.Message}\n" +
                   $"Log file: {_logFilePath}\n" +
                   $"StackTrace:\n{_exception.StackTrace}";
        }
    }
}
