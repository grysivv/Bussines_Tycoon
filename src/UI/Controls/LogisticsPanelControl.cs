using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Logistics;
using Conglomerate.UI;

namespace Conglomerate.UI.Controls
{
    /// <summary>
    /// Panel zarządzania logistyką i flotą — Modern UI.
    /// Pozwala tworzyć, wstrzymywać i usuwać automatyczne trasy dostaw (SupplyRoute),
    /// które obsługuje LogisticsManager w pętli gry.
    /// </summary>
    public class LogisticsPanelControl : UserControl
    {
        private GameManager? _gm;

        private Label _lblFleet = null!;
        private Panel _pnlRoutesList = null!;

        // Formularz tworzenia trasy
        private ComboBox _cmbSourceType = null!;
        private ComboBox _cmbSourceBuilding = null!;
        private ComboBox _cmbTargetBuilding = null!;
        private ComboBox _cmbResource = null!;
        private ComboBox _cmbVehicle = null!;
        private ComboBox _cmbLoadRule = null!;
        private ComboBox _cmbPriority = null!;
        private NumericUpDown _numAmount = null!;
        private NumericUpDown _numInterval = null!;

        private System.Windows.Forms.Timer _refreshTimer = null!;

        /// <summary>Opakowanie budynku do umieszczenia w ComboBox z czytelną nazwą.</summary>
        private sealed class BuildingItem
        {
            public Building Building { get; }
            public BuildingItem(Building b) { Building = b; }
            public override string ToString() => $"{Building.Name}";
        }

