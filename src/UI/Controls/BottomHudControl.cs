using System;
using System.Drawing;
using System.Windows.Forms;
using Conglomerate.Financials;
using System.Collections.Generic;

namespace Conglomerate.UI.Controls
{
    public class BottomHudControl : UserControl
    {
        private Company _company;
        private GameManager _gameManager;
        
        private Panel pnlNews;
        private ListBox lstNews;
        private Panel pnlToolbar;
        
        private Label lblCash;
        private Label lblProfit;
        private Label lblDate;
        
        private Panel chartCashflow;
        
        public event EventHandler? OnFinanceClicked;
        public event EventHandler? OnCorporateClicked;
        public event EventHandler? OnMapClicked;
        public event EventHandler? OnBuildClicked;
        
        public event EventHandler? OnTimePaused;
        public event EventHandler? OnTimeNormal;
        public event EventHandler? OnTimeFast;

        public BottomHudControl(Company company, GameManager gameManager)
        {
            _company = company;
            _gameManager = gameManager;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            
            // Subskrypcja na update z gry (wymaga timer-a lub ticków eventów)
            System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer { Interval = 200 };
            updateTimer.Tick += (s, e) => RefreshData();
            updateTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(10, 25, 45); // Capitalism Lab ma niebieskawo-granatowy dół

            // Główne kontenery
            pnlNews = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(15, 30, 60), BorderStyle = BorderStyle.Fixed3D };
            pnlToolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlNews);

            // --- News Ticker ---
            lstNews = new ListBox();
            lstNews.Dock = DockStyle.Fill;
            lstNews.BackColor = Color.FromArgb(15, 30, 60);
            lstNews.ForeColor = Color.LightSkyBlue;
            lstNews.BorderStyle = BorderStyle.None;
            lstNews.Font = new Font("Consolas", 9, FontStyle.Bold);
            lstNews.SelectionMode = SelectionMode.None;
            pnlNews.Controls.Add(lstNews);

            // Wypełnienie przykładowymi danymi (do podpięcia pod prawdziwe eventy z silnika)
            lstNews.Items.Add("Jan 1, 2000 - Witamy w Conglomerate Tycoon!");
            lstNews.Items.Add("Jan 5, 2000 - Konkurencja złożyła wniosek o budowę nowej kopalni.");

            // --- Toolbar (Na dole) ---
            
            // Lewa sekcja (Przyciski akcji)
            FlowLayoutPanel flowLeft = new FlowLayoutPanel();
            flowLeft.Dock = DockStyle.Left;
            flowLeft.Width = 200;
            flowLeft.FlowDirection = FlowDirection.LeftToRight;
            flowLeft.WrapContents = false;
            flowLeft.Padding = new Padding(5);

            Button btnMap = CreateIconButton("M", Color.SteelBlue);
            btnMap.Click += (s, e) => OnMapClicked?.Invoke(this, EventArgs.Empty);
            
            Button btnFinance = CreateIconButton("$", Color.ForestGreen);
            btnFinance.Click += (s, e) => OnFinanceClicked?.Invoke(this, EventArgs.Empty);
            
            Button btnCorp = CreateIconButton("C", Color.DarkGoldenrod);
            btnCorp.Click += (s, e) => OnCorporateClicked?.Invoke(this, EventArgs.Empty);
            
            Button btnBuild = CreateIconButton("B", Color.Brown);
            btnBuild.Click += (s, e) => OnBuildClicked?.Invoke(this, EventArgs.Empty);

            flowLeft.Controls.Add(btnMap);
            flowLeft.Controls.Add(btnFinance);
            flowLeft.Controls.Add(btnCorp);
            flowLeft.Controls.Add(btnBuild);
            pnlToolbar.Controls.Add(flowLeft);

            // Środkowa sekcja (Cash, Profit)
            Panel pnlCenter = new Panel { Dock = DockStyle.Fill };
            pnlToolbar.Controls.Add(pnlCenter);
            
            Label lblCashTitle = new Label { Text = "Cash:", ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true, Location = new Point(10, 10) };
            lblCash = new Label { Text = "$0", ForeColor = Color.Lime, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true, Location = new Point(60, 10) };
            
