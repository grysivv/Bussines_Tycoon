using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using XnaPoint = Microsoft.Xna.Framework.Point;

namespace Conglomerate
{
    public class MainForm : Form
    {
        private Company? _company;
        private Map? _map;
        private GameManager? _gameManager;

        // Kontenery scen (Jedno okno)
        private Panel pnlStartScreen = null!;
        private Panel pnlStartCenter = null!;
        private Panel pnlGameBoard = null!;

        // Zegar gry (System Tick)
        private System.Windows.Forms.Timer _gameTimer = null!;

        // Stan zaznaczenia
        private XnaPoint? _selectedTile = null;

        // Kontrolki gry
        private Panel pnlLeft = null!;
        private Panel pnlRight = null!;
        private Panel pnlBottom = null!;
        private IsometricMapControl mapControl = null!;

        private Label lblCompanyName = null!;
        private Label lblCash = null!;
        private Label lblDay = null!;
        private Label lblBottomStatus = null!;
        private Label lblSelectedTileInfo = null!;

        private enum SelectedBlueprint { None, Farm, CoalMine }
        private SelectedBlueprint _selectedBlueprint = SelectedBlueprint.None;
        private Button btnBuildFarm = null!;
        private Button btnBuildCoalMine = null!;
        private Button btnCenterCamera = null!;

        // Przyciski kontroli prędkości czasu
        private Button btnSpeedPause = null!;
        private Button btnSpeed1x = null!;
        private Button btnSpeed2x = null!;
        private Button btnSpeed3x = null!;
        private Button btnSpeed5x = null!;
        private Button? _activeSpeedButton = null;

        private XnaPoint? _hoveredTile = null;
        private Panel pnlBuildingDetails = null!;
        private Building? _inspectingBuilding = null;
        private Dictionary<string, string> _enteredSellQuantities = new Dictionary<string, string>();
        private Panel pnlFinanceReport = null!;

        public MainForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            
            // Konfiguracja zegara automatycznej symulacji czasu
            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Tick += (s, e) =>
            {
                if (_gameManager != null)
                {
                    _gameManager.NextTick();
                }
            };

            // Ustawienie okna na pełny ekran / zmaksymalizowane
            this.WindowState = FormWindowState.Maximized;
        }

