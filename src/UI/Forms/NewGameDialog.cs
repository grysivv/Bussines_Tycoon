using System;
using System.Drawing;
using System.Windows.Forms;
using Conglomerate.UI;

namespace Conglomerate.UI.Forms
{
    /// <summary>
    /// Okno konfiguracji nowej gry — pozwala graczowi ustawić nazwę firmy,
    /// kapitał startowy, dług, podatek, rozmiar mapy i agresywność konkurencji.
    /// Po zatwierdzeniu udostępnia gotowy obiekt <see cref="GameGenerationSettings"/>.
    /// </summary>
    public class NewGameDialog : Form
    {
        private TextBox       _txtCompanyName  = null!;
        private NumericUpDown _numStartingCash = null!;
        private NumericUpDown _numStartingDebt = null!;
        private NumericUpDown _numTaxRate      = null!;
        private ComboBox      _cmbMapSize      = null!;
        private ComboBox      _cmbAggressiveness = null!;
        private NumericUpDown _numStartYear    = null!;

        /// <summary>Ustawienia wybrane przez gracza (ważne po DialogResult.OK).</summary>
        public GameGenerationSettings Settings { get; private set; } = new GameGenerationSettings();

        public NewGameDialog()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text            = "Nowa Gra";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ClientSize      = new Size(440, 540);
            BackColor       = ThemeManager.BackgroundColor;
            ForeColor       = ThemeManager.TextColor;
            Font            = ThemeManager.DefaultFont;

            var lblHeader = new Label
            {
                Text      = "KONFIGURACJA NOWEJ GRY",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                AutoSize  = false,
                Size      = new Size(400, 32),
                Location  = new Point(20, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblHeader);

            var separator = new Panel
            {
                Location  = new Point(20, 54),
                Size      = new Size(400, 1),
                BackColor = ThemeManager.BorderColor
            };
            Controls.Add(separator);

            int y = 74;

            // ── Nazwa firmy ─────────────────────────────────────────
            _txtCompanyName = new TextBox
            {
                Text       = Settings.CompanyName,
                MaxLength  = 32,
                BackColor  = ThemeManager.PanelBackground,
                ForeColor  = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            AddRow("Nazwa firmy", _txtCompanyName, ref y);

            // ── Kapitał startowy ────────────────────────────────────
            _numStartingCash = NewNumeric(1000, 1_000_000_000, 10000, (decimal)Settings.StartingCash);
            _numStartingCash.ThousandsSeparator = true;
            AddRow("Kapitał startowy (zł)", _numStartingCash, ref y);

            // ── Kredyt startowy (dług) ──────────────────────────────
            _numStartingDebt = NewNumeric(0, 1_000_000_000, 10000, (decimal)Settings.StartingDebt);
            _numStartingDebt.ThousandsSeparator = true;
            AddRow("Kredyt startowy (zł)", _numStartingDebt, ref y,
                   hint: "Długoterminowa pożyczka doliczona do salda");

            // ── Podatek CIT ─────────────────────────────────────────
            _numTaxRate = NewNumeric(0, 50, 1, Math.Round(Settings.GlobalCorporateTax * 100m));
            AddRow("Podatek CIT (%)", _numTaxRate, ref y);

            // ── Rozmiar mapy ────────────────────────────────────────
            _cmbMapSize = NewCombo(new[] { "Mała (8×8)", "Średnia (10×10)", "Duża (14×14)", "Ogromna (18×18)" }, 1);
            AddRow("Rozmiar mapy", _cmbMapSize, ref y);

            // ── Agresywność konkurencji ─────────────────────────────
            _cmbAggressiveness = NewCombo(new[] { "Niska", "Normalna", "Agresywna" }, 1);
            AddRow("Agresywność konkurencji", _cmbAggressiveness, ref y);

            // ── Rok startowy ────────────────────────────────────────
            _numStartYear = NewNumeric(2000, 3000, 1, Settings.StartYear);
            AddRow("Rok startowy", _numStartYear, ref y);

            // ── Przyciski ───────────────────────────────────────────
            var btnStart = new Button
            {
                Text     = "▶  Rozpocznij",
                Size     = new Size(160, 38),
                Location = new Point(ClientSize.Width - 180, ClientSize.Height - 54)
            };
            ThemeManager.ApplyButtonTheme(btnStart);
            btnStart.BackColor = ThemeManager.HeaderBackground;
            btnStart.ForeColor = ThemeManager.GoldColor;
            btnStart.Click += OnStartClicked;
            Controls.Add(btnStart);

            var btnCancel = new Button
            {
                Text         = "Anuluj",
                Size         = new Size(110, 38),
                Location     = new Point(ClientSize.Width - 300, ClientSize.Height - 54),
                DialogResult = DialogResult.Cancel
            };
            ThemeManager.ApplySecondaryButtonTheme(btnCancel);
            Controls.Add(btnCancel);

            AcceptButton = btnStart;
            CancelButton = btnCancel;
        }

        // ── Helpery budowania kontrolek ─────────────────────────────

        private void AddRow(string caption, Control input, ref int y, string? hint = null)
        {
            var lbl = new Label
            {
                Text      = caption,
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.DefaultFont,
                AutoSize  = false,
                Size      = new Size(220, 22),
                Location  = new Point(20, y + 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lbl);

            input.Size     = new Size(180, 26);
            input.Location = new Point(240, y);
            Controls.Add(input);

            if (hint != null)
            {
                var lblHint = new Label
                {
                    Text      = hint,
                    ForeColor = Color.FromArgb(90, 115, 140),
                    Font      = ThemeManager.SmallFont,
                    AutoSize  = false,
                    Size      = new Size(400, 16),
                    Location  = new Point(20, y + 28)
                };
                Controls.Add(lblHint);
                y += 18;
            }

            y += 44;
        }

        private static NumericUpDown NewNumeric(decimal min, decimal max, decimal step, decimal value)
        {
            return new NumericUpDown
            {
                Minimum     = min,
                Maximum     = max,
                Increment   = step,
                Value       = Math.Clamp(value, min, max),
                BackColor   = ThemeManager.PanelBackground,
                ForeColor   = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = ThemeManager.DataFont,
                TextAlign   = HorizontalAlignment.Right
            };
        }

        private static ComboBox NewCombo(string[] items, int selectedIndex)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = ThemeManager.PanelBackground,
                ForeColor     = ThemeManager.TextColor,
                FlatStyle     = FlatStyle.Flat
            };
            cmb.Items.AddRange(items);
            cmb.SelectedIndex = selectedIndex;
            return cmb;
        }

        // ── Zatwierdzenie ───────────────────────────────────────────

        private void OnStartClicked(object? sender, EventArgs e)
        {
            string name = _txtCompanyName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Podaj nazwę firmy.", "Nowa Gra",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtCompanyName.Focus();
                return;
            }

            (int w, int h) = _cmbMapSize.SelectedIndex switch
            {
                0 => (8, 8),
                2 => (14, 14),
                3 => (18, 18),
                _ => (10, 10)
            };

            Settings = new GameGenerationSettings
            {
                CompanyName                 = name,
                StartingCash                = _numStartingCash.Value,
                StartingDebt                = _numStartingDebt.Value,
                GlobalCorporateTax          = _numTaxRate.Value / 100m,
                MapWidth                    = w,
                MapHeight                   = h,
                AICompetitionAggressiveness = _cmbAggressiveness.SelectedItem?.ToString() ?? "Normalna",
                StartYear                   = (int)_numStartYear.Value
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
