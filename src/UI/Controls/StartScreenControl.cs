using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Conglomerate.UI.Controls
{
    public class StartScreenControl : UserControl
    {
        public event EventHandler? StartNewGameClicked;
        public event EventHandler? LoadGameClicked;
        public event EventHandler? ExitClicked;

        private Panel pnlMenu;

        public StartScreenControl()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(4, 10, 20);

            // Własny Paint dla gradienta tła i siatki
            this.Paint += StartScreen_Paint;

            // Panel menu (centralny, pływający)
            pnlMenu = new Panel
            {
                Size      = new Size(340, 360),
                BackColor = Color.FromArgb(10, 22, 40)
            };
            pnlMenu.Paint += PnlMenu_Paint;
            this.Controls.Add(pnlMenu);

            this.SizeChanged += (s, e) => CenterMenu();
            CenterMenu();

            // ── Tytuł gry ──────────────────────────────────────────────────────
            Label lblTitle = new Label
            {
                Text      = "CONGLOMERATE",
                Font      = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                AutoSize  = false,
                Size      = new Size(340, 52),
                Location  = new Point(0, 28),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlMenu.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text      = "T  Y  C  O  O  N",
                Font      = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 150, 200),
                AutoSize  = false,
                Size      = new Size(340, 22),
                Location  = new Point(0, 76),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlMenu.Controls.Add(lblSubtitle);

            // Linia złota pod tytułem
            Panel separator = new Panel
            {
                Location  = new Point(40, 106),
                Size      = new Size(260, 2),
                BackColor = Color.FromArgb(120, 90, 20)
            };
            pnlMenu.Controls.Add(separator);

            Label lblTagline = new Label
            {
                Text      = "Zbuduj swoje imperium handlowe",
                Font      = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = ThemeManager.MutedTextColor,
                AutoSize  = false,
                Size      = new Size(340, 20),
                Location  = new Point(0, 114),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlMenu.Controls.Add(lblTagline);

            // ── Przyciski menu ──────────────────────────────────────────────────
            int btnY = 152;
            pnlMenu.Controls.Add(CreateMenuButton("  ▶  Nowa Gra",      Color.FromArgb(30, 60, 100), ThemeManager.GoldColor, ref btnY, () => StartNewGameClicked?.Invoke(this, EventArgs.Empty)));
            pnlMenu.Controls.Add(CreateMenuButton("  ⏩  Wczytaj Grę",  Color.FromArgb(18, 38, 65),  ThemeManager.TextColor,  ref btnY, () => LoadGameClicked?.Invoke(this, EventArgs.Empty)));
            pnlMenu.Controls.Add(CreateMenuButton("  ✕  Wyjście",       Color.FromArgb(14, 26, 46),  ThemeManager.MutedTextColor, ref btnY, () => ExitClicked?.Invoke(this, EventArgs.Empty)));

            // Wersja
            Label lblVersion = new Label
            {
                Text      = "v0.1 Alpha  •  © 2024 Conglomerate Studios",
                Font      = ThemeManager.SmallFont,
                ForeColor = Color.FromArgb(50, 75, 100),
                AutoSize  = false,
                Size      = new Size(340, 20),
                Location  = new Point(0, 332),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlMenu.Controls.Add(lblVersion);
        }

        private Button CreateMenuButton(string text, Color bg, Color fg, ref int yPos, Action onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(260, 40),
                Location  = new Point(40, yPos),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btn.FlatAppearance.MouseOverBackColor = ThemeManager.HeaderBackground;
            btn.Click += (s, e) => onClick();
            yPos += 50;
            return btn;
        }

        private void CenterMenu()
        {
            if (pnlMenu == null) return;
            pnlMenu.Location = new Point(
                (this.Width  - pnlMenu.Width)  / 2,
                (this.Height - pnlMenu.Height) / 2
            );
        }

        private void StartScreen_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var bounds = this.ClientRectangle;

            // Gradient radialny (od środka ciemno-niebieskiego do czarnego)
            using var bgBrush = new LinearGradientBrush(
                bounds,
                Color.FromArgb(8, 18, 36),
                Color.FromArgb(2, 6, 14),
                LinearGradientMode.Vertical
            );
            g.FillRectangle(bgBrush, bounds);

            // Siatka w stylu CL - delikatne linie poziome
            using var gridPen = new Pen(Color.FromArgb(10, 30, 55), 1);
            for (int y = 0; y < bounds.Height; y += 30)
                g.DrawLine(gridPen, 0, y, bounds.Width, y);

            // Subtelna poświata w centrum
            DrawGlow(g, bounds.Width / 2, bounds.Height / 2, 300, Color.FromArgb(15, 30, 60, 100));
        }

        private void DrawGlow(Graphics g, int cx, int cy, int radius, Color color)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using var brush = new PathGradientBrush(path)
            {
                CenterColor    = color,
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(brush, path);
        }

        private void PnlMenu_Paint(object? sender, PaintEventArgs e)
        {
            var p = (Panel)sender!;
            var g = e.Graphics;

            // Tło panelu z delikatnym gradientem
            using var brush = new LinearGradientBrush(
                p.ClientRectangle,
                Color.FromArgb(14, 28, 50),
                Color.FromArgb(8, 18, 36),
                LinearGradientMode.Vertical
            );
            g.FillRectangle(brush, p.ClientRectangle);

            // Złota linia na górze (signet CL)
            using var goldBrush = new LinearGradientBrush(
                new Rectangle(0, 0, p.Width, 3),
                Color.Transparent,
                ThemeManager.GoldColor,
                LinearGradientMode.Horizontal
            );
            // Dwukierunkowy gradient złotej linii
            using var goldPen = new Pen(ThemeManager.GoldColor, 3);
            g.DrawLine(goldPen, 40, 0, p.Width - 40, 0);

            // Ramka
            using var borderPen = new Pen(ThemeManager.BorderColor, 1);
            g.DrawRectangle(borderPen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
