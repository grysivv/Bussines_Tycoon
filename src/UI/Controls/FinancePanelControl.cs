using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Finance;
using Conglomerate.Financials;

namespace Conglomerate.UI.Controls
{
    public class FinancePanelControl : UserControl
    {
        private Company _company;
        private GameManager _gameManager;

        // Header KPI labels (updated on refresh)
        private Label _lblCash, _lblWorkingCap, _lblMarketCap, _lblEPS, _lblPE;

        // Tab content panels refreshed on demand
        private Panel _pnlPLChart;
        private DataGridView _dgvPL;
        private Panel _pnlPieChart;
        private Label _lblPlayerPct, _lblFreePct;
        private DataGridView _dgvLoans;
        private Label _lblTotalDebt, _lblMonthlyPayment, _lblROIC, _lblDebtCost;
        private Panel _pnlCCIChart;
        private Label _lblPhase, _lblCCI, _lblInflation, _lblRate;

        public FinancePanelControl(Company company)
        {
            _company = company;
            InitializeComponent();
        }

        public void SetGameManager(GameManager gm) => _gameManager = gm;

        // ─────────────────────────────────────────────────────────
        //  Bootstrap
        // ─────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Size           = new Size(940, 700);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;
            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            var header = BuildHeader();
            this.Controls.Add(header);

            var tabs = BuildTabs();
            this.Controls.Add(tabs);

            ThemeManager.MakeDraggable(header, this);
        }

        // ─────────────────────────────────────────────────────────
        //  Header KPI bar
        // ─────────────────────────────────────────────────────────

