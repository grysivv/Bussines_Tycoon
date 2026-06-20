using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Conglomerate.UI.Controls
{
    public class BuildingInspectorControl : UserControl
    {
        private Building _building;
        private Company? _company;
        private GameManager? _gameManager;

        /// <summary>Wywoływane po ręcznej sprzedaży — pozwala odświeżyć HUD (saldo).</summary>
        public event EventHandler? OnInventoryChanged;

        private Label lblTitle;
        private Label lblTypeValue;
        private Label lblLocationValue;
        private Label lblWorkerExpValue;
        private Label lblTrainingValue;
        private TrackBar tbTrainingBudget;
        private Panel pnlStatusDot;

        // ── Sekcja R&D (widoczna tylko dla RNDCenter) ──
        private Panel _pnlRnd;
        private ComboBox _cmbRndProject;
        private Label _lblRndTech;
        private Label _lblRndStatus;
        private Panel _pnlRndProgressFg;
        private bool _suppressRndEvent;

        // ── Sekcja magazynu / sprzedaży ──
        private Panel _pnlInventory = null!;
        private Panel _invList = null!;
        private CheckBox _chkAutoSell = null!;
        private Label _lblInvEmpty = null!;
        private bool _suppressAutoSellEvent;
        private readonly Dictionary<string, InvRow> _invRows = new(StringComparer.OrdinalIgnoreCase);

        private const int BaseHeight = 400;
        private const int RndHeight = 560;
        private const int InventoryHeight = 620;

        private sealed class InvRow
        {
            public Panel Row = null!;
            public Label Qty = null!;
            public Label Price = null!;
            public Button Sell = null!;
        }

        public BuildingInspectorControl(Building building)
        {
            _building = building;
            InitializeComponent();
            RefreshData();
        }

        private void InitializeComponent()
        {
            this.Size           = new Size(300, 400);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            // Ramka okna
            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = ThemeManager.HeaderBackground
            };
            pnlHeader.Paint += (s, e) =>
            {
                ThemeManager.DrawWindowHeader(e.Graphics, ((Panel)s).ClientRectangle, "", ThemeManager.HeaderFont);
                // Złoty pasek na górze
                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, ((Panel)s).Width, 0);
            };

            lblTitle = new Label
            {
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                Location  = new Point(12, 14),
                AutoSize  = true,
                Text      = "—"
            };
            pnlHeader.Controls.Add(lblTitle);

            pnlStatusDot = new Panel
            {
                Size      = new Size(10, 10),
                Location  = new Point(12, 6),
                BackColor = ThemeManager.PositiveColor
            };
            // Okrągły status
            pnlStatusDot.Paint += (s, e) =>
            {
                var p = (Panel)s;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(p.BackColor);
                e.Graphics.FillEllipse(brush, 0, 0, p.Width - 1, p.Height - 1);
            };
            pnlStatusDot.Visible = false;
            pnlHeader.Controls.Add(pnlStatusDot);

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(28, 28),
                Location = new Point(this.Width - 36, 10),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.Click    += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // ── Zawartość ────────────────────────────────────────────────────────
            Panel pnlContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ThemeManager.BackgroundColor,
                Padding   = new Padding(14, 10, 14, 10)
            };

            int y = 10;

            // Sekcja: Informacje ogólne
            pnlContent.Controls.Add(MakeSectionHeader("INFORMACJE", ref y));
            pnlContent.Controls.Add(MakeRow("Typ:", ref y, out lblTypeValue));
            pnlContent.Controls.Add(MakeRow("Lokalizacja:", ref y, out lblLocationValue));

            y += 8;
            pnlContent.Controls.Add(MakeSeparator(ref y));

            // Sekcja: Personel
            pnlContent.Controls.Add(MakeSectionHeader("PERSONEL", ref y));
            pnlContent.Controls.Add(MakeRow("Doświadczenie:", ref y, out lblWorkerExpValue));

            y += 6;

            // Budżet szkoleniowy
            Label lblTrainingTitle = new Label
            {
                Text      = "BUDŻET SZKOLENIOWY",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(260, 14),
                Location  = new Point(0, y),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlContent.Controls.Add(lblTrainingTitle);
            y += 18;

            lblTrainingValue = new Label
            {
                Text      = "$0 / msc",
                ForeColor = ThemeManager.GoldColor,
                Font      = ThemeManager.BoldDataFont,
                AutoSize  = false,
                Size      = new Size(260, 18),
                Location  = new Point(0, y)
            };
            pnlContent.Controls.Add(lblTrainingValue);
            y += 22;

            tbTrainingBudget = new TrackBar
            {
                Location    = new Point(0, y),
                Width       = 260,
                Minimum     = 0,
                Maximum     = 1000000,
                TickFrequency = 100000,
                SmallChange = 10000,
                LargeChange = 100000,
                BackColor   = ThemeManager.BackgroundColor
            };
            tbTrainingBudget.Scroll += (s, e) =>
            {
                if (_building == null) return;
                _building.TrainingBudget = tbTrainingBudget.Value;
                lblTrainingValue.Text    = $"${_building.TrainingBudget:N0} / msc";
            };
            pnlContent.Controls.Add(tbTrainingBudget);
            y += 50;

            y += 4;
            pnlContent.Controls.Add(MakeSeparator(ref y));

            // Sekcja: Akcje
            pnlContent.Controls.Add(MakeSectionHeader("AKCJE", ref y));

            Button btnUpgrade = new Button
            {
                Text     = "Ulepsz Budynek",
                Size     = new Size(260, 32),
                Location = new Point(0, y)
            };
            ThemeManager.ApplyButtonTheme(btnUpgrade);
            btnUpgrade.BackColor   = ThemeManager.HeaderBackground;
            pnlContent.Controls.Add(btnUpgrade);
            y += 38;

            Button btnDemolish = new Button
            {
                Text     = "Wyburz",
                Size     = new Size(120, 28),
                Location = new Point(0, y)
            };
            ThemeManager.ApplySecondaryButtonTheme(btnDemolish);
            btnDemolish.ForeColor = ThemeManager.NegativeColor;
            pnlContent.Controls.Add(btnDemolish);

            // ── Sekcja R&D (domyślnie ukryta) ───────────────────────────────────
            _pnlRnd = new Panel
            {
                Location  = new Point(0, y + 38),
                Size      = new Size(260, 160),
                BackColor = Color.Transparent,
                Visible   = false
            };

            _pnlRnd.Controls.Add(new Panel { Size = new Size(260, 1), Location = new Point(0, 0), BackColor = ThemeManager.SeparatorColor });

            _pnlRnd.Controls.Add(new Label
            {
                Text = "BADANIA I ROZWÓJ",
                ForeColor = ThemeManager.GoldColor,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(260, 16),
                Location = new Point(0, 10)
            });

            _pnlRnd.Controls.Add(new Label
            {
                Text = "Projekt:",
                ForeColor = ThemeManager.MutedTextColor,
                Font = ThemeManager.SmallFont,
                AutoSize = false,
                Size = new Size(60, 22),
                Location = new Point(0, 34),
                TextAlign = ContentAlignment.MiddleLeft
            });

            _cmbRndProject = new ComboBox
            {
                Location = new Point(62, 32),
                Width = 198,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                Font = ThemeManager.DefaultFont
            };
            _cmbRndProject.Items.AddRange(new object[] { "Brak", "Mleko", "Mięso", "Ser", "Węgiel", "Ruda Miedzi", "Miedź" });
            _cmbRndProject.SelectedIndex = 0;
            _cmbRndProject.SelectedIndexChanged += (s, e) => OnRndProjectChanged();
            _pnlRnd.Controls.Add(_cmbRndProject);

            _lblRndTech = new Label
            {
                Text = "",
                ForeColor = ThemeManager.AccentColor,
                Font = ThemeManager.BoldDataFont,
                AutoSize = false,
                Size = new Size(260, 18),
                Location = new Point(0, 66)
            };
            _pnlRnd.Controls.Add(_lblRndTech);

            _pnlRnd.Controls.Add(new Label
            {
                Text = "Postęp badań:",
                ForeColor = ThemeManager.MutedTextColor,
                Font = ThemeManager.SmallFont,
                AutoSize = false,
                Size = new Size(260, 14),
                Location = new Point(0, 90)
            });

            Panel pnlRndProgressBg = new Panel
            {
                Location = new Point(0, 108),
                Size = new Size(260, 18),
                BackColor = Color.FromArgb(20, 36, 56)
            };
            _pnlRndProgressFg = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 18),
                BackColor = Color.FromArgb(200, 100, 250)
            };
            pnlRndProgressBg.Controls.Add(_pnlRndProgressFg);
            _pnlRnd.Controls.Add(pnlRndProgressBg);

            _lblRndStatus = new Label
            {
                Text = "Status: Bezczynny",
                ForeColor = ThemeManager.MutedTextColor,
                Font = ThemeManager.SmallFont,
                AutoSize = false,
                Size = new Size(260, 16),
                Location = new Point(0, 132)
            };
            _pnlRnd.Controls.Add(_lblRndStatus);

            pnlContent.Controls.Add(_pnlRnd);

            // ── Sekcja Magazyn / Sprzedaż (dla budynków produkcyjnych) ──────────
            _pnlInventory = new Panel
            {
                Location  = new Point(0, y + 38),
                Size      = new Size(260, 230),
                BackColor = Color.Transparent,
                Visible   = false
            };

            _pnlInventory.Controls.Add(new Panel { Size = new Size(260, 1), Location = new Point(0, 0), BackColor = ThemeManager.SeparatorColor });

            _pnlInventory.Controls.Add(new Label
            {
                Text      = "MAGAZYN",
                ForeColor = ThemeManager.GoldColor,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(160, 16),
                Location  = new Point(0, 10)
            });

            _chkAutoSell = new CheckBox
            {
                Text      = "Auto-sprzedaż",
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(160, 20),
                Location  = new Point(0, 30),
                FlatStyle = FlatStyle.Flat
            };
            _chkAutoSell.CheckedChanged += (s, e) =>
            {
                if (_suppressAutoSellEvent || _building == null) return;
                _building.AutoSell = _chkAutoSell.Checked;
            };
            _pnlInventory.Controls.Add(_chkAutoSell);

            _invList = new Panel
            {
                Location  = new Point(0, 56),
                Size      = new Size(260, 168),
                BackColor = Color.FromArgb(10, 20, 34),
                AutoScroll = true
            };
            _pnlInventory.Controls.Add(_invList);

            _lblInvEmpty = new Label
            {
                Text      = "Magazyn pusty",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(240, 20),
                Location  = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _invList.Controls.Add(_lblInvEmpty);

            pnlContent.Controls.Add(_pnlInventory);

            this.Controls.Add(pnlContent);

            ThemeManager.MakeDraggable(pnlHeader, this);
            ThemeManager.MakeDraggable(lblTitle, this);
        }

        /// <summary>Przekazuje GameManager (potrzebny do dnia/godziny przy sprzedaży).</summary>
        public void SetGameManager(GameManager gm) => _gameManager = gm;

        private Label MakeSectionHeader(string text, ref int y)
        {
            var lbl = new Label
            {
                Text      = text,
                ForeColor = ThemeManager.GoldColor,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(260, 16),
                Location  = new Point(0, y),
                TextAlign = ContentAlignment.BottomLeft
            };
            y += 20;
            return lbl;
        }

        private Panel MakeSeparator(ref int y)
        {
            var sep = new Panel
            {
                Size      = new Size(260, 1),
                Location  = new Point(0, y),
                BackColor = ThemeManager.SeparatorColor
            };
            y += 8;
            return sep;
        }

        private Panel MakeRow(string label, ref int y, out Label valueLabel)
        {
            var row = new Panel
            {
                Size      = new Size(260, 18),
                Location  = new Point(0, y),
                BackColor = Color.Transparent
            };

            var lblKey = new Label
            {
                Text      = label,
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(110, 18),
                Location  = Point.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            var lblVal = new Label
            {
                Text      = "—",
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.DataFont,
                AutoSize  = false,
                Size      = new Size(148, 18),
                Location  = new Point(112, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            row.Controls.Add(lblKey);
            row.Controls.Add(lblVal);
            valueLabel = lblVal;
            y += 22;
            return row;
        }

        /// <summary>Przekazuje aktywną firmę (potrzebną do odczytu poziomu technologii w R&D).</summary>
        public void SetCompany(Company company) => _company = company;

        public void SetBuilding(Building building)
        {
            _building = building;

            bool isRnd = building is RNDCenter;
            _pnlRnd.Visible = isRnd;
            _pnlInventory.Visible = !isRnd;
            this.Height = isRnd ? RndHeight : InventoryHeight;

            if (isRnd)
            {
                var rnd = (RNDCenter)building;
                _suppressRndEvent = true;
                string active = rnd.ActiveResearchProject;
                int idx = string.IsNullOrEmpty(active) ? 0 : _cmbRndProject.Items.IndexOf(active);
                _cmbRndProject.SelectedIndex = idx >= 0 ? idx : 0;
                _suppressRndEvent = false;
            }
            else
            {
                _suppressAutoSellEvent = true;
                _chkAutoSell.Checked = building.AutoSell;
                _suppressAutoSellEvent = false;

                // Wymuś pełną przebudowę listy przy zmianie budynku
                foreach (var r in _invRows.Values) _invList.Controls.Remove(r.Row);
                _invRows.Clear();
            }

            RefreshData();
        }

        private void OnRndProjectChanged()
        {
            if (_suppressRndEvent || _building is not RNDCenter rnd) return;

            if (_cmbRndProject.SelectedIndex > 0)
                rnd.SetResearchProject(_cmbRndProject.SelectedItem!.ToString(), 24 * 30); // 30 dni gry
            else
                rnd.SetResearchProject(null!, 0);

            RefreshData();
        }

        public void RefreshData()
        {
            if (_building == null) return;

            lblTitle.Text          = _building.Name ?? "Nieznany Budynek";
            lblTypeValue.Text      = _building.GetType().Name;
            lblLocationValue.Text  = $"X:{_building.X}  Y:{_building.Y}";
            lblWorkerExpValue.Text = $"{(_building.WorkerExperience * 100f):F0}%";
            lblWorkerExpValue.ForeColor = _building.WorkerExperience > 0.7f
                ? ThemeManager.PositiveColor
                : _building.WorkerExperience > 0.4f
                    ? ThemeManager.GoldColor
                    : ThemeManager.NegativeColor;

            if (tbTrainingBudget.Value != (int)_building.TrainingBudget)
            {
                tbTrainingBudget.Value = Math.Clamp((int)_building.TrainingBudget, 0, 1000000);
                lblTrainingValue.Text  = $"${_building.TrainingBudget:N0} / msc";
            }

            if (_building is RNDCenter rnd && _pnlRnd.Visible)
            {
                string proj = rnd.ActiveResearchProject;
                if (string.IsNullOrEmpty(proj))
                {
                    _lblRndTech.Text   = "";
                    _lblRndStatus.Text = "Status: Bezczynny";
                    _lblRndStatus.ForeColor = ThemeManager.MutedTextColor;
                    _pnlRndProgressFg.Width = 0;
                }
                else
                {
                    float tech = _company != null && _company.TechLevels.ContainsKey(proj) ? _company.TechLevels[proj] : 0f;
                    _lblRndTech.Text   = $"Poziom technologii „{proj}”: {tech:F0}";
                    _lblRndStatus.Text = $"Status: Badania w toku ({rnd.ProgressNormalized * 100f:F0}%)";
                    _lblRndStatus.ForeColor = Color.FromArgb(200, 100, 250);
                    _pnlRndProgressFg.Width = (int)(rnd.ProgressNormalized * _pnlRndProgressFg.Parent.Width);
                }
            }

            if (_pnlInventory.Visible)
                RefreshInventory();
        }

        // ── Magazyn / sprzedaż ──────────────────────────────────────────────────

        private void RefreshInventory()
        {
            if (_building == null) return;

            // Aktualne produkty w magazynie (ilość > 0)
            var current = _building.Warehouse
                .Where(kvp => kvp.Value.Quantity > 0m)
                .Select(kvp => kvp.Key)
                .ToList();

            _lblInvEmpty.Visible = current.Count == 0;

            // Usuń wiersze produktów, których już nie ma
            foreach (var key in _invRows.Keys.ToList())
            {
                if (!current.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    _invList.Controls.Remove(_invRows[key].Row);
                    _invRows[key].Row.Dispose();
                    _invRows.Remove(key);
                }
            }

            // Dodaj/uaktualnij wiersze
            int rowY = 4;
            foreach (var res in current.OrderBy(r => r))
            {
                if (!_invRows.TryGetValue(res, out var row))
                {
                    row = BuildInventoryRow(res);
                    _invRows[res] = row;
                    _invList.Controls.Add(row.Row);
                }

                row.Row.Location = new Point(0, rowY);
                rowY += 28;

                decimal qty = _building.GetProductQuantity(res);
                row.Qty.Text = $"{qty:N0}";

                bool hasPrice = _building.ResourcePrices.TryGetValue(res, out decimal price) && price > 0m;
                row.Price.Text = hasPrice ? $"{price:N0} zł/szt" : "brak ceny";
                row.Sell.Enabled = hasPrice;
                row.Sell.ForeColor = hasPrice ? ThemeManager.PositiveColor : ThemeManager.MutedTextColor;
            }
        }

        private InvRow BuildInventoryRow(string resource)
        {
            var panel = new Panel
            {
                Size      = new Size(238, 26),
                BackColor = Color.Transparent
            };

            var lblName = new Label
            {
                Text      = resource,
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(96, 24),
                Location  = new Point(2, 1),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblName);

            var lblQty = new Label
            {
                Text      = "0",
                ForeColor = ThemeManager.GoldColor,
                Font      = ThemeManager.BoldDataFont,
                AutoSize  = false,
                Size      = new Size(40, 24),
                Location  = new Point(96, 1),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(lblQty);

            var lblPrice = new Label
            {
                Text      = "",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = new Font("Segoe UI", 6.5f, FontStyle.Regular),
                AutoSize  = false,
                Size      = new Size(78, 12),
                Location  = new Point(140, 14),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(lblPrice);

            var btnSell = new Button
            {
                Text      = "Sprzedaj",
                Size      = new Size(78, 14),
                Location  = new Point(140, 1),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.HeaderBackground,
                ForeColor = ThemeManager.PositiveColor,
                Font      = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnSell.FlatAppearance.BorderColor = ThemeManager.BorderColor;
            btnSell.Click += (s, e) => SellAll(resource);
            panel.Controls.Add(btnSell);

            return new InvRow { Row = panel, Qty = lblQty, Price = lblPrice, Sell = btnSell };
        }

        private void SellAll(string resource)
        {
            if (_building == null || _company == null) return;

            decimal qty = _building.GetProductQuantity(resource);
            if (qty <= 0m) return;

            int day  = _gameManager?.CurrentDay  ?? 1;
            int hour = _gameManager?.CurrentHour ?? 8;

            if (_building.SellResource(resource, qty, _company, day, hour))
            {
                RefreshInventory();
                OnInventoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
