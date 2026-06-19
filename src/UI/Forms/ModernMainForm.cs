using System;
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
        
        private System.Windows.Forms.Timer _gameTimer;
        private SelectedBlueprint _selectedBlueprint = SelectedBlueprint.None;


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

            mainContainer = new MainViewContainer();
            mainContainer.Dock = DockStyle.Fill;
            this.Controls.Add(mainContainer);

            startScreen = new StartScreenControl();
            startScreen.StartNewGameClicked += StartScreen_StartNewGameClicked;
            startScreen.ExitClicked += StartScreen_ExitClicked;

            ShowStartScreen();
        }

        private void ShowStartScreen()
        {
            mainContainer.LeftSidebar.Visible = false;
            mainContainer.TopToolbar.Visible = false;
            mainContainer.BottomStatusBar.Visible = false;
            mainContainer.SetMainContent(startScreen);
        }

        private void StartScreen_ExitClicked(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void StartScreen_StartNewGameClicked(object? sender, EventArgs e)
        {
            // Pokaż UI gry
            mainContainer.LeftSidebar.Visible = true;
            mainContainer.TopToolbar.Visible = true;
            mainContainer.BottomStatusBar.Visible = true;
            
            // Inicjalizacja danych gry
            _company = new Company("My Tycoon Company", 1000000m);
            _map = new Map(10, 10);
            _gameManager = new GameManager(_company, _map);
            
            // Inicjalizacja paneli
            _mapControl = new IsometricMapControl();
            _mapControl.Dock = DockStyle.Fill;
            _mapControl.Initialize(_map, _gameManager);
            
            _corporatePanel = new CorporatePanelControl(_company);
            _financePanel = new FinancePanelControl(_company);
            
            // Inicjalizacja Inspektora Budynków
            _inspectorControl = new BuildingInspectorControl(null!);
            _inspectorControl.Visible = false; // Na starcie ukryte
            
            // Inicjalizacja Paska Budowania
            _buildPanel = new BuildPanelControl(_gameManager, _mapControl);
            _buildPanel.Visible = false;
            
            _mapControl.OnTileSelected += MapControl_OnTileSelected;
            
            // Domyślny widok to mapa (zajmująca całą wolną przestrzeń)
            mainContainer.SetMainContent(_mapControl);

            // Wyłączamy stary boczny pasek i górny, aby mapa zajmowała większość ekranu
            mainContainer.LeftSidebar.Visible = false;
            mainContainer.TopToolbar.Visible = false;
            
            // Konfiguracja dolnego paska na HUD rodem z Capitalism Lab
            mainContainer.BottomStatusBar.Height = 100; // Wysoki panel na newsy i ikony
            mainContainer.BottomStatusBar.Controls.Clear();
            
            _bottomHud = new BottomHudControl(_company, _gameManager);
            _bottomHud.OnMapClicked += (s, ev) => 
            {
                mainContainer.HideOverlay(_financePanel);
                mainContainer.HideOverlay(_corporatePanel);
            };
            _bottomHud.OnFinanceClicked += (s, ev) => 
            {
                _financePanel.RefreshData();
                mainContainer.ShowOverlay(_financePanel);
            };
            _bottomHud.OnCorporateClicked += (s, ev) => 
            {
                _corporatePanel.RefreshData();
                mainContainer.ShowOverlay(_corporatePanel);
            };
            _bottomHud.OnBuildClicked += (s, ev) => 
            {
                mainContainer.ShowOverlay(_buildPanel);
            };
            
            mainContainer.BottomStatusBar.Controls.Add(_bottomHud);
            
            // Odpalenie ticków
            _gameTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _gameTimer.Tick += (s, ev) => _gameManager.NextTick();
            _gameTimer.Start();
            
            // Reakcja na kontrolę czasu z HUD
            _bottomHud.OnTimePaused += (s, ev) => _gameTimer.Stop();
            _bottomHud.OnTimeNormal += (s, ev) => { _gameTimer.Interval = 1000; _gameTimer.Start(); };
            _bottomHud.OnTimeFast += (s, ev) => { _gameTimer.Interval = 250; _gameTimer.Start(); };
            
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
                                $"Budowa: {newBuilding.Name}", -newBuilding.BuildCost, "Inwestycje");
                            
                            _gameManager.ActiveCompany.Buildings.Add(newBuilding);
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
                _ => null!
            };
        }
    }
}