        public LogisticsPanelControl()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────
        //  Budowa UI
        // ─────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            this.Size           = new Size(900, 620);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = ThemeManager.HeaderBackground };
            pnlHeader.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var brush = new LinearGradientBrush(p.ClientRectangle,
                    ThemeManager.HeaderBackground, Color.FromArgb(10, 22, 40), LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);
                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, p.Width, 0);
            };

            Label lblTitle = new Label
            {
                Text = "🚚 Logistyka i Flota Imperium",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.TextColor,
                Location = new Point(16, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 28),
                Location = new Point(this.Width - 44, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // ── Prawa kolumna: formularz tworzenia trasy ────────────────────────
            Panel pnlForm = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = ThemeManager.PanelBackground,
                Padding = new Padding(12)
            };
            pnlForm.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
            };
            this.Controls.Add(pnlForm);

            int y = 8;
            pnlForm.Controls.Add(MakeSectionLabel("NOWA TRASA DOSTAW", ref y));

            pnlForm.Controls.Add(MakeFieldLabel("Źródło", ref y));
            _cmbSourceType = MakeCombo(ref y);
            _cmbSourceType.Items.AddRange(new object[] { "🏪 Wolny rynek", "🏭 Budynek (własny)" });
            _cmbSourceType.SelectedIndex = 0;
            _cmbSourceType.SelectedIndexChanged += (s, e) =>
            {
                _cmbSourceBuilding.Enabled = _cmbSourceType.SelectedIndex == 1;
                RepopulateResources();
            };
            pnlForm.Controls.Add(_cmbSourceType);

            pnlForm.Controls.Add(MakeFieldLabel("Budynek źródłowy", ref y));
            _cmbSourceBuilding = MakeCombo(ref y);
            _cmbSourceBuilding.Enabled = false;
            _cmbSourceBuilding.SelectedIndexChanged += (s, e) => RepopulateResources();
            pnlForm.Controls.Add(_cmbSourceBuilding);

            pnlForm.Controls.Add(MakeFieldLabel("Budynek docelowy", ref y));
            _cmbTargetBuilding = MakeCombo(ref y);
            pnlForm.Controls.Add(_cmbTargetBuilding);

            pnlForm.Controls.Add(MakeFieldLabel("Surowiec", ref y));
            _cmbResource = MakeCombo(ref y);
            pnlForm.Controls.Add(_cmbResource);

            pnlForm.Controls.Add(MakeFieldLabel("Pojazd", ref y));
            _cmbVehicle = MakeCombo(ref y);
            foreach (var v in VehicleRegistry.VehicleTypes)
                _cmbVehicle.Items.Add(v.DisplayName);
            if (_cmbVehicle.Items.Count > 0) _cmbVehicle.SelectedIndex = 0;
            pnlForm.Controls.Add(_cmbVehicle);

            pnlForm.Controls.Add(MakeFieldLabel("Reguła załadunku", ref y));
            _cmbLoadRule = MakeCombo(ref y);
            _cmbLoadRule.Items.AddRange(new object[]
            {
                "Zegar (co X godzin)",
                "Min. 50% pojemności",
                "Tylko pełny załadunek"
            });
            _cmbLoadRule.SelectedIndex = 0;
            pnlForm.Controls.Add(_cmbLoadRule);

            pnlForm.Controls.Add(MakeFieldLabel("Priorytet", ref y));
            _cmbPriority = MakeCombo(ref y);
            _cmbPriority.Items.AddRange(new object[] { "Niski", "Średni", "Wysoki" });
            _cmbPriority.SelectedIndex = 1;
            pnlForm.Controls.Add(_cmbPriority);

            // Ilość + interwał obok siebie
            var lblAmount = MakeFieldLabel("Ilość / kurs", ref y);
            lblAmount.Width = 130;
            var lblInterval = new Label
            {
                Text = "Interwał (h)",
                Font = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location = new Point(162, lblAmount.Top),
                AutoSize = false,
                Size = new Size(130, 16)
            };
            pnlForm.Controls.Add(lblAmount);
            pnlForm.Controls.Add(lblInterval);

            _numAmount = new NumericUpDown
            {
                Location = new Point(12, y),
                Width = 130,
                Minimum = 1,
                Maximum = 100000,
                Value = 20,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeManager.DataFont
            };
            _numInterval = new NumericUpDown
            {
                Location = new Point(162, y),
                Width = 130,
                Minimum = 1,
                Maximum = 240,
                Value = 24,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeManager.DataFont
            };
            pnlForm.Controls.Add(_numAmount);
            pnlForm.Controls.Add(_numInterval);
            y += 34;

            Button btnAdd = new Button
            {
                Text = "➕  Utwórz trasę",
                Location = new Point(12, y),
                Size = new Size(280, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            ThemeManager.ApplyButtonTheme(btnAdd);
            btnAdd.BackColor = Color.FromArgb(30, 70, 45);
            btnAdd.ForeColor = ThemeManager.PositiveColor;
            btnAdd.FlatAppearance.BorderColor = ThemeManager.PositiveColor;
            btnAdd.Click += (s, e) => CreateRoute();
            pnlForm.Controls.Add(btnAdd);

            // ── Lewa kolumna: lista tras ────────────────────────────────────────
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.BackgroundColor, Padding = new Padding(12) };
            this.Controls.Add(pnlLeft);
            pnlLeft.BringToFront();

            _lblFleet = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = "Aktywne dostawy: 0 / 0",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _pnlRoutesList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(10, 18, 30),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlLeft.Controls.Add(_pnlRoutesList);
            pnlLeft.Controls.Add(_lblFleet);

            // Timer odświeżania statusów (tylko gdy widoczny)
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) => { if (this.Visible) RebuildRoutesList(); };
            _refreshTimer.Start();
        }

        // ─────────────────────────────────────────────────────────
        //  API publiczne
        // ─────────────────────────────────────────────────────────

        /// <summary>Podpina dane gry i przygotowuje formularz oraz listę tras.</summary>
        public void SetData(GameManager gm)
        {
            _gm = gm;
            PopulateBuildingCombos();
            RepopulateResources();
            RebuildRoutesList();
        }

        public void RefreshData() => RebuildRoutesList();

        // ─────────────────────────────────────────────────────────
        //  Wypełnianie list
        // ─────────────────────────────────────────────────────────

        private void PopulateBuildingCombos()
        {
            if (_gm == null) return;

            _cmbSourceBuilding.Items.Clear();
            _cmbTargetBuilding.Items.Clear();

            foreach (var b in _gm.ActiveCompany.Buildings)
            {
                _cmbSourceBuilding.Items.Add(new BuildingItem(b));
                _cmbTargetBuilding.Items.Add(new BuildingItem(b));
            }

            if (_cmbSourceBuilding.Items.Count > 0) _cmbSourceBuilding.SelectedIndex = 0;
            if (_cmbTargetBuilding.Items.Count > 0) _cmbTargetBuilding.SelectedIndex = 0;
        }

        private void RepopulateResources()
        {
            if (_gm == null) return;

            string previous = _cmbResource.SelectedItem as string ?? string.Empty;
            _cmbResource.Items.Clear();

            IEnumerable<string> names;
            if (_cmbSourceType.SelectedIndex == 1 && _cmbSourceBuilding.SelectedItem is BuildingItem item)
            {
                // Surowce, którymi handluje / które posiada budynek źródłowy
                names = item.Building.ResourcePrices.Keys
                    .Union(item.Building.Warehouse.Keys, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // Wszystkie surowce dostępne na wolnym rynku
                names = _gm.Market.Listings.Keys;
            }

            foreach (var n in names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n))
                _cmbResource.Items.Add(n);

            if (_cmbResource.Items.Count > 0)
            {
                int idx = _cmbResource.Items.IndexOf(previous);
                _cmbResource.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Tworzenie trasy
        // ─────────────────────────────────────────────────────────

        private void CreateRoute()
        {
            if (_gm == null) return;

            bool fromMarket = _cmbSourceType.SelectedIndex == 0;

            if (_cmbTargetBuilding.SelectedItem is not BuildingItem targetItem)
            {
                MessageBox.Show("Wybierz budynek docelowy.", "Brak celu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_cmbResource.SelectedItem is not string resource || string.IsNullOrWhiteSpace(resource))
            {
                MessageBox.Show("Wybierz surowiec do transportu.", "Brak surowca", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BuildingItem? sourceItem = _cmbSourceBuilding.SelectedItem as BuildingItem;
            if (!fromMarket && sourceItem == null)
            {
                MessageBox.Show("Wybierz budynek źródłowy.", "Brak źródła", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!fromMarket && sourceItem!.Building.FacilityId == targetItem.Building.FacilityId)
            {
                MessageBox.Show("Źródło i cel nie mogą być tym samym budynkiem.", "Błędna trasa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var route = new SupplyRoute
            {
                SourceType       = fromMarket ? RouteSourceType.Market : RouteSourceType.Building,
                SourceFacilityId = fromMarket ? string.Empty : sourceItem!.Building.FacilityId,
                TargetFacilityId = targetItem.Building.FacilityId,
                ResourceName     = resource,
                AmountPerTrip    = (int)_numAmount.Value,
                IntervalHours    = (int)_numInterval.Value,
                VehicleTypeName  = VehicleRegistry.VehicleTypes[Math.Max(0, _cmbVehicle.SelectedIndex)].Name,
                LoadRule         = (LoadThresholdRule)Math.Max(0, _cmbLoadRule.SelectedIndex),
                Priority         = (RoutePriority)Math.Max(0, _cmbPriority.SelectedIndex),
                IsEnabled        = true
            };

            _gm.Logistics.AddRoute(route);
            RebuildRoutesList();
        }

        // ─────────────────────────────────────────────────────────
        //  Lista tras
        // ─────────────────────────────────────────────────────────

        private void RebuildRoutesList()
        {
            if (_gm == null) return;

            _lblFleet.Text = $"Aktywne dostawy: {_gm.Logistics.ActiveTrips.Count} / {_gm.Logistics.TotalFleetSize}    •    Trasy: {_gm.Logistics.Routes.Count}";

            _pnlRoutesList.SuspendLayout();
            _pnlRoutesList.Controls.Clear();

            var buildingMap = _gm.ActiveCompany.Buildings.ToDictionary(b => b.FacilityId, b => b);
            var routes = _gm.Logistics.Routes;

            if (routes.Count == 0)
            {
                _pnlRoutesList.Controls.Add(new Label
                {
                    Text = "Brak tras. Utwórz pierwszą trasę w panelu po prawej stronie.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = ThemeManager.MutedTextColor,
                    Location = new Point(12, 16),
                    AutoSize = true
                });
                _pnlRoutesList.ResumeLayout();
                return;
            }

            int rowY = 6;
            int rowWidth = _pnlRoutesList.ClientSize.Width - 26;
            if (rowWidth < 200) rowWidth = 200;

            foreach (var route in routes)
            {
                var row = BuildRouteRow(route, buildingMap, rowWidth);
                row.Location = new Point(8, rowY);
                _pnlRoutesList.Controls.Add(row);
                rowY += row.Height + 6;
            }

            _pnlRoutesList.ResumeLayout();
        }

        private Panel BuildRouteRow(SupplyRoute route, Dictionary<string, Building> buildingMap, int width)
        {
            var row = new Panel
            {
                Size = new Size(width, 70),
                BackColor = Color.FromArgb(20, 34, 54)
            };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };

            string sourceName = route.SourceType == RouteSourceType.Market
                ? "🏪 Rynek"
                : (buildingMap.TryGetValue(route.SourceFacilityId, out var src) ? $"🏭 {src.Name}" : "❓ (brak źródła)");
            string destName = buildingMap.TryGetValue(route.TargetFacilityId, out var dst) ? dst.Name : "❓ (brak celu)";

            row.Controls.Add(new Label
            {
                Text = $"{sourceName}  ➔  {destName}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeManager.TextColor,
                Location = new Point(8, 6),
                Size = new Size(width - 130, 18),
                AutoEllipsis = true
            });

            var vehicle = VehicleRegistry.Get(route.VehicleTypeName);
            row.Controls.Add(new Label
            {
                Text = $"{route.ResourceName} × {route.AmountPerTrip}  •  {vehicle.DisplayName.Split('[')[0].Trim()}  •  co {route.IntervalHours}h  •  {PriorityText(route.Priority)}",
                Font = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location = new Point(8, 26),
                Size = new Size(width - 16, 16),
                AutoEllipsis = true
            });

            row.Controls.Add(new Label
            {
                Text = route.IsEnabled ? route.LastTripResult : "⏸ Wstrzymana",
                Font = ThemeManager.SmallFont,
                ForeColor = StatusColor(route),
                Location = new Point(8, 46),
                Size = new Size(width - 130, 16),
                AutoEllipsis = true
            });

            // Przyciski: pauza/wznów + usuń
            Button btnToggle = new Button
            {
                Text = route.IsEnabled ? "⏸" : "▶",
                Size = new Size(40, 24),
                Location = new Point(width - 96, 8),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            ThemeManager.ApplySecondaryButtonTheme(btnToggle);
            btnToggle.Click += (s, e) => { route.IsEnabled = !route.IsEnabled; RebuildRoutesList(); };
            row.Controls.Add(btnToggle);

            Button btnDelete = new Button
            {
                Text = "🗑",
                Size = new Size(40, 24),
                Location = new Point(width - 50, 8),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            ThemeManager.ApplySecondaryButtonTheme(btnDelete);
            btnDelete.ForeColor = ThemeManager.NegativeColor;
            btnDelete.Click += (s, e) =>
            {
                _gm!.Logistics.RemoveRoute(route.Id);
                RebuildRoutesList();
            };
            row.Controls.Add(btnDelete);

            // Etykiety dodane wcześniej są na wierzchu (z-order WinForms) — przyciski na wierzch,
            // aby cały ich obszar był klikalny.
            btnToggle.BringToFront();
            btnDelete.BringToFront();

            return row;
        }

        private static string PriorityText(RoutePriority p) => p switch
        {
            RoutePriority.High   => "Priorytet: Wysoki",
            RoutePriority.Medium => "Priorytet: Średni",
            _                    => "Priorytet: Niski"
        };

        private static Color StatusColor(SupplyRoute route)
        {
            if (!route.IsEnabled) return ThemeManager.MutedTextColor;
            string r = route.LastTripResult;
            if (r.Contains("W drodze") || r.StartsWith("✅")) return ThemeManager.PositiveColor;
            if (r.Contains("Brak środków") || r.Contains("nie istnieje")) return ThemeManager.NegativeColor;
            if (r.Contains("flotę") || r.Contains("pełny") || r.Contains("Oczekiwanie")) return ThemeManager.GoldColor;
            return ThemeManager.MutedTextColor;
        }

        // ─────────────────────────────────────────────────────────
        //  Pomocnicze tworzenie kontrolek formularza
        // ─────────────────────────────────────────────────────────

        private Label MakeSectionLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                Location = new Point(12, y),
                AutoSize = false,
                Size = new Size(290, 20)
            };
            y += 26;
            return lbl;
        }

        private Label MakeFieldLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location = new Point(12, y),
                AutoSize = false,
                Size = new Size(280, 16)
            };
            y += 18;
            return lbl;
        }

        private ComboBox MakeCombo(ref int y)
        {
            var cmb = new ComboBox
            {
                Location = new Point(12, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                Font = ThemeManager.DefaultFont
            };
            y += 32;
            return cmb;
        }
    }
}
