using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Conglomerate.UI.Controls;

using Conglomerate.Financials;

namespace Conglomerate.UI.Forms
{
    public class ModernMainForm : Form
    {
        private MainViewContainer mainContainer;
        private StartScreenControl startScreen;

        private Company? _company;
        private Map? _map;
        private GameManager? _gameManager;
        
        private IsometricMapControl _mapControl;
        private CorporatePanelControl _corporatePanel;
        private FinancePanelControl _financePanel;
        private BottomHudControl _bottomHud;
        private BuildingInspectorControl _inspectorControl;
        private BuildPanelControl _buildPanel;

        // Panele systemów Capitalism Lab (Giełda / Bank / Logistyka)
        private StockMarketForm _stockForm;
        private BankingForm _bankForm;
        private MarketReportForm _marketForm;
        private ExecutivesForm _execForm;
        private CapLabOverlayHost _stockHost;
        private CapLabOverlayHost _bankHost;
        private CapLabOverlayHost _marketHost;
        private CapLabOverlayHost _execHost;
        private LogisticsPanelControl _logisticsPanel;
        private HRPanelControl _hrPanel;
        
        private System.Windows.Forms.Timer _gameTimer;
        private SelectedBlueprint _selectedBlueprint = SelectedBlueprint.None;

        // Rejestr nakładek: klucz → (kontrolka, akcja przed pokazaniem)
        private readonly Dictionary<string, (Control Overlay, Action? OnShow)> _overlayRegistry = new();


        public ModernMainForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Conglomerate Tycoon (Modern UI)";
            this.Size = new Size(1280, 720);
            this.MinimumSize = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Globalna obsługa klawiszy (ESC zamyka aktywne okno/nakładkę)
            this.KeyPreview = true;
            this.KeyDown += ModernMainForm_KeyDown;

            mainContainer = new MainViewContainer();
            mainContainer.Dock = DockStyle.Fill;
            this.Controls.Add(mainContainer);

            startScreen = new StartScreenControl();
            startScreen.StartNewGameClicked += StartScreen_StartNewGameClicked;
            startScreen.LoadGameClicked += StartScreen_LoadGameClicked;
            startScreen.ExitClicked += StartScreen_ExitClicked;

            ShowStartScreen();
        }

        private void ModernMainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (CloseTopmostOverlay())
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void RegisterOverlay(string key, Control overlay, Action? onShow = null)
        {
            _overlayRegistry[key] = (overlay, onShow);
            overlay.Visible = false;
        }

        private void ShowRegisteredOverlay(string key)
        {
            if (!_overlayRegistry.TryGetValue(key, out var entry)) return;
            entry.OnShow?.Invoke();
            mainContainer.ShowOverlay(entry.Overlay);
        }

        private void HideAllOverlays()
        {
            foreach (var (overlay, _) in _overlayRegistry.Values)
                mainContainer.HideOverlay(overlay);
        }

        /// <summary>Zamyka najwyżej położoną widoczną nakładkę. Zwraca true, jeśli coś zamknięto.</summary>
        private bool CloseTopmostOverlay()
        {
            if (mainContainer == null) return false;

            Control? topmost = null;
            int bestIndex = int.MaxValue;

            foreach (var (overlay, _) in _overlayRegistry.Values)
            {
                if (!overlay.Visible) continue;
                if (!mainContainer.MainWorkspace.Controls.Contains(overlay)) continue;

                int idx = mainContainer.MainWorkspace.Controls.GetChildIndex(overlay);
                if (idx < bestIndex)
                {
                    bestIndex = idx;
                    topmost = overlay;
                }
            }

            if (topmost != null)
            {
                mainContainer.HideOverlay(topmost);
                return true;
            }
            return false;
        }

        private void ShowStartScreen()
        {
            mainContainer.LeftSidebar.Visible = false;
            mainContainer.TopToolbar.Visible = false;
            mainContainer.BottomStatusBar.Visible = false;
            // Usuń pozostałości po rozgrywce (mapa + nakładki), aby nie nachodziły na menu
            mainContainer.MainWorkspace.Controls.Clear();
            mainContainer.SetMainContent(startScreen);
        }