        private void InitializeComponent()
        {
            this.Text = "Conglomerate Tycoon - Symulator Farmy Izometrycznej";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            // ========================================================
            // SCENA 1: EKRAN POWITALNY (STARTOWY)
            // ========================================================
            pnlStartScreen = new Panel();
            pnlStartScreen.Dock = DockStyle.Fill;
            pnlStartScreen.BackColor = Color.FromArgb(20, 20, 20);
            this.Controls.Add(pnlStartScreen);

            pnlStartCenter = new Panel();
            pnlStartCenter.Size = new Size(400, 250);
            pnlStartCenter.BackColor = Color.FromArgb(30, 30, 30);
            pnlStartScreen.Controls.Add(pnlStartCenter);

            // Zapewnienie automatycznego centrowania przy każdej zmianie rozmiaru panelu startowego
            pnlStartScreen.SizeChanged += (s, e) =>
            {
                pnlStartCenter.Location = new Point(
                    (pnlStartScreen.Width - pnlStartCenter.Width) / 2,
                    (pnlStartScreen.Height - pnlStartCenter.Height) / 2
                );
            };

            // Obramowanie ozdobne dla panelu startowego
            Panel pnlStartBorder = new Panel();
            pnlStartBorder.Dock = DockStyle.Top;
            pnlStartBorder.Height = 4;
            pnlStartBorder.BackColor = Color.FromArgb(50, 150, 250);
            pnlStartCenter.Controls.Add(pnlStartBorder);

            Label lblTitle = new Label();
            lblTitle.Text = "CONGLOMERATE";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(20, 20);
            lblTitle.Size = new Size(360, 40);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlStartCenter.Controls.Add(lblTitle);

            Label lblSubTitleStart = new Label();
            lblSubTitleStart.Text = "Isometric Business Tycoon Simulator";
            lblSubTitleStart.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblSubTitleStart.ForeColor = Color.DarkGray;
            lblSubTitleStart.Location = new Point(20, 60);
            lblSubTitleStart.Size = new Size(360, 20);
            lblSubTitleStart.TextAlign = ContentAlignment.MiddleCenter;
            pnlStartCenter.Controls.Add(lblSubTitleStart);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Wprowadź nazwę swojej nowej korporacji:";
            lblPrompt.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblPrompt.Location = new Point(30, 95);
            lblPrompt.Size = new Size(340, 20);
            pnlStartCenter.Controls.Add(lblPrompt);

            TextBox txtCompanyName = new TextBox();
            txtCompanyName.Text = "Moja Korporacja";
            txtCompanyName.Font = new Font("Segoe UI", 10);
            txtCompanyName.Location = new Point(30, 120);
            txtCompanyName.Size = new Size(340, 25);
            txtCompanyName.BackColor = Color.FromArgb(45, 45, 45);
            txtCompanyName.ForeColor = Color.White;
            txtCompanyName.BorderStyle = BorderStyle.FixedSingle;
            pnlStartCenter.Controls.Add(txtCompanyName);

            Button btnStartGame = new Button();
            btnStartGame.Text = "Rozpocznij Symulację";
            btnStartGame.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnStartGame.Location = new Point(30, 165);
            btnStartGame.Size = new Size(340, 45);
            btnStartGame.FlatStyle = FlatStyle.Flat;
            btnStartGame.FlatAppearance.BorderSize = 0;
            btnStartGame.BackColor = Color.FromArgb(50, 150, 250);
            btnStartGame.ForeColor = Color.White;
            btnStartGame.Cursor = Cursors.Hand;
            btnStartGame.Click += (s, e) =>
            {
                string name = txtCompanyName.Text.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    StartGame(name);
                }
                else
                {
                    MessageBox.Show("Nazwa korporacji nie może być pusta!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            pnlStartCenter.Controls.Add(btnStartGame);

            // ========================================================
            // SCENA 2: PLANSZA ROZGRYWKI (GRA)
            // ========================================================
            pnlGameBoard = new Panel();
            pnlGameBoard.Dock = DockStyle.Fill;
            pnlGameBoard.Visible = false; // Domyślnie ukryta
            this.Controls.Add(pnlGameBoard);

            // 2.0 PANEL GÓRNY NAWIGACJI
            Panel pnlTopNav = new Panel();
            pnlTopNav.Dock = DockStyle.Top;
            pnlTopNav.Height = 45;
            pnlTopNav.BackColor = Color.FromArgb(20, 20, 20);
            pnlTopNav.Padding = new Padding(15, 0, 15, 0);
            pnlGameBoard.Controls.Add(pnlTopNav);

            Label lblLogo = new Label();
            lblLogo.Text = "CONGLOMERATE TYCOON 2";
            lblLogo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblLogo.ForeColor = Color.FromArgb(50, 150, 250);
            lblLogo.Location = new Point(15, 12);
            lblLogo.Size = new Size(200, 25);
            pnlTopNav.Controls.Add(lblLogo);

            // Przycisk FINANSE
            Button btnNavFinance = new Button();
            btnNavFinance.Text = "FINANSE";
            btnNavFinance.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnNavFinance.Location = new Point(230, 8);
            btnNavFinance.Size = new Size(100, 30);
            btnNavFinance.FlatStyle = FlatStyle.Flat;
            btnNavFinance.FlatAppearance.BorderSize = 1;
            btnNavFinance.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnNavFinance.BackColor = Color.FromArgb(35, 35, 35);
            btnNavFinance.ForeColor = Color.FromArgb(50, 150, 250);
            btnNavFinance.Cursor = Cursors.Hand;
            btnNavFinance.Click += (s, e) => ToggleFinanceReport();
            pnlTopNav.Controls.Add(btnNavFinance);

            // Atrapy innych przycisków
            Button btnNavMarket = new Button();
            btnNavMarket.Text = "RYNEK (Lock)";
            btnNavMarket.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            btnNavMarket.Location = new Point(340, 8);
            btnNavMarket.Size = new Size(100, 30);
            btnNavMarket.FlatStyle = FlatStyle.Flat;
            btnNavMarket.FlatAppearance.BorderSize = 1;
            btnNavMarket.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnNavMarket.BackColor = Color.FromArgb(25, 25, 25);
            btnNavMarket.ForeColor = Color.Gray;
            btnNavMarket.Enabled = false;
            pnlTopNav.Controls.Add(btnNavMarket);

            Button btnNavCongress = new Button();
            btnNavCongress.Text = "KONGRES (Lock)";
            btnNavCongress.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            btnNavCongress.Location = new Point(450, 8);
            btnNavCongress.Size = new Size(110, 30);
            btnNavCongress.FlatStyle = FlatStyle.Flat;
            btnNavCongress.FlatAppearance.BorderSize = 1;
            btnNavCongress.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnNavCongress.BackColor = Color.FromArgb(25, 25, 25);
            btnNavCongress.ForeColor = Color.Gray;
            btnNavCongress.Enabled = false;
            pnlTopNav.Controls.Add(btnNavCongress);

            // 2.1 PANEL LEWY (Statystyki i Akcje)
            pnlLeft = new Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 260;
            pnlLeft.BackColor = Color.FromArgb(24, 24, 24);
            pnlLeft.Padding = new Padding(15);
            pnlGameBoard.Controls.Add(pnlLeft);

            // Nagłówek gry w panelu bocznym
            Label lblGameTitle = new Label();
            lblGameTitle.Text = "CONGLOMERATE";
            lblGameTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblGameTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblGameTitle.Location = new Point(15, 20);
            lblGameTitle.Size = new Size(230, 30);
            pnlLeft.Controls.Add(lblGameTitle);

            Label lblSubTitleGame = new Label();
            lblSubTitleGame.Text = "Business Tycoon Simulator";
            lblSubTitleGame.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblSubTitleGame.ForeColor = Color.DarkGray;
            lblSubTitleGame.Location = new Point(17, 50);
            lblSubTitleGame.Size = new Size(230, 20);
            pnlLeft.Controls.Add(lblSubTitleGame);

            // Nazwa firmy
            lblCompanyName = new Label();
            lblCompanyName.Text = "Firma: ...";
            lblCompanyName.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblCompanyName.Location = new Point(15, 90);
            lblCompanyName.Size = new Size(230, 25);
            pnlLeft.Controls.Add(lblCompanyName);

            // Stan Gotówki
            Label lblCashTitle = new Label();
            lblCashTitle.Text = "SALDO FINANSOWE:";
            lblCashTitle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblCashTitle.ForeColor = Color.Gray;
            lblCashTitle.Location = new Point(15, 120);
            lblCashTitle.Size = new Size(230, 15);
            pnlLeft.Controls.Add(lblCashTitle);

            lblCash = new Label();
            lblCash.Text = "0,00 zł";
            lblCash.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblCash.ForeColor = Color.FromArgb(100, 220, 100);
            lblCash.Location = new Point(12, 135);
            lblCash.Size = new Size(230, 35);
            pnlLeft.Controls.Add(lblCash);

            // Dzień / Godzina
            lblDay = new Label();
            lblDay.Text = "DZIEŃ: 1 (08:00)";
            lblDay.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblDay.ForeColor = Color.FromArgb(240, 180, 50);
            lblDay.Location = new Point(15, 180);
            lblDay.Size = new Size(230, 25);
            pnlLeft.Controls.Add(lblDay);

            // Kontrola prędkości czasu (Pause, 1x, 2x, 3x, 5x)
            Label lblSpeedTitle = new Label();
            lblSpeedTitle.Text = "PRĘDKOŚĆ SYMULACJI:";
            lblSpeedTitle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblSpeedTitle.ForeColor = Color.Gray;
            lblSpeedTitle.Location = new Point(15, 215);
            lblSpeedTitle.Size = new Size(230, 15);
            pnlLeft.Controls.Add(lblSpeedTitle);

            Panel pnlSpeedControls = new Panel();
            pnlSpeedControls.Location = new Point(15, 230);
            pnlSpeedControls.Size = new Size(230, 40);
            pnlLeft.Controls.Add(pnlSpeedControls);

            btnSpeedPause = CreateSpeedButton("||", 0, (s, e) => SetGameSpeed(0, btnSpeedPause));
            btnSpeed1x = CreateSpeedButton("1x", 46, (s, e) => SetGameSpeed(1000, btnSpeed1x));
            btnSpeed2x = CreateSpeedButton("2x", 92, (s, e) => SetGameSpeed(500, btnSpeed2x));
            btnSpeed3x = CreateSpeedButton("3x", 138, (s, e) => SetGameSpeed(333, btnSpeed3x));
            btnSpeed5x = CreateSpeedButton("5x", 184, (s, e) => SetGameSpeed(200, btnSpeed5x));

            pnlSpeedControls.Controls.Add(btnSpeedPause);
            pnlSpeedControls.Controls.Add(btnSpeed1x);
            pnlSpeedControls.Controls.Add(btnSpeed2x);
            pnlSpeedControls.Controls.Add(btnSpeed3x);
            pnlSpeedControls.Controls.Add(btnSpeed5x);

            // Przycisk "Wycentruj Kamerę" - przesunięty w górę
            btnCenterCamera = new Button();
            btnCenterCamera.Text = "Wycentruj Widok";
            btnCenterCamera.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btnCenterCamera.Location = new Point(15, 280);
            btnCenterCamera.Size = new Size(230, 30);
            btnCenterCamera.FlatStyle = FlatStyle.Flat;
            btnCenterCamera.FlatAppearance.BorderSize = 0;
            btnCenterCamera.BackColor = Color.FromArgb(50, 50, 50);
            btnCenterCamera.ForeColor = Color.White;
            btnCenterCamera.Cursor = Cursors.Hand;
            btnCenterCamera.Click += (s, e) => mapControl.CenterCamera();
            pnlLeft.Controls.Add(btnCenterCamera);

            // Informacja o zaznaczonym polu
            Label lblSelectedTitle = new Label();
            lblSelectedTitle.Text = "HOVEROWANY KAFEL:";
            lblSelectedTitle.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblSelectedTitle.ForeColor = Color.Gray;
            lblSelectedTitle.Location = new Point(15, 330);
            lblSelectedTitle.Size = new Size(230, 15);
            pnlLeft.Controls.Add(lblSelectedTitle);

            lblSelectedTileInfo = new Label();
            lblSelectedTileInfo.Text = "Najedź myszką na mapę,\naby zobaczyć szczegóły.";
            lblSelectedTileInfo.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblSelectedTileInfo.ForeColor = Color.FromArgb(180, 180, 180);
            lblSelectedTileInfo.Location = new Point(15, 350);
            lblSelectedTileInfo.Size = new Size(230, 180);
            pnlLeft.Controls.Add(lblSelectedTileInfo);

            // 2.2 PANEL PRAWY (Budownictwo)
            pnlRight = new Panel();
            pnlRight.Dock = DockStyle.Right;
            pnlRight.Width = 190;
            pnlRight.BackColor = Color.FromArgb(24, 24, 24);
            pnlRight.Padding = new Padding(15);
            pnlGameBoard.Controls.Add(pnlRight);

            Label lblRightHeader = new Label();
            lblRightHeader.Text = "BUDOWNICTWO";
            lblRightHeader.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblRightHeader.ForeColor = Color.FromArgb(50, 150, 250);
            lblRightHeader.Location = new Point(15, 20);
            lblRightHeader.Size = new Size(160, 25);
            pnlRight.Controls.Add(lblRightHeader);

            btnBuildFarm = new Button();
            btnBuildFarm.Text = "Farma Krów\n(Koszt: $10k)";
            btnBuildFarm.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildFarm.Location = new Point(15, 60);
            btnBuildFarm.Size = new Size(160, 50);
            btnBuildFarm.FlatStyle = FlatStyle.Flat;
            btnBuildFarm.FlatAppearance.BorderSize = 1;
            btnBuildFarm.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildFarm.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildFarm.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildFarm.Cursor = Cursors.Hand;
            btnBuildFarm.Click += (s, e) => SelectBlueprint(SelectedBlueprint.Farm, btnBuildFarm);
            pnlRight.Controls.Add(btnBuildFarm);

            btnBuildCoalMine = new Button();
            btnBuildCoalMine.Text = "Kopalnia Węgla\n(Koszt: $15k)";
            btnBuildCoalMine.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildCoalMine.Location = new Point(15, 120);
            btnBuildCoalMine.Size = new Size(160, 50);
            btnBuildCoalMine.FlatStyle = FlatStyle.Flat;
            btnBuildCoalMine.FlatAppearance.BorderSize = 1;
            btnBuildCoalMine.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildCoalMine.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildCoalMine.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildCoalMine.Cursor = Cursors.Hand;
            btnBuildCoalMine.Click += (s, e) => SelectBlueprint(SelectedBlueprint.CoalMine, btnBuildCoalMine);
            pnlRight.Controls.Add(btnBuildCoalMine);

            // 2.3 PANEL DOLNY (Status)
            pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 40;
            pnlBottom.BackColor = Color.FromArgb(20, 20, 20);
            pnlBottom.Padding = new Padding(10, 5, 10, 5);
            pnlGameBoard.Controls.Add(pnlBottom);

            lblBottomStatus = new Label();
            lblBottomStatus.Text = "Sterowanie: [Lewy / Prawy myszy] Przesuwanie mapy | [Kółko myszy] Zoom | [Kliknięcie] Zaznaczenie kafla terenu";
            lblBottomStatus.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblBottomStatus.ForeColor = Color.DarkGray;
            lblBottomStatus.Location = new Point(10, 12);
            lblBottomStatus.Size = new Size(800, 20);
            pnlBottom.Controls.Add(lblBottomStatus);

            // 2.4 CENTRALNY MONOGAME PANEL (Renderer)
            mapControl = new IsometricMapControl();
            mapControl.Dock = DockStyle.Fill;
            pnlGameBoard.Controls.Add(mapControl);
            mapControl.BringToFront();

            // 2.5 PANEL SZCZEGÓŁÓW BUDYNKU (Floating Overlay Panel)
            pnlBuildingDetails = new Panel();
            pnlBuildingDetails.Size = new Size(400, 350);
            pnlBuildingDetails.BackColor = Color.FromArgb(30, 30, 30);
            pnlBuildingDetails.BorderStyle = BorderStyle.FixedSingle;
            pnlBuildingDetails.Visible = false;
            pnlGameBoard.Controls.Add(pnlBuildingDetails);
            pnlBuildingDetails.BringToFront();

            // 2.6 PANEL RAPORTU FINANSOWEGO (Floating Overlay Panel)
            pnlFinanceReport = new Panel();
            pnlFinanceReport.Size = new Size(600, 480);
            pnlFinanceReport.BackColor = Color.FromArgb(30, 30, 30);
            pnlFinanceReport.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Visible = false;
            pnlGameBoard.Controls.Add(pnlFinanceReport);
            pnlFinanceReport.BringToFront();

            // Zapewnienie automatycznego centrowania przy zmianie rozmiaru
            pnlGameBoard.SizeChanged += (s, e) => {
                CenterBuildingDetailsPanel();
                CenterFinanceReportPanel();
            };
        }

        private Button CreateSpeedButton(string text, int x, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(40, 32);
            btn.Location = new Point(x, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(50, 50, 50);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            return btn;
        }

        private void SetGameSpeed(int interval, Button clickedButton)
        {
            if (_activeSpeedButton != null)
            {
                _activeSpeedButton.BackColor = Color.FromArgb(50, 50, 50);
            }

            _activeSpeedButton = clickedButton;
            _activeSpeedButton.BackColor = Color.FromArgb(50, 150, 250);

            if (interval == 0)
            {
                _gameTimer.Stop();
                lblBottomStatus.Text = "Symulacja czasu zatrzymana (Pauza).";
            }
            else
            {
                _gameTimer.Interval = interval;
                _gameTimer.Start();
                lblBottomStatus.Text = $"Symulacja przyspieszona (1 tick = 1 godzina w grze. Prędkość: {clickedButton.Text}).";
            }
        }

        private void StartGame(string companyName)
        {
            // Inicjalizacja modeli silnika gry (Core)
            _company = new Company(companyName, 50000m);
            _map = new Map(10, 10);
            _gameManager = new GameManager(_company, _map);

            // Subskrypcja zdarzeń
            _gameManager.OnTickPerformed += OnTickPerformed;
            mapControl.OnTileSelected += OnTileSelectedOnMap;
            mapControl.OnTileHovered += OnTileHoveredOnMap;

            // Inicjalizacja danych mapy w kontrolce
            mapControl.Initialize(_map, _gameManager);

            // Przejście scen: Ukrycie startowego, wyświetlenie gry
            pnlStartScreen.Visible = false;
            pnlGameBoard.Visible = true;

            // Domyślne uruchomienie gry z prędkością 1x (tick co 1 sekunda)
            SetGameSpeed(1000, btnSpeed1x);

            RefreshStats();
        }

        private void ToggleBuildMode(bool activate)
        {
            mapControl.SetBuildMode(activate);

            if (mapControl.GetBuildMode())
            {
                string name = _selectedBlueprint == SelectedBlueprint.Farm ? "farmę krów ($10 000)" : "kopalnię węgla ($15 000)";
                lblBottomStatus.Text = $"TRYB BUDOWANIA AKTYWNY: Kliknij na wolny zielony kafel trawy, aby wybudować {name}.";
            }
            else
            {
                _selectedBlueprint = SelectedBlueprint.None;
                btnBuildFarm.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFarm.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildCoalMine.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildCoalMine.ForeColor = Color.FromArgb(50, 150, 250);
                lblBottomStatus.Text = "Sterowanie: [Lewy / Prawy myszy] Przesuwanie mapy | [Kółko myszy] Zoom | [Hover] Szczegóły kafla terenu";
            }
        }

        private void SelectBlueprint(SelectedBlueprint blueprint, Button clickedButton)
        {
            if (_selectedBlueprint == blueprint)
            {
                ToggleBuildMode(false);
            }
            else
            {
                // Reset other buttons
                btnBuildFarm.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFarm.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildCoalMine.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildCoalMine.ForeColor = Color.FromArgb(50, 150, 250);

                _selectedBlueprint = blueprint;
                clickedButton.BackColor = Color.FromArgb(50, 150, 250);
                clickedButton.ForeColor = Color.White;
                ToggleBuildMode(true);
            }
        }

        private void OnTileSelectedOnMap(XnaPoint tileCoord)
        {
            if (_map == null || _company == null) return;

            Tile tile;
            try
            {
                tile = _map.GetTile(tileCoord.X, tileCoord.Y);
            }
            catch
            {
                return;
            }

            _selectedTile = tileCoord;

            if (mapControl.GetBuildMode())
            {
                if (tile.Type == TileType.Grass)
                {
                    Building building;
                    string buildingName;
                    if (_selectedBlueprint == SelectedBlueprint.Farm)
                    {
                        buildingName = $"Farma Krów #{_company.Buildings.Count + 1}";
                        building = new Farm(buildingName);
                    }
                    else if (_selectedBlueprint == SelectedBlueprint.CoalMine)
                    {
                        buildingName = $"Kopalnia Węgla #{_company.Buildings.Count + 1}";
                        building = new CoalMine(buildingName);
                    }
                    else
                    {
                        return;
                    }

                    if (_company.BuyBuilding(building, _map, tileCoord.X, tileCoord.Y, _gameManager!.CurrentDay, _gameManager!.CurrentHour))
                    {
                        lblBottomStatus.Text = $"Pomyślnie wybudowano '{buildingName}' na polu ({tileCoord.X}, {tileCoord.Y})!";
                        ToggleBuildMode(false);
                        RefreshStats();
                        ShowTileInfo(tile);
                    }
                    else
                    {
                        MessageBox.Show($"Błąd inwestycji!\n\nUpewnij się, że masz wystarczające środki na budowę.\nWymagane: {building.BuildCost:C}\nDostępne: {_company.Balance:C}", "Brak Środków", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Budowa możliwa tylko na wolnych polach zielonych (Trawa).", "Zablokowane Pole", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                ShowTileInfo(tile);
                if (tile.Type == TileType.Building && tile.Building != null)
                {
                    OpenBuildingDetails(tile.Building);
                }
                else
                {
                    CloseBuildingDetails();
                }
            }
        }

        private void OnTileHoveredOnMap(XnaPoint? tileCoord)
        {
            _hoveredTile = tileCoord;
            if (_map == null) return;
            if (tileCoord.HasValue)
            {
                try
                {
                    var tile = _map.GetTile(tileCoord.Value.X, tileCoord.Value.Y);
                    ShowTileInfo(tile);
                }
                catch
                {
                    // Ignoruj out of bounds
                }
            }
            else
            {
                lblSelectedTileInfo.Text = "Najedź myszką na mapę,\naby zobaczyć szczegóły.";
            }
        }

        private void ShowTileInfo(Tile tile)
        {
            string info = $"Kafel: ({tile.X}, {tile.Y})\n";
            if (tile.Type == TileType.Grass)
            {
                info += "Typ: Trawa (Grass)\nStatus: Wolne pod zabudowę.";
            }
            else if (tile.Type == TileType.Water)
            {
                info += "Typ: Rzeka (Water)\nStatus: Naturalna przeszkoda.";
            }
            else if (tile.Type == TileType.Building && tile.Building != null)
            {
                var building = tile.Building;
                int totalStock = building.GetTotalStock();
                bool isFull = totalStock >= building.WarehouseCapacity;
                info += $"Typ: {building.ActivityType}\n" +
                        $"Nazwa: {building.Name}\n" +
                        $"Status: {(isFull ? "Magazyn pełny" : "Produkcja...")}\n" +
                        $"Utrzymanie: {building.MaintenanceCost:C}/dzień\n" +
                        $"Pojemność: {totalStock}/{building.WarehouseCapacity}\n" +
                        "Magazyn:\n";
                foreach (var kvp in building.Warehouse)
                {
                    info += $"  - {kvp.Key}: {kvp.Value} szt.\n";
                }
            }

            lblSelectedTileInfo.Text = info;
        }

        private void OnTickPerformed()
        {
            if (_map == null || _gameManager == null) return;

            RefreshStats();

            // Odświeżenie szczegółowych informacji o najechanym polu na żywo
            if (_hoveredTile.HasValue)
            {
                try
                {
                    var tile = _map.GetTile(_hoveredTile.Value.X, _hoveredTile.Value.Y);
                    ShowTileInfo(tile);
                }
                catch {}
            }

            // Odświeżenie szczegółów otwartego panelu budynku na żywo
            if (pnlBuildingDetails.Visible && _inspectingBuilding != null)
            {
                OpenBuildingDetails(_inspectingBuilding);
            }

            // Odświeżenie szczegółów otwartego raportu finansowego na żywo
            if (pnlFinanceReport.Visible)
            {
                UpdateFinanceReportView();
            }
        }

        private void RefreshStats()
        {
            if (_company == null || _gameManager == null) return;

            lblCompanyName.Text = $"Firma: {_company.Name}";
            lblCash.Text = $"{_company.Balance:C}";
            
            // Format zegara: Dzień X (Godzina YY:00)
            lblDay.Text = $"DZIEŃ: {_gameManager.CurrentDay} ({_gameManager.CurrentHour:D2}:00)";
        }

        private void OpenBuildingDetails(Building building)
        {
            bool isRefresh = pnlBuildingDetails.Visible && _inspectingBuilding == building;
            if (isRefresh)
            {
                // Zaktualizuj pojemność magazynu
                var lblCapRef = pnlBuildingDetails.Controls.Find("lblCap", true).FirstOrDefault() as Label;
                if (lblCapRef != null)
                {
                    lblCapRef.Text = $"Pojemność magazynu: {building.GetTotalStock()} / {building.WarehouseCapacity} szt.";
                }

                // Zaktualizuj stany magazynowe surowców
                foreach (var key in building.Warehouse.Keys)
                {
                    var lblResInfoRef = pnlBuildingDetails.Controls.Find("lblResInfo_" + key, true).FirstOrDefault() as Label;
                    if (lblResInfoRef != null)
                    {
                        decimal price = building.ResourcePrices.ContainsKey(key) ? building.ResourcePrices[key] : 0m;
                        lblResInfoRef.Text = $"{key}: {building.Warehouse[key]} szt. (Cena: {price:C}/szt.)";
                    }
                }
                return;
            }

            if (_inspectingBuilding != building)
            {
                _enteredSellQuantities.Clear();
                _inspectingBuilding = building;
            }

            pnlBuildingDetails.Controls.Clear();

            // Obramowanie ozdobne na górze
            Panel pnlTopLine = new Panel();
            pnlTopLine.Dock = DockStyle.Top;
            pnlTopLine.Height = 4;
            pnlTopLine.BackColor = Color.FromArgb(50, 150, 250);
            pnlBuildingDetails.Controls.Add(pnlTopLine);

            // Przycisk zamknięcia [X]
            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnClose.Size = new Size(25, 25);
            btnClose.Location = new Point(pnlBuildingDetails.Width - 35, 10);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.Gray;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => CloseBuildingDetails();
            pnlBuildingDetails.Controls.Add(btnClose);

            // Nazwa budynku
            Label lblName = new Label();
            lblName.Text = building.Name;
            lblName.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblName.ForeColor = Color.FromArgb(50, 150, 250);
            lblName.Location = new Point(20, 20);
            lblName.Size = new Size(330, 25);
            pnlBuildingDetails.Controls.Add(lblName);

            // Typ działalności i koszty
            Label lblSub = new Label();
            lblSub.Text = $"{building.ActivityType} | Utrzymanie: {building.MaintenanceCost:C}/doba";
            lblSub.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblSub.ForeColor = Color.LightGray;
            lblSub.Location = new Point(20, 48);
            lblSub.Size = new Size(350, 20);
            pnlBuildingDetails.Controls.Add(lblSub);

            // Pojemność magazynu
            int totalStock = building.GetTotalStock();
            Label lblCap = new Label();
            lblCap.Name = "lblCap";
            lblCap.Text = $"Pojemność magazynu: {totalStock} / {building.WarehouseCapacity} szt.";
            lblCap.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblCap.Location = new Point(20, 75);
            lblCap.Size = new Size(350, 20);
            pnlBuildingDetails.Controls.Add(lblCap);

            // Lista zasobów z możliwością sprzedaży
            Panel pnlResources = new Panel();
            pnlResources.Location = new Point(20, 100);
            pnlResources.Size = new Size(360, 140);
            pnlResources.AutoScroll = true;
            pnlBuildingDetails.Controls.Add(pnlResources);

            int yOffset = 0;
            foreach (var key in building.Warehouse.Keys.ToList())
            {
                int currentQty = building.Warehouse[key];
                decimal price = building.ResourcePrices.ContainsKey(key) ? building.ResourcePrices[key] : 0m;

                Label lblResInfo = new Label();
                lblResInfo.Name = "lblResInfo_" + key;
                lblResInfo.Text = $"{key}: {currentQty} szt. (Cena: {price:C}/szt.)";
                lblResInfo.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                lblResInfo.Location = new Point(0, yOffset);
                lblResInfo.Size = new Size(340, 20);
                pnlResources.Controls.Add(lblResInfo);

                TextBox txtSellQty = new TextBox();
                txtSellQty.Text = _enteredSellQuantities.ContainsKey(key) ? _enteredSellQuantities[key] : "0";
                txtSellQty.Font = new Font("Segoe UI", 9);
                txtSellQty.Location = new Point(0, yOffset + 22);
                txtSellQty.Size = new Size(80, 23);
                txtSellQty.BackColor = Color.FromArgb(45, 45, 45);
                txtSellQty.ForeColor = Color.White;
                txtSellQty.BorderStyle = BorderStyle.FixedSingle;
                
                string resourceName = key;
                txtSellQty.TextChanged += (s, e) =>
                {
                    _enteredSellQuantities[resourceName] = txtSellQty.Text;
                };
                pnlResources.Controls.Add(txtSellQty);

                Button btnAll = new Button();
                btnAll.Text = "ALL";
                btnAll.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                btnAll.Location = new Point(90, yOffset + 21);
                btnAll.Size = new Size(45, 24);
                btnAll.FlatStyle = FlatStyle.Flat;
                btnAll.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
                btnAll.BackColor = Color.FromArgb(35, 35, 35);
                btnAll.ForeColor = Color.FromArgb(50, 150, 250);
                btnAll.Cursor = Cursors.Hand;
                btnAll.Click += (s, e) => {
                    txtSellQty.Text = building.Warehouse[resourceName].ToString();
                };
                pnlResources.Controls.Add(btnAll);

                Button btnSell = new Button();
                btnSell.Text = "Sprzedaj";
                btnSell.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                btnSell.Location = new Point(145, yOffset + 21);
                btnSell.Size = new Size(70, 24);
                btnSell.FlatStyle = FlatStyle.Flat;
                btnSell.FlatAppearance.BorderSize = 0;
                btnSell.BackColor = Color.FromArgb(100, 220, 100);
                btnSell.ForeColor = Color.White;
                btnSell.Cursor = Cursors.Hand;
                
                btnSell.Click += (s, e) =>
                {
                    if (int.TryParse(txtSellQty.Text, out int qty) && qty > 0)
                    {
                        if (building.SellResource(resourceName, qty, _company!, _gameManager!.CurrentDay, _gameManager!.CurrentHour))
                        {
                            lblBottomStatus.Text = $"Sprzedano {qty} szt. {resourceName} za {(price * qty):C}!";
                            _enteredSellQuantities[resourceName] = "0"; // Reset po udanej sprzedaży
                            RefreshStats();
                            OpenBuildingDetails(building); // Odśwież panel
                            if (_hoveredTile.HasValue)
                            {
                                ShowTileInfo(_map!.GetTile(_hoveredTile.Value.X, _hoveredTile.Value.Y));
                            }
                        }
                        else
                        {
                            MessageBox.Show("Niepoprawna ilość do sprzedaży (za mało w magazynie)!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Wpisz poprawną liczbę dodatnią!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                pnlResources.Controls.Add(btnSell);

                yOffset += 55;
            }

            // Opcja Automatycznej Sprzedaży (CheckBox)
            CheckBox chkAutoSell = new CheckBox();
            chkAutoSell.Text = "Automatyczna sprzedaż (raz dziennie)";
            chkAutoSell.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            chkAutoSell.ForeColor = Color.FromArgb(100, 220, 100);
            chkAutoSell.Location = new Point(20, 250);
            chkAutoSell.Size = new Size(350, 22);
            chkAutoSell.Checked = building.AutoSell;
            chkAutoSell.Cursor = Cursors.Hand;
            chkAutoSell.CheckedChanged += (s, e) =>
            {
                building.AutoSell = chkAutoSell.Checked;
                lblBottomStatus.Text = $"Automatyczna sprzedaż dla '{building.Name}' została {(building.AutoSell ? "włączona" : "wyłączona")}.";
            };
            pnlBuildingDetails.Controls.Add(chkAutoSell);

            // Podpis informacyjny na dole
            Label lblEsc = new Label();
            lblEsc.Text = "Wskazówka: Naciśnij ESC, aby zamknąć to okno.";
            lblEsc.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblEsc.ForeColor = Color.Gray;
            lblEsc.Location = new Point(20, 315);
            lblEsc.Size = new Size(350, 15);
            pnlBuildingDetails.Controls.Add(lblEsc);

            CenterBuildingDetailsPanel();
            pnlBuildingDetails.Visible = true;
            pnlBuildingDetails.BringToFront();
            pnlBuildingDetails.Focus();
        }

        private void CloseBuildingDetails()
        {
            _inspectingBuilding = null;
            _enteredSellQuantities.Clear();
            if (pnlBuildingDetails != null)
            {
                pnlBuildingDetails.Visible = false;
            }
        }

        private void CenterBuildingDetailsPanel()
        {
            if (pnlBuildingDetails != null && pnlGameBoard != null)
            {
                pnlBuildingDetails.Location = new Point(
                    (pnlGameBoard.Width - pnlBuildingDetails.Width) / 2,
                    (pnlGameBoard.Height - pnlBuildingDetails.Height) / 2
                );
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                CloseBuildingDetails();
                CloseFinanceReport();
            }
        }

        private void ToggleFinanceReport()
        {
            if (pnlFinanceReport.Visible)
            {
                CloseFinanceReport();
            }
            else
            {
                CloseBuildingDetails(); // Zamknij szczegóły budynku jeśli były otwarte
                OpenFinanceReport();
            }
        }

        private void OpenFinanceReport()
        {
            CenterFinanceReportPanel();
            pnlFinanceReport.Visible = true;
            pnlFinanceReport.BringToFront();
            UpdateFinanceReportView();
            pnlFinanceReport.Focus();
        }

        private void CloseFinanceReport()
        {
            if (pnlFinanceReport != null)
            {
                pnlFinanceReport.Visible = false;
            }
        }

        private void CenterFinanceReportPanel()
        {
            if (pnlFinanceReport != null && pnlGameBoard != null)
            {
                pnlFinanceReport.Location = new Point(
                    (pnlGameBoard.Width - pnlFinanceReport.Width) / 2,
                    (pnlGameBoard.Height - pnlFinanceReport.Height) / 2
                );
            }
        }

        private void UpdateFinanceReportView()
        {
            if (_company == null || _gameManager == null) return;

            // --- OBLICZENIE PRZEPŁYWÓW (CASH FLOW) ---
            int curDay = _gameManager.CurrentDay;
            int curHour = _gameManager.CurrentHour;

            // Filtrujemy transakcje z użyciem Ledger:
            var dailyTrans = _company.Ledger.GetTransactionsForPeriod(curDay, curHour, 24);
            var monthlyTrans = _company.Ledger.GetTransactionsForPeriod(curDay, curHour, 720);
            var yearlyTrans = _company.Ledger.GetTransactionsForPeriod(curDay, curHour, 8760);

            // Przepływy pomocnicze
            decimal dSales = _company.Ledger.GetSumByCategory(dailyTrans, "Sprzedaż");
            decimal dMaint = _company.Ledger.GetSumByCategory(dailyTrans, "Utrzymanie");
            decimal dBuild = _company.Ledger.GetSumByCategory(dailyTrans, "Budowa");
            decimal dNet = dSales + dMaint + dBuild;

            decimal mSales = _company.Ledger.GetSumByCategory(monthlyTrans, "Sprzedaż");
            decimal mMaint = _company.Ledger.GetSumByCategory(monthlyTrans, "Utrzymanie");
            decimal mBuild = _company.Ledger.GetSumByCategory(monthlyTrans, "Budowa");
            decimal mNet = mSales + mMaint + mBuild;

            decimal ySales = _company.Ledger.GetSumByCategory(yearlyTrans, "Sprzedaż");
            decimal yMaint = _company.Ledger.GetSumByCategory(yearlyTrans, "Utrzymanie");
            decimal yBuild = _company.Ledger.GetSumByCategory(yearlyTrans, "Budowa");
            decimal yNet = ySales + yMaint + yBuild;

            int activeFarms = _company.Buildings.Count(b => b is Farm);
            int activeMines = _company.Buildings.Count(b => b is CoalMine);
            decimal totalMaintCost = _company.Buildings.Sum(b => b.MaintenanceCost);

            // Obliczenie przewidywanego maksymalnego zysku dobowego
            // Farma: 2 Milk * $50 + 1 Meat * $150 = $250. Cost = $150. Profit = $100.
            // Kopalnia: 4 Coal * $100 = $400. Cost = $250. Profit = $150.
            decimal maxDailyRevenue = _company.Buildings.Sum(b => {
                if (b is Farm) return 250m;
                if (b is CoalMine) return 400m;
                return 0m;
            });
            decimal maxDailyProfit = maxDailyRevenue - totalMaintCost;

            string rentText = $"Aktywne obiekty:\n" +
                              $"  - Farmy krów: {activeFarms}\n" +
                              $"  - Kopalnie węgla: {activeMines}\n\n" +
                              $"Saldo gotówkowe:\n" +
                              $"  {_company.Balance:C}\n\n" +
                              $"Dobowe koszty stałe:\n" +
                              $"  -{totalMaintCost:C}/doba\n\n" +
                              $"Max. potencjał zysku:\n" +
                              $"  +{maxDailyProfit:C}/doba\n\n" +
                              $"Status korporacji:\n" +
                              $"  {(maxDailyProfit > 0 ? "Rentowna" : (totalMaintCost == 0 ? "Brak kosztów" : "Deficytowa"))}";

            // Weryfikacja czy panel jest już zainicjalizowany w celu optymalizacji migotania (Fast Update)
            var lblRentDetailsRef = pnlFinanceReport.Controls.Find("lblRentDetails", true).FirstOrDefault() as Label;
            if (lblRentDetailsRef != null)
            {
                lblRentDetailsRef.Text = rentText;

                UpdateRowLabels("Doba", dSales, dMaint, dBuild, dNet);
                UpdateRowLabels("Miesiac", mSales, mMaint, mBuild, mNet);
                UpdateRowLabels("Rok", ySales, yMaint, yBuild, yNet);

                return;
            }

            pnlFinanceReport.Controls.Clear();

            // Obramowanie ozdobne na górze
            Panel pnlTopLine = new Panel();
            pnlTopLine.Dock = DockStyle.Top;
            pnlTopLine.Height = 4;
            pnlTopLine.BackColor = Color.FromArgb(50, 150, 250);
            pnlFinanceReport.Controls.Add(pnlTopLine);

            // Przycisk zamknięcia [X]
            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnClose.Size = new Size(25, 25);
            btnClose.Location = new Point(pnlFinanceReport.Width - 35, 10);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.Gray;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => CloseFinanceReport();
            pnlFinanceReport.Controls.Add(btnClose);

            // Tytuł
            Label lblTitle = new Label();
            lblTitle.Text = "RAPORT FINANSOWY KORPORACJI";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(20, 20);
            lblTitle.Size = new Size(400, 25);
            pnlFinanceReport.Controls.Add(lblTitle);

            // Sekcja lewa: Przepływy
            Label lblFlowsTitle = new Label();
            lblFlowsTitle.Text = "PRZEPŁYWY FINANSOWE (CASH FLOW)";
            lblFlowsTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblFlowsTitle.ForeColor = Color.White;
            lblFlowsTitle.Location = new Point(20, 65);
            lblFlowsTitle.Size = new Size(330, 20);
            pnlFinanceReport.Controls.Add(lblFlowsTitle);

            // Prosta tabela
            Panel pnlTable = new Panel();
            pnlTable.Location = new Point(20, 90);
            pnlTable.Size = new Size(330, 310);
            pnlTable.BackColor = Color.FromArgb(25, 25, 25);
            pnlTable.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Controls.Add(pnlTable);

            int y = 10;
            // Dobowe
            AddTableRow(pnlTable, "DOBOWE (24h)", dSales, dMaint, dBuild, dNet, y, "Doba");
            y += 95;
            // Miesięczne
            AddTableRow(pnlTable, "MIESIĘCZNE (30d)", mSales, mMaint, mBuild, mNet, y, "Miesiac");
            y += 95;
            // Roczne
            AddTableRow(pnlTable, "ROCZNE (365d)", ySales, yMaint, yBuild, yNet, y, "Rok");

            // Sekcja prawa: Szczegóły rentowności
            Label lblRentTitle = new Label();
            lblRentTitle.Text = "RENTOWNOŚĆ I ZASOBY";
            lblRentTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblRentTitle.ForeColor = Color.White;
            lblRentTitle.Location = new Point(370, 65);
            lblRentTitle.Size = new Size(200, 20);
            pnlFinanceReport.Controls.Add(lblRentTitle);

            Panel pnlRentInfo = new Panel();
            pnlRentInfo.Location = new Point(370, 90);
            pnlRentInfo.Size = new Size(210, 310);
            pnlRentInfo.BackColor = Color.FromArgb(25, 25, 25);
            pnlRentInfo.BorderStyle = BorderStyle.FixedSingle;
            pnlRentInfo.Padding = new Padding(10);
            pnlFinanceReport.Controls.Add(pnlRentInfo);

            Label lblRentDetails = new Label();
            lblRentDetails.Name = "lblRentDetails";
            lblRentDetails.Text = rentText;
            lblRentDetails.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblRentDetails.ForeColor = Color.FromArgb(200, 200, 200);
            lblRentDetails.Location = new Point(10, 10);
            lblRentDetails.Size = new Size(190, 290);
            pnlRentInfo.Controls.Add(lblRentDetails);

            // Stopka ESC
            Label lblFooter = new Label();
            lblFooter.Text = "Wskazówka: Naciśnij ESC, aby zamknąć to okno.";
            lblFooter.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblFooter.ForeColor = Color.Gray;
            lblFooter.Location = new Point(20, 445);
            lblFooter.Size = new Size(400, 15);
            pnlFinanceReport.Controls.Add(lblFooter);
        }

        private void UpdateRowLabels(string suffix, decimal sales, decimal maint, decimal build, decimal net)
        {
            var lblDetailsRef = pnlFinanceReport.Controls.Find("lblDetails_" + suffix, true).FirstOrDefault() as Label;
            if (lblDetailsRef != null)
            {
                lblDetailsRef.Text = $"Przychody:  +{sales:C}\n" +
                                     $"Utrzymanie:  {maint:C}\n" +
                                     $"Inwestycje:  {build:C}";
            }

            var lblNetRef = pnlFinanceReport.Controls.Find("lblNet_" + suffix, true).FirstOrDefault() as Label;
            if (lblNetRef != null)
            {
                lblNetRef.Text = $"NETTO:\n{net:C}";
                lblNetRef.ForeColor = net >= 0 ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            }
        }

        private void AddTableRow(Panel container, string title, decimal sales, decimal maint, decimal build, decimal net, int yPosition, string suffix)
        {
            Label lblRowTitle = new Label();
            lblRowTitle.Text = title;
            lblRowTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblRowTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblRowTitle.Location = new Point(10, yPosition);
            lblRowTitle.Size = new Size(200, 15);
            container.Controls.Add(lblRowTitle);

            Label lblDetails = new Label();
            lblDetails.Name = "lblDetails_" + suffix;
            lblDetails.Text = $"Przychody:  +{sales:C}\n" +
                              $"Utrzymanie:  {maint:C}\n" +
                              $"Inwestycje:  {build:C}";
            lblDetails.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            lblDetails.ForeColor = Color.LightGray;
            lblDetails.Location = new Point(15, yPosition + 18);
            lblDetails.Size = new Size(180, 50);
            container.Controls.Add(lblDetails);

            Label lblNet = new Label();
            lblNet.Name = "lblNet_" + suffix;
            lblNet.Text = $"NETTO:\n{net:C}";
            lblNet.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblNet.ForeColor = net >= 0 ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            lblNet.Location = new Point(210, yPosition + 18);
            lblNet.Size = new Size(110, 45);
            lblNet.TextAlign = ContentAlignment.TopRight;
            container.Controls.Add(lblNet);

            // Linia separatora
            Panel pnlSep = new Panel();
            pnlSep.Location = new Point(10, yPosition + 80);
            pnlSep.Size = new Size(310, 1);
            pnlSep.BackColor = Color.FromArgb(50, 50, 50);
            container.Controls.Add(pnlSep);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gameTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