            Label lblProfitTitle = new Label { Text = "Profit:", ForeColor = Color.White, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true, Location = new Point(160, 10) };
            lblProfit = new Label { Text = "$0", ForeColor = Color.Lime, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true, Location = new Point(210, 10) };

            pnlCenter.Controls.Add(lblCashTitle);
            pnlCenter.Controls.Add(lblCash);
            pnlCenter.Controls.Add(lblProfitTitle);
            pnlCenter.Controls.Add(lblProfit);
            
            // Kontrola czasu
            Button btnPause = CreateIconButton("||", Color.DimGray);
            btnPause.Location = new Point(350, 5);
            btnPause.Click += (s, e) => OnTimePaused?.Invoke(this, EventArgs.Empty);
            
            Button btnPlay = CreateIconButton(">", Color.DimGray);
            btnPlay.Location = new Point(390, 5);
            btnPlay.Click += (s, e) => OnTimeNormal?.Invoke(this, EventArgs.Empty);
            
            Button btnFast = CreateIconButton(">>", Color.DimGray);
            btnFast.Location = new Point(430, 5);
            btnFast.Click += (s, e) => OnTimeFast?.Invoke(this, EventArgs.Empty);
            
            pnlCenter.Controls.Add(btnPause);
            pnlCenter.Controls.Add(btnPlay);
            pnlCenter.Controls.Add(btnFast);

            // Prawa sekcja (Data, mini-wykres)
            Panel pnlRight = new Panel { Dock = DockStyle.Right, Width = 250, BackColor = Color.FromArgb(20, 20, 20) };
            pnlToolbar.Controls.Add(pnlRight);
            
            lblDate = new Label();
            lblDate.Dock = DockStyle.Top;
            lblDate.Height = 20;
            lblDate.TextAlign = ContentAlignment.MiddleCenter;
            lblDate.Font = new Font("Arial", 10, FontStyle.Bold);
            lblDate.ForeColor = Color.White;
            lblDate.Text = "Jan 1, 2000";
            pnlRight.Controls.Add(lblDate);
            
            // Chart Cashflow (Custom Paint)
            chartCashflow = new Panel();
            chartCashflow.Dock = DockStyle.Fill;
            chartCashflow.BackColor = Color.FromArgb(20, 20, 20);
            chartCashflow.Paint += ChartCashflow_Paint;
            
            pnlRight.Controls.Add(chartCashflow);
        }
        
        private void ChartCashflow_Paint(object? sender, PaintEventArgs e)
        {
            // Simple bar chart
            Graphics g = e.Graphics;
            int[] data = { 100, 150, 120, 200, 250 };
            int max = 300;
            
            int barWidth = 20;
            int spacing = 10;
            int startX = 20;
            
            for(int i = 0; i < data.Length; i++)
            {
                int barHeight = (int)((float)data[i] / max * chartCashflow.Height);
                int y = chartCashflow.Height - barHeight;
                int x = startX + i * (barWidth + spacing);
                
                using (SolidBrush brush = new SolidBrush(Color.Lime))
                {
                    g.FillRectangle(brush, x, y, barWidth, barHeight);
                }
            }
        }

        private Button CreateIconButton(string text, Color backColor)
        {
            Button btn = new Button();
            btn.Size = new Size(32, 32);
            btn.Text = text;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Arial", 12, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(2);
            return btn;
        }

        public void RefreshData()
        {
            if (_company != null)
            {
                lblCash.Text = $"{_company.Balance:C0}";
                lblCash.ForeColor = _company.Balance >= 0 ? Color.Lime : Color.Red;
                
                // Placeholder dla profiltu, obecnie stała wartość lub z historii
                decimal profit = 509170624m; // Przykładowa stała, docelowo z Ledger
                lblProfit.Text = $"{profit:C0}";
                lblProfit.ForeColor = profit >= 0 ? Color.Lime : Color.Red;
            }

            if (_gameManager != null)
            {
                // Wyliczenie daty. W Capitalism Lab data zależy od ticków/dni.
                // Uproszczone wyświetlanie:
                lblDate.Text = $"Day {_gameManager.CurrentDay}"; 
            }
        }
    }
}