        private void StartScreen_ExitClicked(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void StartScreen_StartNewGameClicked(object? sender, EventArgs e)
        {
            // Konfiguracja nowej gry (nazwa firmy, kapitał, mapa, konkurencja…)
            using var dlg = new NewGameDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var settings = dlg.Settings;

            // Inicjalizacja danych nowej gry na podstawie ustawień gracza
            _company = new Company(settings.CompanyName, settings.StartingCash);
            _map = new Map(settings.MapWidth, settings.MapHeight);
            _gameManager = new GameManager(_company, _map, settings);

            BuildGameView();
        }

        /// <summary>
        /// Buduje cały widok rozgrywki na podstawie aktualnych pól _company / _map / _gameManager.
        /// Współdzielone przez „Nową grę" oraz „Wczytaj grę".
        /// </summary>
        private void BuildGameView()
        {
            // Zatrzymaj poprzedni zegar (przy ponownym wejściu do gry / wczytaniu)
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
                _gameTimer = null!;
            }

            _overlayRegistry.Clear();

            // Wyczyść poprzedni widok (mapa + nakładki z poprzedniej rozgrywki)
            mainContainer.MainWorkspace.Controls.Clear();

            // Pokaż UI gry
            mainContainer.LeftSidebar.Visible = true;
            mainContainer.BottomStatusBar.Visible = true;

            // Górny pasek narzędzi (Zapisz / Wczytaj / Menu)
            BuildTopToolbar();

            // Inicjalizacja paneli
            _mapControl = new IsometricMapControl();
            _mapControl.Dock = DockStyle.Fill;
            _mapControl.Initialize(_map, _gameManager);
            
            _corporatePanel = new CorporatePanelControl(_company);
            _financePanel = new FinancePanelControl(_company);
            
            // Inicjalizacja Inspektora Budynków
            _inspectorControl = new BuildingInspectorControl(null!);
            _inspectorControl.SetCompany(_company);
            _inspectorControl.SetGameManager(_gameManager);
            _inspectorControl.OnInventoryChanged += (s, ev) => _bottomHud?.RefreshData();
            _inspectorControl.Visible = false; // Na starcie ukryte
            
            // Inicjalizacja Paska Budowania
            _buildPanel = new BuildPanelControl(_gameManager, _mapControl);
            _buildPanel.Visible = false;
            
            _mapControl.OnTileSelected += MapControl_OnTileSelected;
            
            // Domyślny widok to mapa (zajmująca całą wolną przestrzeń)
            mainContainer.SetMainContent(_mapControl);

            // Wyłączamy lewy boczny pasek, aby mapa zajmowała większość ekranu
            mainContainer.LeftSidebar.Visible = false;

            // Konfiguracja dolnego paska na HUD rodem z Capitalism Lab
            mainContainer.BottomStatusBar.Height = 100; // Wysoki panel na newsy i ikony
            mainContainer.BottomStatusBar.Controls.Clear();
            
            _bottomHud = new BottomHudControl(_company, _gameManager);
            RegisterOverlay("inspector",  _inspectorControl);
            RegisterOverlay("build",      _buildPanel);
            RegisterOverlay("finance",    _financePanel,   () => _financePanel.RefreshData());
            RegisterOverlay("corporate",  _corporatePanel, () => _corporatePanel.RefreshData());

            // ── Systemy Capitalism Lab: Giełda / Bank / Logistyka ──────────────
            _stockForm = new StockMarketForm();
            _stockHost = new CapLabOverlayHost("📈 Giełda Papierów Wartościowych", 860, 620, _stockForm);

            _bankForm = new BankingForm();
            _bankHost = new CapLabOverlayHost("🏦 Bank — Kredyty i Finansowanie", 740, 540, _bankForm);

            _logisticsPanel = new LogisticsPanelControl();

            // ── Marketing / Dyrektorzy (C-Suite) / Kadry (HR) ──────────────────
            _marketForm = new MarketReportForm();
            _marketHost = new CapLabOverlayHost("📢 Marketing i Analiza Rynku", 720, 700, _marketForm);

            _execForm = new ExecutivesForm();
            _execHost = new CapLabOverlayHost("👔 Zarząd (C-Suite)", 740, 680, _execForm);

            _hrPanel = new HRPanelControl();

            RegisterOverlay("stock",      _stockHost,      () => { _stockForm.SetGameManager(_gameManager!, _company!); _stockForm.RefreshData(); });
            RegisterOverlay("bank",       _bankHost,       () => { _bankForm.SetGameManager(_gameManager!, _company!); _bankForm.RefreshData(); });
            RegisterOverlay("logistics",  _logisticsPanel, () => _logisticsPanel.SetData(_gameManager!));
            RegisterOverlay("market",     _marketHost,     () => { _marketForm.SetGameManager(_gameManager!, _company!); _marketForm.RefreshData(); });
            RegisterOverlay("executives", _execHost,       () => { _execForm.SetGameManager(_gameManager!, _company!); _execForm.RefreshData(); });
            RegisterOverlay("hr",         _hrPanel,        () => _hrPanel.SetData(_gameManager!));

            _bottomHud.OnModuleClicked += (s, key) =>
            {
                if (key == "map") { HideAllOverlays(); return; }
                ShowRegisteredOverlay(key);
            };

            mainContainer.BottomStatusBar.Controls.Add(_bottomHud);
            
            // Odpalenie ticków
            _gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _gameTimer.Tick += (s, ev) =>
            {
                _gameManager.NextTick();
                if (_inspectorControl.Visible) _inspectorControl.RefreshData();
            };
            _gameTimer.Start();
            
            // Reakcja na kontrolę czasu z HUD (1x/2x/3x/5x/10x + pauza)
            _bottomHud.OnTimePaused += (s, ev) => _gameTimer?.Stop();
            _bottomHud.OnSpeedSelected += (s, intervalMs) =>
            {
                if (_gameTimer == null) return;
                _gameTimer.Interval = intervalMs;
                _gameTimer.Start();
            };
            
            _buildPanel.OnBlueprintSelected += (s, blueprint) => 
            {
                _selectedBlueprint = blueprint;
                _mapControl.SetBuildMode(true);
            };
        }

