using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Conglomerate.UI;

namespace Conglomerate.UI.Controls
{
    /// <summary>
    /// Lekka ramka-nakładka (overlay) w stylu Modern UI, hostująca dowolną kontrolkę
    /// (np. istniejące panele StockMarketForm / BankingForm). Zapewnia nagłówek z tytułem,
    /// przycisk zamknięcia oraz możliwość przeciągania okna.
    /// </summary>
    public class CapLabOverlayHost : UserControl
    {
        private readonly Control _child;

        public CapLabOverlayHost(string title, int width, int height, Control child)
        {
            _child = child;
            InitializeComponent(title, width, height);
        }

        private void InitializeComponent(string title, int width, int height)
        {
            this.Size           = new Size(width, height);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = ThemeManager.HeaderBackground
            };
            pnlHeader.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var brush = new LinearGradientBrush(
                    p.ClientRectangle,
                    ThemeManager.HeaderBackground,
                    Color.FromArgb(10, 22, 40),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);

                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, p.Width, 0);

                using var sepPen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(sepPen, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.TextColor,
                Location  = new Point(16, 10),
                AutoSize  = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(32, 28),
                Location = new Point(this.Width - 44, 8),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.ToolTipText("Zamknij");
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.Click    += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            // ── Obszar zawartości ───────────────────────────────────────────────
            Panel pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ThemeManager.PanelBackground,
                Padding   = new Padding(8)
            };
            _child.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(_child);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);

            ThemeManager.MakeDraggable(pnlHeader, this);
        }
    }
}
