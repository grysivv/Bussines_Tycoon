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

        /// <summary>Wywoływane po ręcznej sprzedaży / zmianie stanu — pozwala odświeżyć HUD (saldo).</summary>
        public event EventHandler? OnInventoryChanged;

        // Szerokość pojedynczej sekcji wewnątrz przewijanej kolumny.
        private const int SectionWidth = 292;

        // ── Nagłówek ──
        private Label lblTitle = null!;
        private Panel pnlStatusDot = null!;

        // ── Kolumna sekcji (przewijana) ──
        private FlowLayoutPanel _flow = null!;

        // ── Informacje ──
        private Label _lblTypeValue = null!;
        private Label _lblLocationValue = null!;
        private Label _lblLevelValue = null!;
        private Panel _rowLevel = null!;

        // ── Zatrudnienie (tylko CoalMine) ──
        private Panel _secEmployees = null!;
        private Label _lblEmpMax = null!;
        private NumericUpDown _numEmployees = null!;
        private Label _lblEmpProd = null!;
        private Label _lblEmpCost = null!;
        private Label _lblEmpStatus = null!;
        private Button _btnUpgradeMine = null!;
        private bool _suppressEmpEvent;

        // ── Personel / szkolenia ──
        private Label _lblWorkerExpValue = null!;
        private Label _lblTrainingValue = null!;
        private TrackBar _tbTrainingBudget = null!;

        // ── Magazyn / sprzedaż ──
        private Panel _secInventory = null!;
        private Panel _invList = null!;
        private CheckBox _chkAutoSell = null!;
        private Label _lblInvEmpty = null!;
        private bool _suppressAutoSellEvent;
        private readonly Dictionary<string, InvRow> _invRows = new(StringComparer.OrdinalIgnoreCase);

        // ── R&D (tylko RNDCenter) ──
        private Panel _secRnd = null!;
        private ComboBox _cmbRndProject = null!;
        private Label _lblRndTech = null!;
        private Label _lblRndStatus = null!;
        private Panel _pnlRndProgressFg = null!;
        private bool _suppressRndEvent;

        private const int UpgradeCost = 50000;

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
            this.Size           = new Size(340, 600);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Przewijana kolumna sekcji ───────────────────────────────────────
            _flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = ThemeManager.BackgroundColor,
                Padding       = new Padding(14, 10, 10, 12)
            };

            _flow.Controls.Add(BuildInfoSection());
            _flow.Controls.Add(BuildEmployeesSection());
            _flow.Controls.Add(BuildTrainingSection());
            _flow.Controls.Add(BuildInventorySection());
            _flow.Controls.Add(BuildRndSection());
            _flow.Controls.Add(BuildActionsSection());

            // Fill musi być z tyłu z-order (dodane jako pierwsze), a Top-header na wierzchu —
            // dlatego najpierw dodajemy kolumnę, a dopiero potem nagłówek.
            this.Controls.Add(_flow);
            BuildHeader();
        }

        private void BuildHeader()
        {
            Panel pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = ThemeManager.HeaderBackground
            };
            pnlHeader.Paint += (s, e) =>
            {
                ThemeManager.DrawWindowHeader(e.Graphics, ((Panel)s).ClientRectangle, "", ThemeManager.HeaderFont);
                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, ((Panel)s).Width, 0);
            };

            pnlStatusDot = new Panel
            {
                Size      = new Size(10, 10),
                Location  = new Point(12, 8),
                BackColor = ThemeManager.PositiveColor
            };
            pnlStatusDot.Paint += (s, e) =>
            {
                var p = (Panel)s;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(p.BackColor);
                e.Graphics.FillEllipse(brush, 0, 0, p.Width - 1, p.Height - 1);
            };
            pnlHeader.Controls.Add(pnlStatusDot);

            lblTitle = new Label
            {
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                Location  = new Point(28, 13),
                AutoSize  = true,
                Text      = "—"
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(28, 28),
                Location = new Point(this.Width - 36, 10),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.ToolTipText("Zamknij");
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.Click    += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            ThemeManager.MakeDraggable(pnlHeader, this);
            ThemeManager.MakeDraggable(lblTitle, this);
        }

        // ───────────────────────────────────────────────────────────────────────
        //  Budowniczowie sekcji
        // ───────────────────────────────────────────────────────────────────────

        private Panel NewSection(int height)
        {
            return new Panel
            {
                Size      = new Size(SectionWidth, height),
                Margin    = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
        }

        private Label SectionHeader(string text)
        {
            return new Label
            {
                Text      = text,
                ForeColor = ThemeManager.GoldColor,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(SectionWidth, 16),
                Location  = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Panel SeparatorLine(int y)
        {
            return new Panel
            {
                Size      = new Size(SectionWidth, 1),
                Location  = new Point(0, y),
                BackColor = ThemeManager.SeparatorColor
            };
        }

        private Panel KeyValueRow(string key, int y, out Label valueLabel)
        {
            var row = new Panel { Size = new Size(SectionWidth, 20), Location = new Point(0, y), BackColor = Color.Transparent };
            row.Controls.Add(new Label
            {
                Text = key, ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(120, 20), Location = Point.Empty,
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent
            });
            valueLabel = new Label
            {
                Text = "—", ForeColor = ThemeManager.TextColor, Font = ThemeManager.DataFont,
                AutoSize = false, Size = new Size(SectionWidth - 122, 20), Location = new Point(122, 0),
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent
            };
            row.Controls.Add(valueLabel);
            return row;
        }

        private Panel BuildInfoSection()
        {
            var sec = NewSection(86);
            sec.Controls.Add(SectionHeader("INFORMACJE"));
            sec.Controls.Add(KeyValueRow("Typ:", 20, out _lblTypeValue));
            sec.Controls.Add(KeyValueRow("Lokalizacja:", 42, out _lblLocationValue));
            _rowLevel = KeyValueRow("Poziom:", 64, out _lblLevelValue);
            sec.Controls.Add(_rowLevel);
            return sec;
        }

        private Panel BuildEmployeesSection()
        {
            _secEmployees = NewSection(206);
            _secEmployees.Controls.Add(SeparatorLine(0));
            var head = SectionHeader("ZATRUDNIENIE");
            head.Location = new Point(0, 8);
            _secEmployees.Controls.Add(head);

            _lblEmpMax = new Label
            {
                Text = "Limit: —", ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 16), Location = new Point(0, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _secEmployees.Controls.Add(_lblEmpMax);

            _secEmployees.Controls.Add(new Label
            {
                Text = "Liczba pracowników:", ForeColor = ThemeManager.TextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(160, 24), Location = new Point(0, 48),
                TextAlign = ContentAlignment.MiddleLeft
            });

            _numEmployees = new NumericUpDown
            {
                Location  = new Point(160, 48),
                Width     = 132,
                Minimum   = 0,
                Maximum   = 100000,
                Increment = 50,
                Font      = ThemeManager.BoldDataFont,
                BackColor = ThemeManager.PanelBackground,
                ForeColor = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            _numEmployees.ValueChanged += (s, e) =>
            {
                if (_suppressEmpEvent || _building is not CoalMine mine) return;
                mine.CurrentEmployees = (int)_numEmployees.Value;
                // Setter mógł obciąć wartość do limitu — zsynchronizuj kontrolkę.
                if ((int)_numEmployees.Value != mine.CurrentEmployees)
                {
                    _suppressEmpEvent = true;
                    _numEmployees.Value = mine.CurrentEmployees;
                    _suppressEmpEvent = false;
                }
                UpdateEmployeeLive(mine);
            };
            _secEmployees.Controls.Add(_numEmployees);

            var btnZero = new Button { Text = "0", Size = new Size(58, 26), Location = new Point(0, 80) };
            ThemeManager.ApplySecondaryButtonTheme(btnZero);
            btnZero.Click += (s, e) => { if (_building is CoalMine) _numEmployees.Value = 0; };
            _secEmployees.Controls.Add(btnZero);

            var btnHalf = new Button { Text = "½", Size = new Size(58, 26), Location = new Point(64, 80) };
            ThemeManager.ApplySecondaryButtonTheme(btnHalf);
            btnHalf.Click += (s, e) => { if (_building is CoalMine m) _numEmployees.Value = Math.Min(_numEmployees.Maximum, m.MaxEmployees / 2); };
            _secEmployees.Controls.Add(btnHalf);

            var btnMax = new Button { Text = "Max", Size = new Size(64, 26), Location = new Point(128, 80) };
            ThemeManager.ApplySecondaryButtonTheme(btnMax);
            btnMax.ForeColor = ThemeManager.GoldColor;
            btnMax.Click += (s, e) => { if (_building is CoalMine m) _numEmployees.Value = Math.Min(_numEmployees.Maximum, m.MaxEmployees); };
            _secEmployees.Controls.Add(btnMax);

            _lblEmpProd = new Label
            {
                Text = "Produkcja: —", ForeColor = ThemeManager.PositiveColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 16), Location = new Point(0, 112)
            };
            _secEmployees.Controls.Add(_lblEmpProd);

            _lblEmpCost = new Label
            {
                Text = "Koszt pracy: —", ForeColor = ThemeManager.NegativeColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 16), Location = new Point(0, 130)
            };
            _secEmployees.Controls.Add(_lblEmpCost);

            _btnUpgradeMine = new Button
            {
                Text     = $"Ulepsz do Poziomu 2  (${UpgradeCost:N0})",
                Size     = new Size(SectionWidth, 30),
                Location = new Point(0, 152)
            };
            ThemeManager.ApplyButtonTheme(_btnUpgradeMine);
            _btnUpgradeMine.BackColor = ThemeManager.HeaderBackground;
            _btnUpgradeMine.Click += (s, e) => UpgradeMine();
            _secEmployees.Controls.Add(_btnUpgradeMine);

            _lblEmpStatus = new Label
            {
                Text = "", ForeColor = ThemeManager.NegativeColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 16), Location = new Point(0, 186)
            };
            _secEmployees.Controls.Add(_lblEmpStatus);

            return _secEmployees;
        }

        private Panel BuildTrainingSection()
        {
            var sec = NewSection(126);
            sec.Controls.Add(SeparatorLine(0));
            var head = SectionHeader("PERSONEL");
            head.Location = new Point(0, 8);
            sec.Controls.Add(head);

            sec.Controls.Add(KeyValueRow("Doświadczenie:", 28, out _lblWorkerExpValue));

            sec.Controls.Add(new Label
            {
                Text = "BUDŻET SZKOLENIOWY", ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 14), Location = new Point(0, 52),
                TextAlign = ContentAlignment.MiddleLeft
            });

            _lblTrainingValue = new Label
            {
                Text = "$0 / msc", ForeColor = ThemeManager.GoldColor, Font = ThemeManager.BoldDataFont,
                AutoSize = false, Size = new Size(SectionWidth, 18), Location = new Point(0, 68)
            };
            sec.Controls.Add(_lblTrainingValue);

            _tbTrainingBudget = new TrackBar
            {
                Location      = new Point(-2, 88),
                Width         = SectionWidth,
                Minimum       = 0,
                Maximum       = 1000000,
                TickFrequency = 100000,
                SmallChange   = 10000,
                LargeChange   = 100000,
                BackColor     = ThemeManager.BackgroundColor
            };
            _tbTrainingBudget.Scroll += (s, e) =>
            {
                if (_building == null) return;
                _building.TrainingBudget = _tbTrainingBudget.Value;
                _lblTrainingValue.Text   = $"${_building.TrainingBudget:N0} / msc";
            };
            sec.Controls.Add(_tbTrainingBudget);

            return sec;
        }

        private Panel BuildInventorySection()
        {
            _secInventory = NewSection(244);
            _secInventory.Controls.Add(SeparatorLine(0));
            var head = SectionHeader("MAGAZYN / SPRZEDAŻ");
            head.Location = new Point(0, 8);
            _secInventory.Controls.Add(head);

            _chkAutoSell = new CheckBox
            {
                Text      = "Auto-sprzedaż produkcji",
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.DefaultFont,
                AutoSize  = false,
                Size      = new Size(SectionWidth, 24),
                Location  = new Point(0, 30),
                FlatStyle = FlatStyle.Flat
            };
            _chkAutoSell.CheckedChanged += (s, e) =>
            {
                if (_suppressAutoSellEvent || _building == null) return;
                _building.AutoSell = _chkAutoSell.Checked;
                OnInventoryChanged?.Invoke(this, EventArgs.Empty);
            };
            _secInventory.Controls.Add(_chkAutoSell);

            _invList = new Panel
            {
                Location   = new Point(0, 58),
                Size       = new Size(SectionWidth, 184),
                BackColor  = Color.FromArgb(10, 20, 34),
                AutoScroll = true
            };
            _secInventory.Controls.Add(_invList);

            _lblInvEmpty = new Label
            {
                Text      = "Magazyn pusty",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(SectionWidth - 16, 20),
                Location  = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _invList.Controls.Add(_lblInvEmpty);

            return _secInventory;
        }

        private Panel BuildRndSection()
        {
            _secRnd = NewSection(168);
            _secRnd.Controls.Add(SeparatorLine(0));
            var head = SectionHeader("BADANIA I ROZWÓJ");
            head.Location = new Point(0, 8);
            _secRnd.Controls.Add(head);

            _secRnd.Controls.Add(new Label
            {
                Text = "Projekt:", ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(56, 24), Location = new Point(0, 32),
                TextAlign = ContentAlignment.MiddleLeft
            });

            _cmbRndProject = new ComboBox
            {
                Location      = new Point(58, 30),
                Width         = SectionWidth - 58,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = ThemeManager.PanelBackground,
                ForeColor     = ThemeManager.TextColor,
                Font          = ThemeManager.DefaultFont
            };
            _cmbRndProject.Items.AddRange(new object[] { "Brak", "Mleko", "Mięso", "Ser", "Węgiel", "Ruda Miedzi", "Miedź" });
            _cmbRndProject.SelectedIndex = 0;
            _cmbRndProject.SelectedIndexChanged += (s, e) => OnRndProjectChanged();
            _secRnd.Controls.Add(_cmbRndProject);

            _lblRndTech = new Label
            {
                Text = "", ForeColor = ThemeManager.AccentColor, Font = ThemeManager.BoldDataFont,
                AutoSize = false, Size = new Size(SectionWidth, 18), Location = new Point(0, 64)
            };
            _secRnd.Controls.Add(_lblRndTech);

            _secRnd.Controls.Add(new Label
            {
                Text = "Postęp badań:", ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 14), Location = new Point(0, 88)
            });

            Panel pnlRndProgressBg = new Panel
            {
                Location  = new Point(0, 106),
                Size      = new Size(SectionWidth, 18),
                BackColor = Color.FromArgb(20, 36, 56)
            };
            _pnlRndProgressFg = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(0, 18),
                BackColor = Color.FromArgb(200, 100, 250)
            };
            pnlRndProgressBg.Controls.Add(_pnlRndProgressFg);
            _secRnd.Controls.Add(pnlRndProgressBg);

            _lblRndStatus = new Label
            {
                Text = "Status: Bezczynny", ForeColor = ThemeManager.MutedTextColor, Font = ThemeManager.SmallFont,
                AutoSize = false, Size = new Size(SectionWidth, 16), Location = new Point(0, 130)
            };
            _secRnd.Controls.Add(_lblRndStatus);

            return _secRnd;
        }

        private Panel BuildActionsSection()
        {
            var sec = NewSection(70);
            sec.Controls.Add(SeparatorLine(0));
            var head = SectionHeader("AKCJE");
            head.Location = new Point(0, 8);
            sec.Controls.Add(head);

            Button btnDemolish = new Button
            {
                Text     = "Wyburz",
                Size     = new Size(130, 28),
                Location = new Point(0, 30),
                Enabled  = false
            };
            ThemeManager.ApplySecondaryButtonTheme(btnDemolish);
            btnDemolish.ForeColor = ThemeManager.MutedTextColor;
            var tip = new ToolTip();
            tip.SetToolTip(btnDemolish, "Wyburzanie budynków — wkrótce.");
            sec.Controls.Add(btnDemolish);

            return sec;
        }

        // ───────────────────────────────────────────────────────────────────────

        public void SetGameManager(GameManager gm) => _gameManager = gm;
        public void SetCompany(Company company) => _company = company;

        public void SetBuilding(Building building)
        {
            _building = building;

            bool isRnd  = building is RNDCenter;
            bool isMine = building is CoalMine;

            _secRnd.Visible       = isRnd;
            _secInventory.Visible = !isRnd;
            _secEmployees.Visible = isMine;
            _rowLevel.Visible     = isMine;

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

                foreach (var r in _invRows.Values) { _invList.Controls.Remove(r.Row); r.Row.Dispose(); }
                _invRows.Clear();
            }

            if (isMine)
            {
                var mine = (CoalMine)building;
                _suppressEmpEvent = true;
                _numEmployees.Maximum = mine.MaxEmployees;
                _numEmployees.Value   = Math.Clamp(mine.CurrentEmployees, 0, mine.MaxEmployees);
                _suppressEmpEvent = false;
            }

            RefreshData();
        }

        private void UpdateEmployeeLive(CoalMine mine)
        {
            // Produkcja godzinowa = pracownicy * 0.085 * mnożnik poziomu * mnożnik technologii
            double perHour = mine.CurrentEmployees * 0.085 * mine.LevelMultiplier * mine.TechnologyMultiplier;
            decimal laborCost = mine.CurrentEmployees * 35.25m;

            _lblEmpMax.Text   = $"Limit (Poziom {mine.Level}): {mine.MaxEmployees:N0}";
            _lblEmpProd.Text  = $"Produkcja: {perHour:N1} t/h";
            _lblEmpCost.Text  = $"Koszt pracy: ${laborCost:N0} / h";

            bool storageFull = mine.PreciseStorage >= mine.MaxStorageCapacity;
            _lblEmpStatus.Text    = storageFull ? "⚠ Magazyn pełny — produkcja wstrzymana" : "";
            _lblEmpStatus.Visible = storageFull;
            pnlStatusDot.BackColor = storageFull ? ThemeManager.NegativeColor : ThemeManager.PositiveColor;

            bool canUpgrade = mine.Level < 2;
            _btnUpgradeMine.Visible = canUpgrade;
            _btnUpgradeMine.Enabled = canUpgrade && (_company?.Balance ?? 0m) >= UpgradeCost;
        }

        private void UpgradeMine()
        {
            if (_building is not CoalMine mine || _company == null) return;
            if (mine.Level >= 2) return;
            if (_company.Balance < UpgradeCost)
            {
                MessageBox.Show("Brak środków na ulepszenie kopalni.", "Ulepszenie",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int day  = _gameManager?.CurrentDay  ?? 1;
            int hour  = _gameManager?.CurrentHour ?? 8;

            _company.Balance -= UpgradeCost;
            _company.AddTransaction(day, hour, $"Ulepszenie: {mine.Name} (Poziom 2)",
                -UpgradeCost, "Budowa", mine.FacilityId);
            mine.Level = 2;

            _suppressEmpEvent = true;
            _numEmployees.Maximum = mine.MaxEmployees;
            _suppressEmpEvent = false;

            RefreshData();
            OnInventoryChanged?.Invoke(this, EventArgs.Empty);
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

            lblTitle.Text         = _building.Name ?? "Nieznany Budynek";
            _lblTypeValue.Text    = _building.ActivityType;
            _lblLocationValue.Text = $"X:{_building.X}  Y:{_building.Y}";

            _lblWorkerExpValue.Text = $"{(_building.WorkerExperience * 100f):F0}%";
            _lblWorkerExpValue.ForeColor = _building.WorkerExperience > 0.7f
                ? ThemeManager.PositiveColor
                : _building.WorkerExperience > 0.4f
                    ? ThemeManager.GoldColor
                    : ThemeManager.NegativeColor;

            if (_tbTrainingBudget.Value != (int)_building.TrainingBudget)
            {
                _tbTrainingBudget.Value = Math.Clamp((int)_building.TrainingBudget, 0, 1000000);
                _lblTrainingValue.Text  = $"${_building.TrainingBudget:N0} / msc";
            }

            if (_building is CoalMine mine && _secEmployees.Visible)
            {
                _lblLevelValue.Text = $"{mine.Level}";
                UpdateEmployeeLive(mine);
            }

            if (_building is RNDCenter rnd && _secRnd.Visible)
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

            if (_secInventory.Visible)
                RefreshInventory();
        }

        // ── Magazyn / sprzedaż ──────────────────────────────────────────────────

        private void RefreshInventory()
        {
            if (_building == null) return;

            var current = _building.Warehouse
                .Where(kvp => kvp.Value.Quantity > 0m)
                .Select(kvp => kvp.Key)
                .ToList();

            _lblInvEmpty.Visible = current.Count == 0;

            foreach (var key in _invRows.Keys.ToList())
            {
                if (!current.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    _invList.Controls.Remove(_invRows[key].Row);
                    _invRows[key].Row.Dispose();
                    _invRows.Remove(key);
                }
            }

            int rowY = 4;
            foreach (var res in current.OrderBy(r => r))
            {
                if (!_invRows.TryGetValue(res, out var row))
                {
                    row = BuildInventoryRow(res);
                    _invRows[res] = row;
                    _invList.Controls.Add(row.Row);
                }

                row.Row.Location = new Point(4, rowY);
                rowY += 40;

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
                Size      = new Size(SectionWidth - 26, 36),
                BackColor = Color.FromArgb(18, 32, 50)
            };

            var lblName = new Label
            {
                Text      = resource,
                ForeColor = ThemeManager.TextColor,
                Font      = ThemeManager.DefaultFont,
                AutoSize  = false,
                Size      = new Size(120, 18),
                Location  = new Point(6, 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblName);

            var lblPrice = new Label
            {
                Text      = "",
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(120, 14),
                Location  = new Point(6, 19),
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblPrice);

            var lblQty = new Label
            {
                Text      = "0",
                ForeColor = ThemeManager.GoldColor,
                Font      = ThemeManager.BoldDataFont,
                AutoSize  = false,
                Size      = new Size(60, 32),
                Location  = new Point(126, 2),
                TextAlign = ContentAlignment.MiddleRight
            };
            panel.Controls.Add(lblQty);

            var btnSell = new Button
            {
                Text      = "Sprzedaj",
                Size      = new Size(72, 28),
                Location  = new Point(panel.Width - 76, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.HeaderBackground,
                ForeColor = ThemeManager.PositiveColor,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
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