        private Panel BuildHeader()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 116, BackColor = ThemeManager.HeaderBackground };
            pnl.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var brush = new LinearGradientBrush(p.ClientRectangle,
                    ThemeManager.HeaderBackground, Color.FromArgb(10, 22, 40), LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);
                using var gp = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(gp, 0, 0, p.Width, 0);
                using var sp = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(sp, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            pnl.Controls.Add(new Label
            {
                Text      = "Raport Korporacyjny",
                Font      = ThemeManager.TitleFont,
                ForeColor = ThemeManager.GoldColor,
                Location  = new Point(16, 10),
                AutoSize  = true
            });

            var btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(32, 32),
                Location = new Point(900, 10),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.ToolTipText("Zamknij");
            btnClose.Click    += (s, e) => this.Visible = false;
            pnl.Controls.Add(btnClose);

            // KPI row
            int x = 16;
            _lblCash         = AddKPI(pnl, "Gotówka (Cash)",        "$0",    ref x, ThemeManager.PositiveColor);
            _lblWorkingCap   = AddKPI(pnl, "Kapitał Obrotowy",      "$0",    ref x, ThemeManager.TextColor);
            _lblMarketCap    = AddKPI(pnl, "Market Cap",             "$0",    ref x, ThemeManager.GoldColor);
            _lblEPS          = AddKPI(pnl, "EPS (Zysk/Akcja)",      "$0.00", ref x, ThemeManager.AccentColor);
            _lblPE           = AddKPI(pnl, "P/E Ratio",              "—",     ref x, ThemeManager.AccentColor);

            return pnl;
        }

        private Label AddKPI(Panel pnl, string caption, string init, ref int x, Color color)
        {
            pnl.Controls.Add(new Label
            {
                Text      = caption,
                Font      = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location  = new Point(x, 50),
                AutoSize  = true
            });
            var lbl = new Label
            {
                Text      = init,
                Font      = new Font("Consolas", 13, FontStyle.Bold),
                ForeColor = color,
                Location  = new Point(x, 66),
                AutoSize  = true
            };
            pnl.Controls.Add(lbl);
            x += 178;
            return lbl;
        }

        // ─────────────────────────────────────────────────────────
        //  Tab control
        // ─────────────────────────────────────────────────────────

        private TabControl BuildTabs()
        {
            var tc = new TabControl
            {
                Dock      = DockStyle.Fill,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.DefaultFont
            };

            tc.TabPages.Add(BuildPLTab());
            tc.TabPages.Add(BuildStockTab());
            tc.TabPages.Add(BuildDebtTab());
            tc.TabPages.Add(BuildSubsTab());
            tc.TabPages.Add(BuildMacroTab());

            return tc;
        }

        // ─────────────────────────────────────────────────────────
        //  TAB 1: Rachunek Zysków i Strat (P&L)
        // ─────────────────────────────────────────────────────────

        private TabPage BuildPLTab()
        {
            var page = new TabPage("📊  P&L Statement") { BackColor = ThemeManager.BackgroundColor, ForeColor = ThemeManager.TextColor, AutoScroll = true };

            // Chart panel (upper half)
            _pnlPLChart = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = ThemeManager.PanelBackground };
            _pnlPLChart.Paint += PaintPLBarChart;
            page.Controls.Add(_pnlPLChart);

            // Spacing panel (prevents table overlap)
            var spacer = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = ThemeManager.BackgroundColor };
            page.Controls.Add(spacer);

            // P&L summary DataGridView
            _dgvPL = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = ThemeManager.BackgroundColor };
            ThemeManager.ApplyDataGridViewTheme(_dgvPL);
            _dgvPL.AutoGenerateColumns = false;
            _dgvPL.ReadOnly = true;
            _dgvPL.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pozycja", Name = "Item", Width = 260 });
            var colAmt = new DataGridViewTextBoxColumn { HeaderText = "Bieżący miesiąc", Name = "Amount", Width = 160 };
            colAmt.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvPL.Columns.Add(colAmt);
            var colPrev = new DataGridViewTextBoxColumn { HeaderText = "Poprzedni miesiąc", Name = "Prev", Width = 160 };
            colPrev.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvPL.Columns.Add(colPrev);
            var colDiff = new DataGridViewTextBoxColumn { HeaderText = "Zmiana", Name = "Diff", Width = 120 };
            colDiff.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvPL.Columns.Add(colDiff);
            _dgvPL.CellFormatting += DgvPL_CellFormatting;

            page.Controls.Add(_dgvPL);
            return page;
        }

        private void PaintPLBarChart(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = (Panel)sender!;
            var rc = panel.ClientRectangle;

            g.Clear(ThemeManager.PanelBackground);

            if (_company == null) return;

            var history = _company.Engine.MonthlyHistory;
            if (history.Count == 0)
            {
                DrawCenteredText(g, rc, "Brak danych historycznych (czekaj na zamknięcie miesiąca)", ThemeManager.MutedTextColor);
                return;
            }

            // Show last 8 months
            var data = history.Skip(Math.Max(0, history.Count - 8)).ToList();

            int marginL = 60, marginR = 10, marginT = 24, marginB = 30;
            float chartW = rc.Width - marginL - marginR;
            float chartH = rc.Height - marginT - marginB;

            // Find scale
            decimal maxAbs = 1m;
            foreach (var snap in data)
            {
                if (Math.Abs(snap.PnL.Revenue) > maxAbs) maxAbs = Math.Abs(snap.PnL.Revenue);
                if (Math.Abs(snap.PnL.NetIncome) > maxAbs) maxAbs = Math.Abs(snap.PnL.NetIncome);
            }

            float barGroupW = chartW / data.Count;
            float barW = barGroupW * 0.35f;

            // Axis
            using var axPen = new Pen(ThemeManager.SeparatorColor, 1);
            float zeroY = marginT + chartH * (maxAbs > 0 ? (float)(maxAbs / (maxAbs * 2)) : 0.5f);
            g.DrawLine(axPen, marginL, marginT, marginL, marginT + chartH);
            g.DrawLine(axPen, marginL, zeroY, marginL + chartW, zeroY);

            // Title
            using var titleBrush = new SolidBrush(ThemeManager.MutedTextColor);
            g.DrawString("Przychód vs. Zysk Netto (ostatnie miesiące)", ThemeManager.SmallFont, titleBrush, marginL, 4);

            for (int i = 0; i < data.Count; i++)
            {
                float groupX = marginL + i * barGroupW + barGroupW * 0.1f;
                var snap = data[i];

                DrawBar(g, snap.PnL.Revenue, maxAbs, groupX, zeroY, barW, chartH, ThemeManager.AccentColor);
                DrawBar(g, snap.PnL.NetIncome, maxAbs, groupX + barW + 2, zeroY, barW, chartH,
                    snap.PnL.NetIncome >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor);

                // Month label
                using var lb = new SolidBrush(ThemeManager.MutedTextColor);
                g.DrawString($"M{snap.PeriodIndex}", ThemeManager.SmallFont, lb, groupX, zeroY + 4);
            }

            // Legend
            using var lgBrush = new SolidBrush(ThemeManager.AccentColor);
            g.FillRectangle(lgBrush, rc.Right - 160, 6, 14, 10);
            g.DrawString("Przychód", ThemeManager.SmallFont, titleBrush, rc.Right - 144, 4);
            using var niB = new SolidBrush(ThemeManager.PositiveColor);
            g.FillRectangle(niB, rc.Right - 70, 6, 14, 10);
            g.DrawString("Zysk", ThemeManager.SmallFont, titleBrush, rc.Right - 54, 4);
        }

        private void DrawBar(Graphics g, decimal value, decimal maxAbs, float x, float zeroY, float w, float chartH, Color color)
        {
            if (maxAbs == 0) return;
            float halfH = chartH / 2f;
            float barH = (float)(Math.Abs(value) / maxAbs) * halfH;
            using var brush = new SolidBrush(color);
            if (value >= 0)
                g.FillRectangle(brush, x, zeroY - barH, w, barH);
            else
                g.FillRectangle(brush, x, zeroY, w, barH);
        }

        private void DgvPL_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 1 || e.ColumnIndex == 2 || e.ColumnIndex == 3)
            {
                if (e.Value is string s)
                {
                    if (s.StartsWith("-") || s.StartsWith("▼"))
                        e.CellStyle.ForeColor = ThemeManager.NegativeColor;
                    else if (s.StartsWith("+") || s.StartsWith("▲"))
                        e.CellStyle.ForeColor = ThemeManager.PositiveColor;
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        //  TAB 2: Rynek Kapitałowy (Shareholding)
        // ─────────────────────────────────────────────────────────

        private TabPage BuildStockTab()
        {
            var page = new TabPage("🏛️  Rynek Kapitałowy") { BackColor = ThemeManager.BackgroundColor, ForeColor = ThemeManager.TextColor, AutoScroll = true };

            // Pie chart
            _pnlPieChart = new Panel { Location = new Point(16, 16), Size = new Size(280, 240), BackColor = ThemeManager.PanelBackground };
            _pnlPieChart.Paint += PaintOwnershipPie;
            page.Controls.Add(_pnlPieChart);

            // Info labels
            int lx = 320;
            AddSectionHeader(page, "Struktura Akcjonariatu", lx, 16);

            _lblPlayerPct = AddInfoLabel(page, "Gracz (Ty):", "—", lx, 50, ThemeManager.GoldColor);
            AddInfoLabel(page, "Free Float (AI/Rynek):", "—", lx, 80, ThemeManager.MutedTextColor);
            _lblFreePct = _company != null ? AddInfoLabel(page, "", "—", lx + 160, 80, ThemeManager.MutedTextColor) : null!;

            AddSectionHeader(page, "Inżynieria Kapitałowa", lx, 130);

            var btnBuyback = new Button { Text = "📥 Skup akcji własnych (Buyback)", Location = new Point(lx, 158), Size = new Size(280, 36) };
            ThemeManager.ApplyButtonTheme(btnBuyback);
            btnBuyback.Click += BtnBuyback_Click;
            page.Controls.Add(btnBuyback);

            var btnEmit = new Button { Text = "📤 Emisja nowych akcji (Pozyskaj kapitał)", Location = new Point(lx, 202), Size = new Size(280, 36) };
            ThemeManager.ApplySecondaryButtonTheme(btnEmit);
            btnEmit.Click += BtnEmit_Click;
            page.Controls.Add(btnEmit);

            // Competitors ranking
            AddSectionHeader(page, "Ranking Kapitalizacji Rynkowej", 16, 270);

            var dgvRank = new DataGridView { Location = new Point(16, 296), Size = new Size(880, 220) };
            dgvRank.Name = "dgvRank";
            ThemeManager.ApplyDataGridViewTheme(dgvRank);
            dgvRank.ReadOnly = true;
            dgvRank.AutoGenerateColumns = false;
            dgvRank.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#",        Name = "Rank",    Width = 40  });
            dgvRank.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Spółka",   Name = "Name",    Width = 220 });
            var capCol = new DataGridViewTextBoxColumn { HeaderText = "Market Cap", Name = "Cap",    Width = 160 };
            capCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRank.Columns.Add(capCol);
            var priceCol = new DataGridViewTextBoxColumn { HeaderText = "Cena akcji", Name = "Price",  Width = 130 };
            priceCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRank.Columns.Add(priceCol);
            var ownCol = new DataGridViewTextBoxColumn { HeaderText = "Twój udział", Name = "Own",    Width = 110 };
            ownCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvRank.Columns.Add(ownCol);
            page.Controls.Add(dgvRank);

            return page;
        }

        private void PaintOwnershipPie(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = ((Panel)sender!).ClientRectangle;
            g.Clear(ThemeManager.PanelBackground);

            if (_company == null || _gameManager == null)
            {
                DrawCenteredText(g, rc, "Brak danych", ThemeManager.MutedTextColor);
                return;
            }

            var listing = _gameManager.StockMarket.GetListing(_company.Name);
            if (listing == null || listing.TotalShares == 0)
            {
                DrawCenteredText(g, rc, "Spółka nie notowana", ThemeManager.MutedTextColor);
                return;
            }

            float playerPct  = (float)(listing.PlayerOwnedShares / listing.TotalShares);
            float floatPct   = 1f - playerPct;

            int cx = rc.Width / 2, cy = rc.Height / 2 - 10;
            int r  = Math.Min(cx, cy) - 20;
            var pieRect = new Rectangle(cx - r, cy - r, r * 2, r * 2);

            // Player slice
            using var playerBrush = new SolidBrush(ThemeManager.GoldColor);
            float playerDeg = playerPct * 360f;
            g.FillPie(playerBrush, pieRect, -90, playerDeg);

            // Float slice
            using var floatBrush = new SolidBrush(ThemeManager.SeparatorColor);
            g.FillPie(floatBrush, pieRect, -90 + playerDeg, 360 - playerDeg);

            // Border
            using var border = new Pen(ThemeManager.BorderColor, 1);
            g.DrawEllipse(border, pieRect);

            // Labels
            using var labelBrush = new SolidBrush(ThemeManager.TextColor);
            g.DrawString($"Ty: {playerPct:P1}", ThemeManager.SmallFont, labelBrush, 8, rc.Height - 42);
            using var floatLabelBrush = new SolidBrush(ThemeManager.MutedTextColor);
            g.DrawString($"Free Float: {floatPct:P1}", ThemeManager.SmallFont, floatLabelBrush, 8, rc.Height - 24);

            // Title
            g.DrawString("Struktura akcjonariatu", ThemeManager.SmallFont, floatLabelBrush, 4, 4);
        }

        private void BtnBuyback_Click(object? sender, EventArgs e)
        {
            if (_company == null || _gameManager == null) return;
            var listing = _gameManager.StockMarket.GetListing(_company.Name);
            if (listing == null) return;

            decimal freeFloat = listing.TotalShares - listing.PlayerOwnedShares;
            if (freeFloat <= 0) { MessageBox.Show("Posiadasz już 100% akcji własnej spółki!", "Skup akcji", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            decimal buyCount = Math.Min(100m, freeFloat);
            decimal cost = buyCount * listing.SharePrice;
            if (_company.Balance < cost) { MessageBox.Show($"Brak środków na skup {buyCount:F0} akcji (koszt: {cost:C})", "Skup akcji", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _company.Balance -= cost;
            listing.PlayerOwnedShares += buyCount;
            if (!_company.OwnedShares.ContainsKey(_company.Name)) _company.OwnedShares[_company.Name] = 0m;
            _company.OwnedShares[_company.Name] += buyCount;
            _company.AddTransaction(_gameManager.CurrentDay, _gameManager.CurrentHour,
                $"Skup akcji własnych: {buyCount:F0} szt. @ {listing.SharePrice:C}", -cost, "Inwestycje");

            RefreshData();
        }

        private void BtnEmit_Click(object? sender, EventArgs e)
        {
            if (_company == null || _gameManager == null) return;
            var listing = _gameManager.StockMarket.GetListing(_company.Name);
            if (listing == null) return;

            decimal newShares = 1000m;
            decimal capital = newShares * listing.SharePrice;

            _company.Balance += capital;
            listing.TotalShares += newShares;
            _company.AddTransaction(_gameManager.CurrentDay, _gameManager.CurrentHour,
                $"Emisja akcji: {newShares:F0} szt. @ {listing.SharePrice:C} — pozysk. kapitał", capital, "Inwestycje");

            MessageBox.Show($"Wyemitowano {newShares:F0} nowych akcji.\nPozyskany kapitał: {capital:C}\nTwój udział: {listing.PlayerOwnershipPercent:F1}%",
                "Emisja akcji", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshData();
        }

        // ─────────────────────────────────────────────────────────
        //  TAB 3: Dług i Dźwignia Finansowa
        // ─────────────────────────────────────────────────────────

        private TabPage BuildDebtTab()
        {
            var page = new TabPage("🏦  Dług & Dźwignia") { BackColor = ThemeManager.BackgroundColor, ForeColor = ThemeManager.TextColor, AutoScroll = true };

            AddSectionHeader(page, "Aktualne kredyty", 16, 16);

            _dgvLoans = new DataGridView { Location = new Point(16, 44), Size = new Size(880, 200) };
            ThemeManager.ApplyDataGridViewTheme(_dgvLoans);
            _dgvLoans.ReadOnly = true;
            _dgvLoans.AutoGenerateColumns = false;
            _dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Typ",            Name = "Type",     Width = 140 });
            var balCol = new DataGridViewTextBoxColumn { HeaderText = "Pozostałe saldo", Name = "Balance",  Width = 160 };
            balCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvLoans.Columns.Add(balCol);
            var rateCol = new DataGridViewTextBoxColumn { HeaderText = "Oprocentowanie", Name = "Rate",     Width = 140 };
            rateCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvLoans.Columns.Add(rateCol);
            var mthCol = new DataGridViewTextBoxColumn { HeaderText = "Rata/miesiąc",   Name = "Monthly",  Width = 140 };
            mthCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvLoans.Columns.Add(mthCol);
            _dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Miesięcy pozostało", Name = "Months", Width = 150 });
            page.Controls.Add(_dgvLoans);

            AddSectionHeader(page, "Wskaźniki dźwigni finansowej", 16, 266);

            int iy = 294;
            _lblTotalDebt     = AddInfoLabel(page, "Łączne zadłużenie:",          "—", 16,  iy,      ThemeManager.NegativeColor);
            _lblMonthlyPayment = AddInfoLabel(page, "Zobowiązania miesięczne:",   "—", 320, iy,      ThemeManager.NegativeColor);
            _lblROIC          = AddInfoLabel(page, "ROIC (zwrot z kapitału):",    "—", 16,  iy + 40, ThemeManager.PositiveColor);
            _lblDebtCost      = AddInfoLabel(page, "Koszt długu (avg %):",        "—", 320, iy + 40, ThemeManager.AccentColor);

            AddSectionHeader(page, "Kalkulator dźwigni", 16, iy + 90);

            var pnlCalc = new Panel { Location = new Point(16, iy + 116), Size = new Size(880, 120), BackColor = ThemeManager.PanelBackground };
            pnlCalc.Paint += PaintLeverageInfo;
            page.Controls.Add(pnlCalc);

            return page;
        }

        private void PaintLeverageInfo(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rc = ((Panel)sender!).ClientRectangle;
            g.Clear(ThemeManager.PanelBackground);

            if (_company == null || _gameManager == null) return;

            var pnl = _company.Engine.CalculateCurrentPnL();
            decimal netWorth = _company.GetNetWorth();
            decimal totalDebt = _gameManager.Banking.TotalDebt;

            decimal ebit = pnl.EBIT;
            decimal debtCostAbs = -pnl.Interest;
            decimal roic = (netWorth + totalDebt) > 0 ? (ebit / (netWorth + totalDebt)) * 100m : 0m;
            decimal avgDebtRate = totalDebt > 0 ? (debtCostAbs / totalDebt) * 100m : 0m;

            string recommendation;
            Color recColor;
            if (totalDebt == 0)
            { recommendation = "Brak dźwigni — możesz rozważyć kredyt, jeśli ROIC > kosztu długu."; recColor = ThemeManager.MutedTextColor; }
            else if (roic > avgDebtRate)
            { recommendation = $"✅ Dźwignia działa NA TWOJĄ KORZYŚĆ (ROIC {roic:F1}% > koszt długu {avgDebtRate:F1}%). Rozważ ekspansję."; recColor = ThemeManager.PositiveColor; }
            else
            { recommendation = $"⚠️ Dźwignia PRACUJE PRZECIWKO TOBIE (ROIC {roic:F1}% < koszt długu {avgDebtRate:F1}%). Spłać dług lub zwiększ rentowność."; recColor = ThemeManager.NegativeColor; }

            using var brush = new SolidBrush(recColor);
            g.DrawString(recommendation, ThemeManager.DefaultFont, brush, new RectangleF(12, 12, rc.Width - 24, rc.Height - 24));
        }

        // ─────────────────────────────────────────────────────────
        //  TAB 4: Spółki Zależne (Subsidiaries)
        // ─────────────────────────────────────────────────────────

        private TabPage BuildSubsTab()
        {
            var page = new TabPage("🏢  Spółki Zależne") { BackColor = ThemeManager.BackgroundColor, ForeColor = ThemeManager.TextColor, AutoScroll = true };

            AddSectionHeader(page, "Portfel spółek-córek i udziałów", 16, 16);

            var dgvSubs = new DataGridView { Location = new Point(16, 44), Size = new Size(880, 280) };
            dgvSubs.Name = "dgvSubs";
            ThemeManager.ApplyDataGridViewTheme(dgvSubs);
            dgvSubs.ReadOnly = true;
            dgvSubs.AutoGenerateColumns = false;
            dgvSubs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Spółka",    Name = "Comp",   Width = 220 });
            var ownPctCol = new DataGridViewTextBoxColumn { HeaderText = "Twój udział",   Name = "OwnPct", Width = 120 };
            ownPctCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSubs.Columns.Add(ownPctCol);
            var capCol = new DataGridViewTextBoxColumn { HeaderText = "Market Cap",       Name = "Cap",    Width = 160 };
            capCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSubs.Columns.Add(capCol);
            var priceCol = new DataGridViewTextBoxColumn { HeaderText = "Wartość pakietu", Name = "Value",  Width = 160 };
            priceCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSubs.Columns.Add(priceCol);
            dgvSubs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",    Name = "Status", Width = 160 });
            page.Controls.Add(dgvSubs);

            AddSectionHeader(page, "Instrukcja", 16, 340);
            page.Controls.Add(new Label
            {
                Text      = "Kupuj akcje konkurentów przez zakładkę \"Giełda\", aby budować piramidy kapitałowe.\n" +
                            "Przejęcie >50% akcji spółki AI daje Ci kontrolę nad jej operacjami i dywidendami.\n" +
                            "Przejęcie >75% umożliwia wchłonięcie spółki (Hostile Takeover).",
                Font      = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location  = new Point(16, 366),
                Size      = new Size(880, 60)
            });

            return page;
        }

        // ─────────────────────────────────────────────────────────
        //  TAB 5: Widget Makroekonomiczny
        // ─────────────────────────────────────────────────────────

        private TabPage BuildMacroTab()
        {
            var page = new TabPage("🌐  Makroekonomia") { BackColor = ThemeManager.BackgroundColor, ForeColor = ThemeManager.TextColor, AutoScroll = true };

            // Phase indicator (large)
            var pnlPhase = new Panel { Location = new Point(16, 16), Size = new Size(340, 130), BackColor = ThemeManager.PanelBackground };
            pnlPhase.Paint += PaintPhasePanel;
            page.Controls.Add(pnlPhase);

            // KPI row
            int mx = 380;
            AddSectionHeader(page, "Wskaźniki makroekonomiczne", mx, 16);
            _lblPhase    = AddInfoLabel(page, "Faza cyklu:",              "—",      mx,       44, ThemeManager.GoldColor);
            _lblCCI      = AddInfoLabel(page, "Indeks Zaufania (CCI):",   "—",      mx,       74, ThemeManager.AccentColor);
            _lblInflation = AddInfoLabel(page, "Inflacja:",               "—",      mx,      104, ThemeManager.NegativeColor);
            _lblRate     = AddInfoLabel(page, "Stopa bazowa NBP:",        "—",      mx + 260, 74, ThemeManager.MutedTextColor);

            // CCI line chart
            AddSectionHeader(page, "Historia CCI (ostatnie 60 dni)", 16, 162);
            _pnlCCIChart = new Panel { Location = new Point(16, 188), Size = new Size(440, 180), BackColor = ThemeManager.PanelBackground };
            _pnlCCIChart.Paint += PaintCCIChart;
            page.Controls.Add(_pnlCCIChart);

            // Inflation line chart
            AddSectionHeader(page, "Historia inflacji (ostatnie 60 dni)", 476, 162);
            var pnlInflChart = new Panel { Location = new Point(476, 188), Size = new Size(420, 180), BackColor = ThemeManager.PanelBackground };
            pnlInflChart.Paint += PaintInflationChart;
            page.Controls.Add(pnlInflChart);

            // Impact table
            AddSectionHeader(page, "Wpływ makroekonomii na Twój biznes", 16, 385);
            var pnlImpact = new Panel { Location = new Point(16, 412), Size = new Size(880, 110), BackColor = ThemeManager.PanelBackground };
            pnlImpact.Paint += PaintMacroImpact;
            page.Controls.Add(pnlImpact);

            return page;
        }

        private void PaintPhasePanel(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = ((Panel)sender!).ClientRectangle;
            g.Clear(ThemeManager.PanelBackground);

            if (_gameManager == null) return;

            var macro = _gameManager.Macro;
            using var phaseBrush = new SolidBrush(macro.PhaseColor);
            using var bigFont = new Font("Segoe UI", 28, FontStyle.Bold);
            using var smallBrush = new SolidBrush(ThemeManager.MutedTextColor);

            g.DrawString("FAZA GOSPODARCZA", ThemeManager.SmallFont, smallBrush, 10, 8);
            g.DrawString(macro.PhaseLabel, bigFont, phaseBrush, 10, 32);

            // Visual bar for CCI
            float cci = macro.ConsumerConfidenceIndex / 100f;
            using var barBg = new SolidBrush(ThemeManager.SeparatorColor);
            using var barFg = new SolidBrush(macro.PhaseColor);
            int barY = rc.Height - 28;
            g.FillRectangle(barBg, 10, barY, rc.Width - 20, 12);
            g.FillRectangle(barFg, 10, barY, (int)((rc.Width - 20) * cci), 12);
            g.DrawString($"CCI: {macro.ConsumerConfidenceIndex:F0}/100", ThemeManager.SmallFont, smallBrush, 10, barY - 16);
        }

        private void PaintCCIChart(object? sender, PaintEventArgs e) =>
            PaintLineChart(e, ((Panel)sender!).ClientRectangle, _gameManager?.Macro.CCIHistory, "CCI", ThemeManager.AccentColor, 0, 100);

        private void PaintInflationChart(object? sender, PaintEventArgs e) =>
            PaintLineChart(e, ((Panel)sender!).ClientRectangle, _gameManager?.Macro.InflationHistory, "Inflacja %", ThemeManager.NegativeColor, 0, 20);

        private void PaintLineChart(PaintEventArgs e, Rectangle rc, List<float>? data, string label, Color lineColor, float yMin, float yMax)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ThemeManager.PanelBackground);

            using var muted = new SolidBrush(ThemeManager.MutedTextColor);
            g.DrawString(label, ThemeManager.SmallFont, muted, 4, 4);

            if (data == null || data.Count < 2) { DrawCenteredText(g, rc, "Zbieranie danych...", ThemeManager.MutedTextColor); return; }

            int mL = 36, mR = 8, mT = 20, mB = 20;
            float cW = rc.Width - mL - mR;
            float cH = rc.Height - mT - mB;

            using var axPen = new Pen(ThemeManager.SeparatorColor, 1);
            g.DrawLine(axPen, mL, mT, mL, mT + cH);
            g.DrawLine(axPen, mL, mT + cH, mL + cW, mT + cH);

            // Y labels
            g.DrawString($"{yMax:F0}", ThemeManager.SmallFont, muted, 2, mT - 4);
            g.DrawString($"{yMin:F0}", ThemeManager.SmallFont, muted, 2, mT + cH - 12);

            // Line
            var pts = new PointF[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                float nx = mL + (i / (float)(data.Count - 1)) * cW;
                float ny = mT + cH - ((data[i] - yMin) / (yMax - yMin)) * cH;
                pts[i] = new PointF(nx, ny);
            }

            using var lPen = new Pen(lineColor, 2);
            g.DrawLines(lPen, pts);

            // Last value
            var last = data[^1];
            using var valBrush = new SolidBrush(lineColor);
            g.DrawString($"{last:F1}", ThemeManager.SmallFont, valBrush, pts[^1].X + 2, pts[^1].Y - 8);
        }

        private void PaintMacroImpact(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = ((Panel)sender!).ClientRectangle;
            g.Clear(ThemeManager.PanelBackground);

            if (_gameManager == null) return;

            var macro = _gameManager.Macro;
            float demand = macro.GetRetailDemandMultiplier();
            decimal loanRate = macro.GetLoanRate(LoanType.MediumTerm) * 100m;

            string[] lines =
            {
                $"• Popyt detaliczny (CCI):    {demand:F2}x  {(demand >= 1.0f ? "(powyżej normy)" : "(poniżej normy)")}",
                $"• Oprocentowanie kredytów:    Krótkoterminowy {macro.GetLoanRate(LoanType.ShortTerm)*100m:F2}%  |  Śr. {macro.GetLoanRate(LoanType.MediumTerm)*100m:F2}%  |  Dług. {macro.GetLoanRate(LoanType.LongTerm)*100m:F2}%",
                $"• Presja płacowa (WPI):       {(macro.InflationRate > 4 ? "Wysoka — pracownicy żądają podwyżek" : "Umiarkowana")}",
                $"• Inflacja kosztów:           {(macro.InflationRate > 5 ? $"+{macro.InflationRate:F1}% ↑ koszty surowców i utrzymania" : $"{macro.InflationRate:F1}% — stabilne koszty")}"
            };

            using var brush = new SolidBrush(ThemeManager.TextColor);
            for (int i = 0; i < lines.Length; i++)
                g.DrawString(lines[i], ThemeManager.SmallFont, brush, 10, 8 + i * 22);
        }

        // ─────────────────────────────────────────────────────────
        //  Refresh
        // ─────────────────────────────────────────────────────────

        public void RefreshData()
        {
            if (_company == null) return;

            RefreshKPIs();
            RefreshPLTab();
            RefreshStockTab();
            RefreshDebtTab();
            RefreshMacroTab();

            _pnlPLChart?.Invalidate();
            _pnlPieChart?.Invalidate();
            _pnlCCIChart?.Invalidate();
        }

        private void RefreshKPIs()
        {
            decimal cash = _company.Balance;
            decimal inventoryVal = _company.Engine.Facilities.Sum(f => f.InventoryValue);
            decimal workingCap = cash + inventoryVal;
            decimal netWorth = _company.GetNetWorth();

            StockListing? listing = _gameManager?.StockMarket.GetListing(_company.Name);
            decimal marketCap = listing?.MarketCap ?? 0m;
            decimal sharePrice = listing?.SharePrice ?? 0m;
            decimal totalShares = listing?.TotalShares ?? 1m;

            var pnl = _company.Engine.CalculateCurrentPnL();
            decimal eps = totalShares > 0 ? pnl.NetIncome / totalShares : 0m;
            decimal pe = eps > 0 ? sharePrice / eps : 0m;

            _lblCash.Text      = $"${cash:N0}";
            _lblCash.ForeColor = cash >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;
            _lblWorkingCap.Text = $"${workingCap:N0}";
            _lblMarketCap.Text  = $"${marketCap:N0}";
            _lblEPS.Text        = $"${eps:F2}";
            _lblEPS.ForeColor   = eps >= 0 ? ThemeManager.AccentColor : ThemeManager.NegativeColor;
            _lblPE.Text         = pe > 0 ? $"{pe:F1}x" : "—";
        }

        private void RefreshPLTab()
        {
            var pnl  = _company.Engine.CalculateCurrentPnL();
            var prev = _company.Engine.MonthlyHistory.Count > 0
                ? _company.Engine.MonthlyHistory[^1].PnL
                : new PnLStatement();

            var rows = new (string name, decimal cur, decimal prv)[]
            {
                ("Przychody (Revenue)",               pnl.Revenue,       prev.Revenue),
                ("  Surowce (COGS)",                  pnl.RawMaterials,  prev.RawMaterials),
                ("  Zysk Brutto (Gross Profit)",       pnl.GrossProfit,   prev.GrossProfit),
                ("  Logistyka",                        pnl.Logistics,     prev.Logistics),
                ("  Marketing",                        pnl.Marketing,     prev.Marketing),
                ("  Wynagrodzenia",                    pnl.Salaries,      prev.Salaries),
                ("EBITDA",                             pnl.EBITDA,        prev.EBITDA),
                ("  Amortyzacja",                      pnl.Depreciation,  prev.Depreciation),
                ("EBIT (Zysk operacyjny)",             pnl.EBIT,          prev.EBIT),
                ("  Odsetki (Interest)",               pnl.Interest,      prev.Interest),
                ("EBT (Przed podatkiem)",              pnl.EBT,           prev.EBT),
                ("  Podatek CIT",                      pnl.CorporateTax,  prev.CorporateTax),
                ("Zysk Netto (Net Income)",            pnl.NetIncome,     prev.NetIncome),
            };

            _dgvPL.Rows.Clear();
            foreach (var (name, cur, prv) in rows)
            {
                decimal diff = cur - prv;
                string diffStr = diff == 0 ? "—" : (diff > 0 ? $"▲ {diff:N0}" : $"▼ {Math.Abs(diff):N0}");
                _dgvPL.Rows.Add(name, $"${cur:N0}", $"${prv:N0}", diffStr);
            }
        }

        private void RefreshStockTab()
        {
            if (_gameManager == null) return;

            var listing = _gameManager.StockMarket.GetListing(_company.Name);
            if (listing != null)
            {
                _lblPlayerPct.Text = $"{listing.PlayerOwnershipPercent:F1}%  ({listing.PlayerOwnedShares:F0} / {listing.TotalShares:F0} akcji)";
            }

            // Ranking
            var dgvRank = FindControl<DataGridView>("dgvRank");
            if (dgvRank != null)
            {
                dgvRank.Rows.Clear();
                var ranking = _gameManager.StockMarket.GetRanking();
                for (int i = 0; i < ranking.Count; i++)
                {
                    var l = ranking[i];
                    decimal ownPct = 0m;
                    if (_company.OwnedShares.TryGetValue(l.CompanyName, out var owned) && l.TotalShares > 0)
                        ownPct = owned / l.TotalShares * 100m;
                    string status = l.CompanyName == _company.Name ? "★ Twoja spółka" : (ownPct > 50 ? "✅ Kontrola" : "");
                    dgvRank.Rows.Add(i + 1, l.CompanyName, $"${l.MarketCap:N0}", $"${l.SharePrice:F2}", $"{ownPct:F1}%");
                    if (l.CompanyName == _company.Name)
                        dgvRank.Rows[i].DefaultCellStyle.ForeColor = ThemeManager.GoldColor;
                }
            }

            _pnlPieChart?.Invalidate();
        }

        private void RefreshDebtTab()
        {
            if (_gameManager == null) return;

            _dgvLoans.Rows.Clear();
            foreach (var loan in _gameManager.Banking.Loans)
            {
                decimal dynamicRate = _gameManager.Macro.GetLoanRate(loan.Type) * 100m;
                _dgvLoans.Rows.Add(
                    loan.Type.ToString(),
                    $"${loan.OutstandingBalance:N2}",
                    $"{dynamicRate:F2}%",
                    $"${loan.MonthlyPayment:N2}",
                    loan.MonthsRemaining
                );
            }

            decimal totalDebt = _gameManager.Banking.TotalDebt;
            decimal monthlyPay = _gameManager.Banking.TotalMonthlyPayments;
            var pnl = _company.Engine.CalculateCurrentPnL();
            decimal netWorth = _company.GetNetWorth();
            decimal invested = netWorth + totalDebt;
            decimal roic = invested > 0 ? pnl.EBIT / invested * 100m : 0m;
            decimal avgRate = totalDebt > 0 ? (-pnl.Interest / totalDebt * 100m) : 0m;

            _lblTotalDebt.Text      = $"${totalDebt:N2}";
            _lblMonthlyPayment.Text = $"${monthlyPay:N2}/mies.";
            _lblROIC.Text           = $"{roic:F1}%";
            _lblROIC.ForeColor      = roic > 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;
            _lblDebtCost.Text       = totalDebt > 0 ? $"{avgRate:F2}%" : "Brak długu";
        }

        private void RefreshMacroTab()
        {
            if (_gameManager == null) return;

            var macro = _gameManager.Macro;
            _lblPhase.Text     = macro.PhaseLabel;
            _lblPhase.ForeColor = macro.PhaseColor;
            _lblCCI.Text       = $"{macro.ConsumerConfidenceIndex:F1} / 100";
            _lblInflation.Text  = $"{macro.InflationRate:F2}%";
            _lblRate.Text       = $"{macro.BaseInterestRate:F2}%";

            _pnlCCIChart?.Invalidate();
        }

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────

        private void AddSectionHeader(Control parent, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = ThemeManager.HeaderFont,
                ForeColor = ThemeManager.GoldColor,
                Location  = new Point(x, y),
                AutoSize  = true
            };
            parent.Controls.Add(lbl);
        }

        private Label AddInfoLabel(Control parent, string caption, string value, int x, int y, Color color)
        {
            parent.Controls.Add(new Label
            {
                Text      = caption,
                Font      = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location  = new Point(x, y),
                AutoSize  = true
            });
            var lbl = new Label
            {
                Text      = value,
                Font      = new Font("Consolas", 10, FontStyle.Bold),
                ForeColor = color,
                Location  = new Point(x + 4, y + 16),
                AutoSize  = true
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private static void DrawCenteredText(Graphics g, Rectangle rc, string text, Color color)
        {
            using var brush = new SolidBrush(color);
            var size = g.MeasureString(text, ThemeManager.SmallFont);
            g.DrawString(text, ThemeManager.SmallFont, brush,
                rc.X + (rc.Width - size.Width) / 2f,
                rc.Y + (rc.Height - size.Height) / 2f);
        }

        private T? FindControl<T>(string name) where T : Control
        {
            return FindControlRecursive<T>(this, name);
        }

        private static T? FindControlRecursive<T>(Control root, string name) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t && c.Name == name) return t;
                var found = FindControlRecursive<T>(c, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