        private void MapControl_OnTileSelected(Microsoft.Xna.Framework.Point pt)
        {
            if (_map == null) return;
            var tile = _map.GetTile(pt.X, pt.Y);
            if (tile == null) return;

            if (_mapControl.GetBuildMode() && _selectedBlueprint != SelectedBlueprint.None)
            {
                if (tile.Type == TileType.Grass && tile.Building == null)
                {
                    Building newBuilding = CreateBuildingFromBlueprint(_selectedBlueprint);
                    if (newBuilding != null)
                    {
                        if (_gameManager.ActiveCompany.Balance >= newBuilding.BuildCost)
                        {
                            _gameManager.ActiveCompany.Balance -= newBuilding.BuildCost;
                            _gameManager.ActiveCompany.AddTransaction(_gameManager.CurrentDay, _gameManager.CurrentHour, 
                                $"Budowa: {newBuilding.Name}", -newBuilding.BuildCost, "Budowa");
                            
                            _gameManager.ActiveCompany.Buildings.Add(newBuilding);
                            _gameManager.ActiveCompany.Engine.RegisterFacility(newBuilding);
                            _map.BuildBuildingOnTile(pt.X, pt.Y, newBuilding);
                        }
                        else
                        {
                            MessageBox.Show("Brak środków!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                
                _mapControl.SetBuildMode(false);
                _selectedBlueprint = SelectedBlueprint.None;
                mainContainer.HideOverlay(_buildPanel);
                return;
            }

            if (tile.Building != null)
            {
                // Mamy budynek! Odpalamy inspektor.
                _inspectorControl.SetBuilding(tile.Building);
                mainContainer.ShowOverlay(_inspectorControl);
            }
            else
            {
                mainContainer.HideOverlay(_inspectorControl);
            }
        }
        
        private Building CreateBuildingFromBlueprint(SelectedBlueprint bp)
        {
            return bp switch
            {
                SelectedBlueprint.Farm => new Farm("Farma Krów"),
                SelectedBlueprint.CoalMine => new CoalMine("Kopalnia Węgla"),
                SelectedBlueprint.CopperMine => new CopperMine("Kopalnia Miedzi"),
                SelectedBlueprint.FoodWarehouse => new WarehouseBuilding("Magazyn Żywności", ResourceCategory.Food),
                SelectedBlueprint.MiningWarehouse => new WarehouseBuilding("Magazyn Kopalniany", ResourceCategory.Mining),
                SelectedBlueprint.CheeseFactory => new CheeseFactory("Fabryka Sera"),
                SelectedBlueprint.CopperFoundry => new CopperFoundry("Huta Miedzi"),
                SelectedBlueprint.GeneralStore => new GeneralStore("Sklep Wielobranżowy"),
                SelectedBlueprint.RNDCenter => new RNDCenter("Centrum R&D"),
                _ => null!
            };
        }

        // ─────────────────────────────────────────────────────────
        //  Górny pasek: Zapisz / Wczytaj / Menu
        // ─────────────────────────────────────────────────────────

        private void BuildTopToolbar()
        {
            var bar = mainContainer.TopToolbar;
            bar.Visible = true;
            bar.Height = 36;

            // Usuń poprzednie przyciski (zachowując dolną linię/ramki)
            for (int i = bar.Controls.Count - 1; i >= 0; i--)
                if (bar.Controls[i] is Button) bar.Controls.RemoveAt(i);

            Button MakeBtn(string text, int rightOffset, Color fg)
            {
                var b = new Button
                {
                    Text     = text,
                    Size     = new Size(96, 26),
                    Location = new Point(bar.Width - rightOffset, 5),
                    Anchor   = AnchorStyles.Top | AnchorStyles.Right
                };
                ThemeManager.ApplySecondaryButtonTheme(b);
                b.ForeColor = fg;
                bar.Controls.Add(b);
                return b;
            }

            var btnMenu = MakeBtn("☰ Menu",     112, ThemeManager.MutedTextColor);
            var btnLoad = MakeBtn("📂 Wczytaj",  214, ThemeManager.TextColor);
            var btnSave = MakeBtn("💾 Zapisz",   316, ThemeManager.GoldColor);

            btnSave.Click += (s, e) => DoSave();
            btnLoad.Click += (s, e) => DoLoadInGame();
            btnMenu.Click += (s, e) => ReturnToMainMenu();
        }

        // ─────────────────────────────────────────────────────────
        //  Zapis / Wczytywanie
        // ─────────────────────────────────────────────────────────

        private void DoSave()
        {
            if (_company == null || _map == null || _gameManager == null) return;

            bool wasRunning = _gameTimer != null && _gameTimer.Enabled;
            _gameTimer?.Stop();

            string defaultName = $"{_company.Name} - Dzień {_gameManager.CurrentDay}";
            using var dlg = new SaveNameDialog(defaultName);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    SaveGameManager.SaveGame(dlg.SaveName, _company, _map, _gameManager);
                    MessageBox.Show($"Gra została zapisana jako:\n„{dlg.SaveName}”", "Zapis gry",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisu: {ex.Message}", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (wasRunning) _gameTimer?.Start();
        }

        private void DoLoadInGame()
        {
            bool wasRunning = _gameTimer != null && _gameTimer.Enabled;
            _gameTimer?.Stop();

            using var dlg = new LoadGameDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedPath != null)
                LoadGameFromFile(dlg.SelectedPath);
            else if (wasRunning)
                _gameTimer?.Start();
        }

        private void StartScreen_LoadGameClicked(object? sender, EventArgs e)
        {
            using var dlg = new LoadGameDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedPath != null)
                LoadGameFromFile(dlg.SelectedPath);
        }

        private void LoadGameFromFile(string filePath)
        {
            try
            {
                var container = SaveGameManager.LoadGame(filePath);
                if (container?.State == null)
                {
                    MessageBox.Show("Nie udało się wczytać pliku zapisu!", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var state = container.State;
                _gameTimer?.Stop();

                // 1. Firma + stan finansowy
                _company = new Company(state.CompanyName, state.Cash);
                _company.Engine.RestoreState(
                    state.Cash, state.ShareCapital, state.RetainedEarnings,
                    state.Loans, state.CurrentMonthIndex, state.TaxRate);

                // 2. Mapa
                _map = new Map(10, 10);

                // 3. Budynki
                foreach (var bData in state.Buildings)
                {
                    var building = SaveGameManager.RestoreBuilding(bData);
                    if (building == null) continue;

                    building.X = bData.X;
                    building.Y = bData.Y;
                    _company.Buildings.Add(building);
                    _company.Engine.RegisterFacility(building);
                    _map.BuildBuildingOnTile(bData.X, bData.Y, building);
                }

                // 4. Menedżer gry + dzień/godzina + trasy logistyczne
                _gameManager = new GameManager(_company, _map);
                _gameManager.RestoreState(state.CurrentDay, state.CurrentHour);
                if (state.SupplyRoutes != null)
                    _gameManager.Logistics.RestoreRoutes(state.SupplyRoutes);

                // 5. Zbuduj widok gry
                BuildGameView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania gry: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReturnToMainMenu()
        {
            bool wasRunning = _gameTimer != null && _gameTimer.Enabled;
            _gameTimer?.Stop();

            var res = MessageBox.Show(
                "Wrócić do menu głównego?\nNiezapisany postęp zostanie utracony.",
                "Menu główne", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes)
            {
                if (wasRunning) _gameTimer?.Start();
                return;
            }

            ShowStartScreen();
        }
    }
}
