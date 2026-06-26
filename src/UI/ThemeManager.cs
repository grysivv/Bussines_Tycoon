using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Conglomerate.UI
{
    public static class ThemeManager
    {
        // Autentyczna paleta Capitalism Lab - ciemna granatowa stal + złote/zielone akcenty
        public static readonly Color BackgroundColor    = Color.FromArgb(8,  16, 28);   // Głęboka nocna granat
        public static readonly Color PanelBackground   = Color.FromArgb(13, 24, 40);   // Ciemny panel
        public static readonly Color HeaderBackground  = Color.FromArgb(18, 38, 62);   // Stalowo-niebieski nagłówek
        public static readonly Color ToolbarBackground = Color.FromArgb(10, 20, 36);   // Pasek narzędzi (nieco ciemniejszy)
        public static readonly Color RowAltColor       = Color.FromArgb(16, 30, 50);   // Alternatywny wiersz tabeli
        public static readonly Color TextColor         = Color.FromArgb(210, 225, 240); // Zimny srebrno-biały
        public static readonly Color MutedTextColor    = Color.FromArgb(120, 145, 170); // Przygaszony
        public static readonly Color AccentColor       = Color.FromArgb(60,  140, 220); // Jasnoniebieski akcent
        public static readonly Color GoldColor         = Color.FromArgb(255, 200, 60);  // Złoty - wyróżnienie finansów
        public static readonly Color PositiveColor     = Color.FromArgb(40,  210, 110); // Szmaragdowa zieleń
        public static readonly Color NegativeColor     = Color.FromArgb(240, 70,  60);  // Koralowa czerwień
        public static readonly Color HighlightColor    = Color.FromArgb(255, 200, 60);  // Złoty = HighlightColor
        public static readonly Color BorderColor       = Color.FromArgb(35,  68, 108);  // Stalowo-niebieska ramka
        public static readonly Color SeparatorColor    = Color.FromArgb(25,  50,  82);  // Cienki separator

        // Czcionki
        public static readonly Font TitleFont    = new Font("Segoe UI", 14, FontStyle.Bold);
        public static readonly Font HeaderFont   = new Font("Segoe UI", 9,  FontStyle.Bold);
        public static readonly Font DefaultFont  = new Font("Segoe UI", 9,  FontStyle.Regular);
        public static readonly Font DataFont     = new Font("Consolas", 9,  FontStyle.Regular);
        public static readonly Font BoldDataFont = new Font("Consolas", 9,  FontStyle.Bold);
        public static readonly Font SmallFont    = new Font("Segoe UI", 8,  FontStyle.Regular);
        public static readonly Font IconFont     = new Font("Segoe UI", 18, FontStyle.Bold);
        public static readonly Font HudCashFont  = new Font("Consolas", 12, FontStyle.Bold);
        public static readonly Font HudLabelFont = new Font("Segoe UI", 7,  FontStyle.Regular);
        public static readonly Font HudDateFont  = new Font("Consolas", 11, FontStyle.Bold);

        public static void ApplyTheme(Control control)
        {
            if (control is Form form)
            {
                form.BackColor = BackgroundColor;
                form.ForeColor = TextColor;
            }
            else
            {
                control.BackColor = PanelBackground;
                control.ForeColor = TextColor;
            }
            control.Font = DefaultFont;

            foreach (Control child in control.Controls)
                ApplyThemeRecursive(child);
        }

        private static void ApplyThemeRecursive(Control control)
        {
            string typeName = control.GetType().Name;
            if (typeName == "IsometricMapControl" || typeName == "MonoGameControl" ||
                (control.GetType().Namespace?.Contains("Microsoft.Xna") == true))
                return;

            if (control is Panel panel)
            {
                if (panel.Name == "pnlNewsTicker")
                {
                    // Pozostawiamy nienaruszone (własny styl)
                }
                else if (panel.Height <= 3 || panel.Width <= 3 ||
                         panel.Name.Contains("Border",    StringComparison.OrdinalIgnoreCase) ||
                         panel.Name.Contains("Line",      StringComparison.OrdinalIgnoreCase) ||
                         panel.Name.Contains("Divider",   StringComparison.OrdinalIgnoreCase) ||
                         panel.Name.Contains("Separator", StringComparison.OrdinalIgnoreCase))
                {
                    panel.BackColor = SeparatorColor;
                }
                else
                {
                    panel.BackColor = PanelBackground;
                }
                panel.ForeColor = TextColor;
            }
            else if (control is Label label)
            {
                label.ForeColor = TextColor;
                if (label.Text.Contains("$") || label.Text.Contains("zł") || label.Text.Contains("%") ||
                    label.Name.Contains("Value") || label.Name.Contains("PnL") ||
                    label.Name.Contains("Cash") || label.Name.Contains("Debt"))
                {
                    label.Font = label.Font.Style == FontStyle.Bold ? BoldDataFont : DataFont;
                }
            }
            else if (control is Button button)
            {
                ApplyButtonTheme(button);
            }
            else if (control is DataGridView dgv)
            {
                ApplyDataGridViewTheme(dgv);
            }
            else if (control is ListBox || control is ComboBox || control is TextBox)
            {
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
                if (control is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is NumericUpDown nud)
            {
                nud.BackColor = BackgroundColor;
                nud.ForeColor = TextColor;
                nud.BorderStyle = BorderStyle.FixedSingle;
                nud.Font = DataFont;
            }
            else if (control is TrackBar trk)
            {
                trk.BackColor = PanelBackground;
            }
            else if (control is GroupBox gb)
            {
                gb.BackColor = PanelBackground;
                gb.ForeColor = GoldColor;
                gb.Font = HeaderFont;
            }
            else if (control is TabControl tab)
            {
                tab.BackColor = PanelBackground;
                tab.ForeColor = TextColor;
            }
            else if (control is TabPage tp)
            {
                tp.BackColor = PanelBackground;
                tp.ForeColor = TextColor;
            }

            foreach (Control child in control.Controls)
                ApplyThemeRecursive(child);
        }

        public static void ApplyButtonTheme(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.BackColor = HeaderBackground;
            button.ForeColor = TextColor;
            button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void ApplySecondaryButtonTheme(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = SeparatorColor;
            btn.BackColor = BackgroundColor;
            btn.ForeColor = MutedTextColor;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
        }

        // Przycisk w stylu CL - duży, z ikoną na górze i etykietą na dole
        public static Button CreateHudNavButton(string iconChar, string label, Color iconColor)
        {
            Button btn = new Button();
            btn.Size = new Size(68, 62);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = ToolbarBackground;
            btn.ForeColor = TextColor;
            btn.Cursor = Cursors.Hand;
            btn.Tag = false; // stan: nieaktywny
            btn.AccessibleName = label; // Zapewnia dostępność (screen reader)

            // Tekst: ikona + nowa linia + etykieta (własny Paint)
            btn.Paint += (sender, e) =>
            {
                Button b = (Button)sender;
                bool active = b.Tag is true;
                Color bg = active ? HeaderBackground : ToolbarBackground;
                e.Graphics.Clear(bg);

                // Cienka górna linia aktywnego przycisku
                if (active)
                {
                    using var accentBrush = new SolidBrush(GoldColor);
                    e.Graphics.FillRectangle(accentBrush, 0, 0, b.Width, 3);
                }

                // Ikona
                using var iconBrush = new SolidBrush(active ? GoldColor : iconColor);
                using var iconFont  = new Font("Segoe UI", 16, FontStyle.Bold);
                var iconRect = new RectangleF(0, 4, b.Width, 32);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(iconChar, iconFont, iconBrush, iconRect, sf);

                // Etykieta
                using var labelBrush = new SolidBrush(active ? GoldColor : MutedTextColor);
                using var labelFont  = new Font("Segoe UI", 7, active ? FontStyle.Bold : FontStyle.Regular);
                var labelRect = new RectangleF(0, 36, b.Width, 22);
                e.Graphics.DrawString(label.ToUpper(), labelFont, labelBrush, labelRect, sf);

                // Ramka
                using var pen = new Pen(active ? BorderColor : SeparatorColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, b.Width - 1, b.Height - 1);
            };

            btn.MouseEnter += (s, e) => { ((Button)s).BackColor = HeaderBackground; ((Button)s).Invalidate(); };
            btn.MouseLeave += (s, e) => { ((Button)s).BackColor = (bool)(((Button)s).Tag) ? HeaderBackground : ToolbarBackground; ((Button)s).Invalidate(); };

            return btn;
        }

        // Mały przycisk kontroli czasu
        public static Button CreateTimeButton(string symbol)
        {
            Button btn = new Button();
            btn.Size = new Size(30, 28);
            btn.Text = symbol;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = Color.FromArgb(20, 40, 65);
            btn.ForeColor = TextColor;
            btn.Font = new Font("Consolas", 10, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Padding = Padding.Empty;
            return btn;
        }

        public static void MakeDraggable(Control header, Control window)
        {
            bool isDragging = false;
            Point lastCursorPos = Point.Empty;

            header.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    lastCursorPos = e.Location;
                    window.BringToFront();
                }
            };
            header.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    window.Left += e.X - lastCursorPos.X;
                    window.Top  += e.Y - lastCursorPos.Y;
                }
            };
            header.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left) isDragging = false;
            };
        }

        public static void ApplyDataGridViewTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = BackgroundColor;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = SeparatorColor;

            dgv.DefaultCellStyle.BackColor = BackgroundColor;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = HeaderBackground;
            dgv.DefaultCellStyle.SelectionForeColor = GoldColor;
            dgv.DefaultCellStyle.Font = DataFont;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = RowAltColor;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextColor;

            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GoldColor;
            dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            dgv.ColumnHeadersHeight = 28;

            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowTemplate.Height = 22;
        }

        // Rysuje panel nagłówka okna (gradient) - do użycia w OnPaint
        public static void DrawWindowHeader(Graphics g, Rectangle rect, string title, Font titleFont)
        {
            using var brush = new LinearGradientBrush(rect, HeaderBackground, Color.FromArgb(12, 28, 48), LinearGradientMode.Vertical);
            g.FillRectangle(brush, rect);

            using var textBrush = new SolidBrush(TextColor);
            g.DrawString(title, titleFont, textBrush, new PointF(rect.X + 12, rect.Y + (rect.Height - titleFont.Height) / 2f));

            using var borderPen = new Pen(BorderColor, 1);
            g.DrawLine(borderPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
        }
    }
}
