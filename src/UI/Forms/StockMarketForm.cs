using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Finance;
using Conglomerate.UI;

namespace Conglomerate
{
    /// <summary>
    /// Panel Giełdy Papierów Wartościowych — styl Capitalism Lab.
    /// Wyświetla notowania wszystkich firm, umożliwia zakup/sprzedaż akcji i śledzenie udziałów.
    /// </summary>
    public class StockMarketForm : Panel
    {
        private GameManager? _gm;
        private Company? _player;

        // Kontrolki
        private DataGridView dgvStocks = null!;
        private Panel pnlTradePanel = null!;
        private Label lblSelectedCompany = null!;
        private Label lblCurrentPrice = null!;
        private Label lblPlayerOwns = null!;
        private Label lblPortfolioValue = null!;
        private NumericUpDown numShares = null!;
        private Button btnBuy = null!;
        private Button btnSell = null!;
        private Panel pnlPriceChart = null!;
        private Label lblTotalPortfolio = null!;
        private string _selectedCompany = string.Empty;

        public StockMarketForm()
        {
            InitializeControls();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeControls()
        {

            // Główna tabela notowań
            dgvStocks = new DataGridView
            {
                Dock = DockStyle.Left,
                Width = 520,
                BackgroundColor = Color.FromArgb(25, 25, 35),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 60),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ColumnHeadersHeight = 32
            };
            dgvStocks.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 45);
            dgvStocks.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(150, 180, 255);
            dgvStocks.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvStocks.DefaultCellStyle.BackColor = Color.FromArgb(25, 25, 35);
            dgvStocks.DefaultCellStyle.ForeColor = Color.White;
            dgvStocks.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 80, 130);
            dgvStocks.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvStocks.Columns.Add(new DataGridViewTextBoxColumn { Name = "Firma", HeaderText = "Spółka", Width = 150 });
            dgvStocks.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cena", HeaderText = "Cena akcji", Width = 100 });
            dgvStocks.Columns.Add(new DataGridViewTextBoxColumn { Name = "MarketCap", HeaderText = "Market Cap", Width = 120 });
            dgvStocks.Columns.Add(new DataGridViewTextBoxColumn { Name = "TwojUdzial", HeaderText = "Twój udział", Width = 100 });
            dgvStocks.Columns["Cena"].DefaultCellStyle.ForeColor = Color.FromArgb(100, 230, 100);
            dgvStocks.SelectionChanged += (s, e) => OnStockSelected();
            this.Controls.Add(dgvStocks);

            // Panel boczny: transakcje
            pnlTradePanel = new Panel
            {
                BackColor = Color.FromArgb(25, 25, 40),
                Padding = new Padding(12)
            };

            lblSelectedCompany = CreateLabel("Wybierz spółkę z listy", 11, FontStyle.Bold, Color.FromArgb(150, 180, 255));
            lblCurrentPrice    = CreateLabel("Cena: —", 10, FontStyle.Regular, Color.FromArgb(100, 230, 100));
            lblPlayerOwns      = CreateLabel("Twoje akcje: 0", 9, FontStyle.Regular, Color.White);
            lblPortfolioValue  = CreateLabel("Wartość: —", 9, FontStyle.Regular, Color.FromArgb(240, 180, 50));
            lblTotalPortfolio  = CreateLabel("Wartość portfela: —", 11, FontStyle.Bold, Color.FromArgb(100, 230, 100));

            var lblShares = CreateLabel("Liczba akcji:", 9, FontStyle.Regular, Color.Gray);
            numShares = new NumericUpDown
            {
                Minimum = 1, Maximum = 10000, Value = 100,
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Width = 120
            };

            btnBuy = CreateButton("KUPTJ AKCJE", Color.FromArgb(30, 140, 60));
            btnBuy.Click += (s, e) => ExecuteBuy();
            btnSell = CreateButton("SPRZEDAJ AKCJE", Color.FromArgb(180, 60, 30));
            btnSell.Click += (s, e) => ExecuteSell();

            // Wykres cen
            pnlPriceChart = new Panel
            {
                BackColor = Color.FromArgb(20, 20, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 120
            };
            pnlPriceChart.Paint += OnPriceChartPaint;

            int y = 10;
            void AddCtrl(Control c, int h = 0)
            {
                c.Location = new Point(8, y);
                if (c.Width == 0 || c == pnlPriceChart) c.Width = pnlTradePanel.Width - 20;
                if (h > 0) c.Height = h;
                pnlTradePanel.Controls.Add(c);
                y += c.Height + 6;
            }

            AddCtrl(lblSelectedCompany, 24);
            AddCtrl(lblCurrentPrice, 20);
            AddCtrl(lblPlayerOwns, 20);
            AddCtrl(lblPortfolioValue, 20);
            AddCtrl(new Panel { Height = 1, BackColor = Color.FromArgb(50, 50, 70) });
            AddCtrl(CreateLabel("Wykres ceny (30 dni):", 8, FontStyle.Regular, Color.Gray), 16);
            pnlPriceChart.Width = pnlTradePanel.Width - 20;
            AddCtrl(pnlPriceChart, 120);
            AddCtrl(new Panel { Height = 1, BackColor = Color.FromArgb(50, 50, 70) });
            AddCtrl(lblShares, 18);
            numShares.Location = new Point(8, y); numShares.Width = 160; pnlTradePanel.Controls.Add(numShares); y += numShares.Height + 6;
            btnBuy.Location = new Point(8, y); pnlTradePanel.Controls.Add(btnBuy); y += btnBuy.Height + 4;
            btnSell.Location = new Point(8, y); pnlTradePanel.Controls.Add(btnSell); y += btnSell.Height + 12;
            AddCtrl(new Panel { Height = 1, BackColor = Color.FromArgb(50, 50, 70) });
            AddCtrl(lblTotalPortfolio, 22);

            this.Controls.Add(pnlTradePanel);
        }

