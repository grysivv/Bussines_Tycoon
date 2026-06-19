using System;
using System.Drawing;
using System.Windows.Forms;

namespace Conglomerate.UI
{
    public static class ThemeManager
    {
        // Autentyczna paleta kolorów w stylu Capitalism Lab (wojskowo-stalowa szarość, slate-blue nagłówki i złote/zielone wyróżnienia)
        public static readonly Color BackgroundColor = Color.FromArgb(24, 28, 36);      // Głęboka ciemna stal
        public static readonly Color PanelBackground = Color.FromArgb(32, 38, 48);      // Ciemnoszary panel
        public static readonly Color HeaderBackground = Color.FromArgb(44, 58, 76);     // Slate Blue - nagłówki okien i tabel
        public static readonly Color TextColor = Color.FromArgb(240, 244, 248);          // Srebrno-biały tekst
        public static readonly Color MutedTextColor = Color.FromArgb(148, 163, 184);     // Przygaszony szary
        public static readonly Color AccentColor = Color.FromArgb(52, 152, 219);         // Jasnoniebieski akcent
        public static readonly Color AccentHoverColor = Color.FromArgb(41, 128, 185);    // Ciemniejszy niebieski
        public static readonly Color PositiveColor = Color.FromArgb(16, 185, 129);       // Szmaragdowa zieleń dla zysków
        public static readonly Color NegativeColor = Color.FromArgb(239, 68, 68);        // Koralowa czerwień dla strat
        public static readonly Color HighlightColor = Color.FromArgb(245, 158, 11);       // Złoty/bursztynowy dla wartości i ważnych statystyk
        public static readonly Color BorderColor = Color.FromArgb(56, 70, 92);           // Stalowo-niebieskie ramki

        // Czcionki
        public static readonly Font TitleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        public static readonly Font HeaderFont = new Font("Segoe UI", 10, FontStyle.Bold);
        public static readonly Font DefaultFont = new Font("Segoe UI", 9, FontStyle.Regular);
        public static readonly Font DataFont = new Font("Consolas", 9, FontStyle.Regular); // Monospace dla danych liczbowych
        public static readonly Font BoldDataFont = new Font("Consolas", 9, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8, FontStyle.Regular);

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
            {
                ApplyThemeRecursive(child);
            }
        }

        private static void ApplyThemeRecursive(Control control)
        {
            // Zabezpieczenie przed modyfikacją renderera MonoGame
            string typeName = control.GetType().Name;
            if (typeName == "IsometricMapControl" || typeName == "MonoGameControl" || 
                (control.GetType().Namespace != null && control.GetType().Namespace.Contains("Microsoft.Xna")))
            {
                return;
            }

            if (control is Panel panel)
            {
                // Unikamy zmiany tła dla specyficznych przeźroczystych nakładek newsowych lub innych
                if (panel.Name == "pnlNewsTicker")
                {
                    // Pozostawiamy bez zmian
                }
                else if (panel.Height <= 3 || panel.Width <= 3 || 
                         panel.Name.Contains("Border", StringComparison.OrdinalIgnoreCase) || 
                         panel.Name.Contains("Line", StringComparison.OrdinalIgnoreCase) || 
                         panel.Name.Contains("Divider", StringComparison.OrdinalIgnoreCase))
                {
                    panel.BackColor = BorderColor;
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
                // Jeśli etykieta reprezentuje wartości liczbowe (np. zaczyna się od $, zawiera zł), ustawiamy czcionkę Consolas
                if (label.Text.Contains("$") || label.Text.Contains("zł") || label.Text.Contains("%") || label.Name.Contains("Value") || label.Name.Contains("PnL") || label.Name.Contains("Cash") || label.Name.Contains("Debt"))
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
                gb.ForeColor = HighlightColor;
                gb.Font = HeaderFont;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child);
            }
        }

        public static void ApplyButtonTheme(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = BorderColor;
            
            // Kolor tła przycisku zależy od jego przeznaczenia (zielony kup, czerwony sprzedaj, domyślnie slate blue)
            if (button.BackColor == Color.Empty || button.BackColor == AccentColor || button.BackColor.R == 41)
            {
                button.BackColor = HeaderBackground;
            }
            button.ForeColor = TextColor;
            button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void ApplySecondaryButtonTheme(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = BackgroundColor;
            btn.ForeColor = MutedTextColor;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
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
                    window.Top += e.Y - lastCursorPos.Y;
                }
            };

            header.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = false;
                }
            };
        }

        public static void ApplyDataGridViewTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = BackgroundColor;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.DefaultCellStyle.BackColor = BackgroundColor;
            dgv.DefaultCellStyle.ForeColor = TextColor;
            dgv.DefaultCellStyle.SelectionBackColor = HeaderBackground;
            dgv.DefaultCellStyle.SelectionForeColor = HighlightColor;
            dgv.DefaultCellStyle.Font = DataFont;
            
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            dgv.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            
            dgv.RowHeadersVisible = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(BackgroundColor.R + 6, BackgroundColor.G + 6, BackgroundColor.B + 8);
            dgv.GridColor = BorderColor;
        }
    }
}
