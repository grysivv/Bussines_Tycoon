using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Conglomerate.Financials;
using System.Collections.Generic;

namespace Conglomerate.UI.Controls
{
    /// <summary>
    /// Dolny pasek HUD wzorowany na Capitalism Lab:
    /// [TICKER NEWS] / [COMPANY | CASH | NAV BUTTONS | DATE | CZAS]
    /// </summary>
    public class BottomHudControl : UserControl
    {
        private Company _company;
        private GameManager _gameManager;

        // Ticker
        private Panel pnlTicker;
        private Label lblTickerText;
        private List<string> _newsItems = new List<string>();
        private int _tickerIndex = 0;
        private System.Windows.Forms.Timer _tickerTimer;

        // Toolbar
        private Panel pnlToolbar;
        private Label lblCompanyName;
        private Label lblCashValue;
        private Label lblProfitValue;
        private Label lblDateValue;

        // Przyciski nawigacji
        private Button btnMap;
        private Button btnBuild;
        private Button btnCorp;
        private Button btnFinance;
        private Button btnStock;
        private Button btnBank;
        private Button btnLogistics;
        private Button btnMarketing;
        private Button btnExecutives;
        private Button btnHR;
        private Button[] _navButtons;
        private Button _activeNavButton;

        // Sterowanie czasem
        private Button[] _speedButtons;
        private Button? _activeSpeedButton;
        private Button _btnPause;

        // Zdarzenia
        /// <summary>Kliknięto przycisk nawigacyjny. Argument = klucz modułu ("map", "build", "finance" itp.).</summary>
        public event EventHandler<string>? OnModuleClicked;
        public event EventHandler? OnTimePaused;
        /// <summary>Wybrano prędkość gry. Argument = interwał ticka godzinowego w ms.</summary>
        public event EventHandler<int>? OnSpeedSelected;

        public BottomHudControl(Company company, GameManager gameManager)
        {
            _company = company;
            _gameManager = gameManager;
            InitializeComponent();

            System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer { Interval = 300 };
            updateTimer.Tick += (s, e) => RefreshData();
            updateTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeManager.ToolbarBackground;
            this.DoubleBuffered = true;

            // ── TICKER (górny pasek) ──────────────────────────────────────────────
            pnlTicker = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 22,
                BackColor = Color.FromArgb(6, 14, 26),
                Name      = "pnlNewsTicker"
            };
            pnlTicker.Paint += PnlTicker_Paint;

            lblTickerText = new Label
            {
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(160, 200, 240),
                BackColor = Color.Transparent,
                Font      = new Font("Consolas", 8, FontStyle.Regular),
                Padding   = new Padding(6, 0, 0, 0)
            };
            pnlTicker.Controls.Add(lblTickerText);

            AddNewsItem("Witamy w Conglomerate Tycoon! Zbuduj swoje imperium handlowe.");
            AddNewsItem("Rynki surowców otwarte. Pierwsze dostawy możliwe od zaraz.");

            _tickerTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _tickerTimer.Tick += TickerTimer_Tick;
            _tickerTimer.Start();
            ShowNextTicker();

            // ── TOOLBAR (główny pasek) ────────────────────────────────────────────
            pnlToolbar = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ThemeManager.ToolbarBackground
            };
            pnlToolbar.Paint += PnlToolbar_Paint;