        private static Label CreateLabel(string text, float size, FontStyle style, Color color) =>
            new Label { Text = text, Font = new Font("Segoe UI", size, style), ForeColor = color, AutoSize = true };

        private static Button CreateButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 32),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (pnlTradePanel == null || dgvStocks == null) return;
            dgvStocks.Location = new Point(0, 0);
            dgvStocks.Height = this.Height;
            pnlTradePanel.Location = new Point(dgvStocks.Right + 5, 0);
            pnlTradePanel.Size = new Size(this.Width - dgvStocks.Right - 10, this.Height);
        }

        public void SetGameManager(GameManager gm, Company player)
        {
            _gm = gm;
            _player = player;
            RefreshData();
        }

        public void RefreshData()
        {
            if (_gm == null || _player == null) return;

            dgvStocks.Rows.Clear();
            foreach (var listing in _gm.StockMarket.GetRanking())
            {
                decimal ownedPct = listing.PlayerOwnershipPercent;
                dgvStocks.Rows.Add(
                    listing.CompanyName,
                    listing.SharePrice.ToString("C0"),
                    FormatLargeNumber(listing.MarketCap),
                    $"{listing.PlayerOwnedShares:F0} ({ownedPct:F1}%)"
                );
            }

            // Wartość portfela
            decimal portfolioVal = _gm.StockMarket.GetPlayerPortfolioValue(_player);
            lblTotalPortfolio.Text = $"Portfel akcji: {portfolioVal:C0}";

            UpdateSelectedInfo();
        }

        private void OnStockSelected()
        {
            if (dgvStocks.SelectedRows.Count == 0) return;
            _selectedCompany = dgvStocks.SelectedRows[0].Cells["Firma"].Value?.ToString() ?? "";
            UpdateSelectedInfo();
            pnlPriceChart.Invalidate();
        }

        private void UpdateSelectedInfo()
        {
            if (_gm == null || _player == null || string.IsNullOrEmpty(_selectedCompany)) return;
            var listing = _gm.StockMarket.GetListing(_selectedCompany);
            if (listing == null) return;

            lblSelectedCompany.Text = listing.CompanyName;
            lblCurrentPrice.Text    = $"Cena: {listing.SharePrice:C2}";
            decimal owned = _player.OwnedShares.TryGetValue(_selectedCompany, out var o) ? o : 0m;
            lblPlayerOwns.Text      = $"Twoje akcje: {owned:F0}";
            lblPortfolioValue.Text  = $"Wartość: {owned * listing.SharePrice:C0}";
        }

        private void OnPriceChartPaint(object? sender, PaintEventArgs e)
        {
            if (_gm == null || string.IsNullOrEmpty(_selectedCompany)) return;
            var listing = _gm.StockMarket.GetListing(_selectedCompany);
            if (listing == null || listing.PriceHistory.Count < 2) return;

            var g = e.Graphics;
            var rc = pnlPriceChart.ClientRectangle;
            g.Clear(ThemeManager.BackgroundColor);

            var history = listing.PriceHistory.TakeLast(30).ToList();
            decimal minP = history.Min();
            decimal maxP = history.Max();
            if (maxP == minP) maxP = minP + 1m;

            float scaleX = (float)(rc.Width - 10) / Math.Max(1, history.Count - 1);
            float scaleY = (rc.Height - 20) / (float)(maxP - minP);

            var pts = new List<PointF>();
            for (int i = 0; i < history.Count; i++)
            {
                float x = 5 + i * scaleX;
                float y = rc.Height - 10 - (float)(history[i] - minP) * scaleY;
                pts.Add(new PointF(x, y));
            }

            using var pen = new Pen(ThemeManager.PositiveColor, 2);
            if (pts.Count > 1) g.DrawLines(pen, pts.ToArray());

            // Aktualna cena min/max na osi Y
            using var font = new Font("Segoe UI", 7);
            using var brush = new SolidBrush(ThemeManager.MutedTextColor);
            g.DrawString($"{minP:C0}", font, brush, new PointF(5, rc.Height - 14));
            g.DrawString($"{maxP:C0}", font, brush, new PointF(5, 2));
        }

        private void ExecuteBuy()
        {
            if (_gm == null || _player == null || string.IsNullOrEmpty(_selectedCompany)) return;
            decimal shares = numShares.Value;
            bool ok = _gm.StockMarket.BuyShares(_selectedCompany, shares, _player, _gm.CurrentDay, _gm.CurrentHour);
            MessageBox.Show(ok
                ? $"Kupiono {shares:F0} akcji {_selectedCompany}!"
                : "Nie udało się kupić akcji. Sprawdź saldo lub dostępność.",
                "Giełda", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshData();
        }

        private void ExecuteSell()
        {
            if (_gm == null || _player == null || string.IsNullOrEmpty(_selectedCompany)) return;
            decimal shares = numShares.Value;
            bool ok = _gm.StockMarket.SellShares(_selectedCompany, shares, _player, _gm.CurrentDay, _gm.CurrentHour);
            MessageBox.Show(ok
                ? $"Sprzedano {shares:F0} akcji {_selectedCompany}!"
                : "Nie masz wystarczającej liczby akcji.",
                "Giełda", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshData();
        }

        private static string FormatLargeNumber(decimal value)
        {
            if (value >= 1_000_000m) return $"${value / 1_000_000m:F1}M";
            if (value >= 1_000m) return $"${value / 1_000m:F1}K";
            return $"{value:C0}";
        }
    }
}