            // Blok firmy (lewy) ───────────────────────────────────────────────────
            Panel pnlCompanyBlock = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 190,
                BackColor = Color.FromArgb(10, 22, 38)
            };
            pnlCompanyBlock.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(pen, p.Width - 1, 0, p.Width - 1, p.Height);
            };

            lblCompanyName = new Label
            {
                Text      = _company?.Name ?? "My Company",
                ForeColor = ThemeManager.GoldColor,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize  = false,
                Width     = 180,
                Height    = 18,
                Location  = new Point(8, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblCashLabel = new Label
            {
                Text      = "GOTÓWKA",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.HudLabelFont,
                AutoSize  = false, Width = 80, Height = 14,
                Location  = new Point(8, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblCashValue = new Label
            {
                Text      = "$0",
                ForeColor = ThemeManager.GoldColor,
                Font      = ThemeManager.HudCashFont,
                AutoSize  = false, Width = 178, Height = 20,
                Location  = new Point(8, 34),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblProfitLabel = new Label
            {
                Text      = "ZYSK/MC",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.HudLabelFont,
                AutoSize  = false, Width = 80, Height = 14,
                Location  = new Point(8, 52),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblProfitValue = new Label
            {
                Text      = "$0",
                ForeColor = ThemeManager.PositiveColor,
                Font      = new Font("Consolas", 9, FontStyle.Bold),
                AutoSize  = false, Width = 178, Height = 16,
                Location  = new Point(8, 64),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlCompanyBlock.Controls.Add(lblCompanyName);
            pnlCompanyBlock.Controls.Add(lblCashLabel);
            pnlCompanyBlock.Controls.Add(lblCashValue);
            pnlCompanyBlock.Controls.Add(lblProfitLabel);
            pnlCompanyBlock.Controls.Add(lblProfitValue);

            // Blok daty + czasu (prawy) ───────────────────────────────────────────
            Panel pnlDateBlock = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 232,
                BackColor = Color.FromArgb(10, 22, 38)
            };
            pnlDateBlock.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
            };

            Label lblDateLabel = new Label
            {
                Text      = "DATA / CZAS",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.HudLabelFont,
                AutoSize  = false, Width = 220, Height = 14,
                Location  = new Point(8, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblDateValue = new Label
            {
                Text      = "Dzień 1 • 08:00",
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.HudDateFont,
                AutoSize  = false, Width = 220, Height = 24,
                Location  = new Point(6, 16),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlDateBlock.Controls.Add(lblDateLabel);
            pnlDateBlock.Controls.Add(lblDateValue);

            // Przyciski sterowania czasem: [⏸][1x][2x][3x][5x][10x]
            Button MakeSpeedButton(string text, string tip, int x, int intervalMs)
            {
                var b = ThemeManager.CreateTimeButton(text);
                b.AccessibleName = tip;
                b.Size     = new Size(33, 26);
                b.Font     = new Font("Consolas", 8.5f, FontStyle.Bold);
                b.Location = new Point(x, 48);
                b.ToolTipText(tip);
                b.Click   += (s, e) =>
                {
                    SetActiveSpeed(b);
                    OnSpeedSelected?.Invoke(this, intervalMs);
                };
                pnlDateBlock.Controls.Add(b);
                return b;
            }

            _btnPause = ThemeManager.CreateTimeButton("⏸");
            _btnPause.AccessibleName = "Pauza";
            _btnPause.Size     = new Size(33, 26);
            _btnPause.Location = new Point(6, 48);
            _btnPause.ToolTipText("Pauza");
            _btnPause.Click   += (s, e) =>
            {
                SetActiveSpeed(null);
                _btnPause.BackColor = ThemeManager.HeaderBackground;
                _btnPause.ForeColor = ThemeManager.GoldColor;
                _btnPause.FlatAppearance.BorderColor = ThemeManager.GoldColor;
                OnTimePaused?.Invoke(this, EventArgs.Empty);
            };
            pnlDateBlock.Controls.Add(_btnPause);

            var btn1x  = MakeSpeedButton("1x",  "Prędkość 1x (1h / 1s)",   41,  1000);
            var btn2x  = MakeSpeedButton("2x",  "Prędkość 2x",             76,  500);
            var btn3x  = MakeSpeedButton("3x",  "Prędkość 3x",             111, 333);
            var btn5x  = MakeSpeedButton("5x",  "Prędkość 5x",             146, 200);
            var btn10x = MakeSpeedButton("10x", "Prędkość 10x",            181, 100);

            _speedButtons = new[] { btn1x, btn2x, btn3x, btn5x, btn10x };

            // Domyślnie aktywna prędkość 1x
            SetActiveSpeed(btn1x);

            // Blok nawigacji (środek) ─────────────────────────────────────────────
            Panel pnlNavBlock = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ThemeManager.ToolbarBackground
            };

            // Ikony: mapka, młotek, firma, finanse
            btnMap       = ThemeManager.CreateHudNavButton("🗺", "Mapa",      ThemeManager.AccentColor);
            btnBuild     = ThemeManager.CreateHudNavButton("🔨", "Buduj",     Color.FromArgb(200, 130, 60));
            btnCorp      = ThemeManager.CreateHudNavButton("🏢", "Firma",     Color.FromArgb(80, 160, 240));
            btnFinance   = ThemeManager.CreateHudNavButton("💰", "Finanse",   ThemeManager.GoldColor);
            btnStock      = ThemeManager.CreateHudNavButton("📈", "Giełda",     ThemeManager.PositiveColor);
            btnBank       = ThemeManager.CreateHudNavButton("🏦", "Bank",       Color.FromArgb(120, 180, 250));
            btnLogistics  = ThemeManager.CreateHudNavButton("🚚", "Logistyka",  Color.FromArgb(240, 180, 50));
            btnMarketing  = ThemeManager.CreateHudNavButton("📢", "Marketing",  Color.FromArgb(240, 150, 60));
            btnExecutives = ThemeManager.CreateHudNavButton("👔", "Dyrektorzy", Color.FromArgb(200, 150, 255));
            btnHR         = ThemeManager.CreateHudNavButton("👥", "Kadry",      Color.FromArgb(220, 120, 220));

            _navButtons = new[] { btnMap, btnBuild, btnCorp, btnFinance, btnStock, btnBank, btnLogistics, btnMarketing, btnExecutives, btnHR };

            FlowLayoutPanel flowNav = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Padding       = new Padding(8, 4, 8, 0),
                AutoSize      = false
            };

            foreach (var b in _navButtons)
            {
                b.Margin = new Padding(3, 0, 3, 0);
                flowNav.Controls.Add(b);
            }

            void WireNavButton(Button btn, string key)
            {
                btn.Click += (s, e) =>
                {
                    SetActiveNavBtn(btn);
                    OnModuleClicked?.Invoke(this, key);
                };
            }

            WireNavButton(btnMap,       "map");
            WireNavButton(btnBuild,     "build");
            WireNavButton(btnCorp,      "corporate");
            WireNavButton(btnFinance,   "finance");
            WireNavButton(btnStock,     "stock");
            WireNavButton(btnBank,      "bank");
            WireNavButton(btnLogistics, "logistics");
            WireNavButton(btnMarketing, "market");
            WireNavButton(btnExecutives,"executives");
            WireNavButton(btnHR,        "hr");

            pnlNavBlock.Controls.Add(flowNav);

            // Kolejność dodawania do pnlToolbar (ważna dla Dockingu)
            pnlToolbar.Controls.Add(pnlNavBlock);
            pnlToolbar.Controls.Add(pnlCompanyBlock);
            pnlToolbar.Controls.Add(pnlDateBlock);

            // Dodawanie do UserControl
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlTicker);

            // Domyślnie aktywna: Mapa
            SetActiveNavBtn(btnMap);
        }

        private void PnlTicker_Paint(object? sender, PaintEventArgs e)
        {
            var p = (Panel)sender!;
            using var pen = new Pen(ThemeManager.SeparatorColor, 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private void PnlToolbar_Paint(object? sender, PaintEventArgs e)
        {
            var p = (Panel)sender!;
            using var pen = new Pen(ThemeManager.BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); // Górna linia oddzielająca od mapy
        }

        private void SetActiveNavBtn(Button active)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.Tag = false;
                _activeNavButton.Invalidate();
            }
            _activeNavButton = active;
            active.Tag = true;
            active.Invalidate();
        }

        private void SetActiveSpeed(Button? active)
        {
            _activeSpeedButton = active;

            // Wybór dowolnej prędkości wyłącza podświetlenie pauzy
            if (_btnPause != null)
            {
                _btnPause.BackColor = Color.FromArgb(20, 40, 65);
                _btnPause.ForeColor = ThemeManager.TextColor;
                _btnPause.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            }

            if (_speedButtons == null) return;

            foreach (var b in _speedButtons)
            {
                bool on = b == active;
                b.BackColor = on ? ThemeManager.HeaderBackground : Color.FromArgb(20, 40, 65);
                b.ForeColor = on ? ThemeManager.GoldColor : ThemeManager.TextColor;
                b.FlatAppearance.BorderColor = on ? ThemeManager.GoldColor : ThemeManager.BorderColor;
            }
        }

        public void AddNewsItem(string text)
        {
            string datePrefix = _gameManager != null ? $"[Dzień {_gameManager.CurrentDay}] " : "";
            _newsItems.Add(datePrefix + text);
            if (_newsItems.Count > 100) _newsItems.RemoveAt(0);
        }

        private void ShowNextTicker()
        {
            if (_newsItems.Count == 0) return;
            _tickerIndex = (_tickerIndex + 1) % _newsItems.Count;
            lblTickerText.Text = "  ►  " + _newsItems[_tickerIndex];
        }

        private void TickerTimer_Tick(object? sender, EventArgs e) => ShowNextTicker();

        public void RefreshData()
        {
            if (_company != null)
            {
                lblCashValue.Text  = $"${_company.Balance:N0}";
                lblCashValue.ForeColor = _company.Balance >= 0 ? ThemeManager.GoldColor : ThemeManager.NegativeColor;

                decimal profit = 0;
                lblProfitValue.Text     = profit >= 0 ? $"+${profit:N0}" : $"-${Math.Abs(profit):N0}";
                lblProfitValue.ForeColor = profit >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;
            }
            if (_gameManager != null)
                lblDateValue.Text = $"Dzień {_gameManager.CurrentDay} • {_gameManager.CurrentHour:00}:00";
        }
    }

    // Extension - tooltip helper
    internal static class ButtonExtensions
    {
        private static ToolTip _tip = new ToolTip();
        public static void ToolTipText(this Button btn, string text) => _tip.SetToolTip(btn, text);
    }
}
