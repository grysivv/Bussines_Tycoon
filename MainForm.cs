using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Financials;
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

            // Get P&L and Balance Sheet snapshots from the new engine
            var pnl = _company.Engine.CalculateCurrentPnL();
            var bs = _company.Engine.CalculateCurrentBalanceSheet();

            // Format strings for P&L and Balance Sheet details with padded alignment
            string pnlText = $"{ "Przychody ze sprzedaży:",-26 } {pnl.Revenue,15:C}\n" +
                             $"{ "Koszty surowców (COGS):",-26 } {pnl.RawMaterials,15:C}\n" +
                             $"{ "Logistyka i marketing:",-26 } {(pnl.Logistics + pnl.Marketing),15:C}\n" +
                             $"{ "Wynagrodzenia i płace:",-26 } {pnl.Salaries,15:C}\n" +
                             $"---------------------------------------------\n" +
                             $"{ "EBITDA:",-26 } {pnl.EBITDA,15:C}\n" +
                             $"{ "Amortyzacja (niegotówk.):",-26 } {pnl.Depreciation,15:C}\n" +
                             $"{ "EBIT (Zysk operacyjny):",-26 } {pnl.EBIT,15:C}\n" +
                             $"{ "Podatek dochodowy (CIT):",-26 } {pnl.CorporateTax,15:C}\n";

            string netIncomeText = $"{ "ZYSK NETTO:",-26 } {pnl.NetIncome,15:C}";

            string bsText = $"AKTYWA (Assets)\n" +
                            $"{ "  Gotówka:",-26 } {bs.Cash,15:C}\n" +
                            $"{ "  Zapasy (magazyn):",-26 } {bs.InventoryValue,15:C}\n" +
                            $"{ "  Nieruchomości (netto):",-26 } {bs.PropertyBookValue,15:C}\n" +
                            $"  -------------------------------------------\n" +
                            $"{ "  SUMA AKTYWÓW:",-26 } {bs.TotalAssets,15:C}\n\n" +
                            $"PASYWA (Liabilities & Equity)\n" +
                            $"{ "  Kredyty bankowe:",-26 } {bs.Loans,15:C}\n" +
                            $"{ "  Kapitał akcyjny:",-26 } {bs.ShareCapital,15:C}\n" +
                            $"{ "  Zyski zatrzymane:",-26 } {bs.RetainedEarnings,15:C}\n" +
                            $"  -------------------------------------------\n" +
                            $"{ "  SUMA PASYWÓW:",-26 } {bs.TotalLiabilitiesAndEquity,15:C}";

            string balanceStatusText = $"Bilans zrównoważony: {(bs.IsBalanced ? "TAK" : "NIE")}";

            // Wyszukiwanie kontrolek dla optymalizacji migotania (Fast Update)
            var lblPnLDetailsRef = pnlFinanceReport.Controls.Find("lblPnLDetails", true).FirstOrDefault() as Label;
            if (lblPnLDetailsRef != null)
            {
                lblPnLDetailsRef.Text = pnlText;

                var lblNetIncomeRef = pnlFinanceReport.Controls.Find("lblNetIncome", true).FirstOrDefault() as Label;
                if (lblNetIncomeRef != null)
                {
                    lblNetIncomeRef.Text = netIncomeText;
                    lblNetIncomeRef.ForeColor = pnl.NetIncome >= 0 ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
                }

                var lblBSRefFound = pnlFinanceReport.Controls.Find("lblBSRef", true).FirstOrDefault() as Label;
                if (lblBSRefFound != null)
                {
                    lblBSRefFound.Text = bsText;
                }

                var lblBalanceStatusRef = pnlFinanceReport.Controls.Find("lblBalanceStatus", true).FirstOrDefault() as Label;
                if (lblBalanceStatusRef != null)
                {
                    lblBalanceStatusRef.Text = balanceStatusText;
                    lblBalanceStatusRef.ForeColor = bs.IsBalanced ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
                }

                var lblMonthIndexRef = pnlFinanceReport.Controls.Find("lblMonthIndex", true).FirstOrDefault() as Label;
                if (lblMonthIndexRef != null)
                {
                    lblMonthIndexRef.Text = $"Miesiąc gry: {_company.Engine.CurrentMonthIndex} | Dzień: {_gameManager.CurrentDay}";
                }

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
            lblTitle.Size = new Size(320, 25);
            pnlFinanceReport.Controls.Add(lblTitle);

            // Podtytuł z informacją o miesiącu
            Label lblMonthIndex = new Label();
            lblMonthIndex.Name = "lblMonthIndex";
            lblMonthIndex.Text = $"Miesiąc gry: {_company.Engine.CurrentMonthIndex} | Dzień: {_gameManager.CurrentDay}";
            lblMonthIndex.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblMonthIndex.ForeColor = Color.DarkGray;
            lblMonthIndex.Location = new Point(20, 45);
            lblMonthIndex.Size = new Size(300, 15);
            pnlFinanceReport.Controls.Add(lblMonthIndex);

            // Sekcja lewa: P&L
            Panel pnlPnL = new Panel();
            pnlPnL.Location = new Point(20, 75);
            pnlPnL.Size = new Size(330, 340);
            pnlPnL.BackColor = Color.FromArgb(25, 25, 25);
            pnlPnL.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Controls.Add(pnlPnL);

            Label lblPnLTitle = new Label();
            lblPnLTitle.Text = "RACHUNEK ZYSKÓW I STRAT (P&L)";
            lblPnLTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblPnLTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblPnLTitle.Location = new Point(10, 10);
            lblPnLTitle.Size = new Size(310, 15);
            pnlPnL.Controls.Add(lblPnLTitle);

            Label lblPnLDetails = new Label();
            lblPnLDetails.Name = "lblPnLDetails";
            lblPnLDetails.Text = pnlText;
            lblPnLDetails.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            lblPnLDetails.ForeColor = Color.LightGray;
            lblPnLDetails.Location = new Point(10, 35);
            lblPnLDetails.Size = new Size(310, 240);
            pnlPnL.Controls.Add(lblPnLDetails);

            Label lblNetIncome = new Label();
            lblNetIncome.Name = "lblNetIncome";
            lblNetIncome.Text = netIncomeText;
            lblNetIncome.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblNetIncome.ForeColor = pnl.NetIncome >= 0 ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            lblNetIncome.Location = new Point(10, 295);
            lblNetIncome.Size = new Size(310, 20);
            pnlPnL.Controls.Add(lblNetIncome);

            // Sekcja prawa: Balance Sheet
            Panel pnlBS = new Panel();
            pnlBS.Location = new Point(370, 75);
            pnlBS.Size = new Size(330, 340);
            pnlBS.BackColor = Color.FromArgb(25, 25, 25);
            pnlBS.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Controls.Add(pnlBS);

            Label lblBSTitle = new Label();
            lblBSTitle.Text = "BILANS (BALANCE SHEET)";
            lblBSTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblBSTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblBSTitle.Location = new Point(10, 10);
            lblBSTitle.Size = new Size(310, 15);
            pnlBS.Controls.Add(lblBSTitle);

            Label lblBSRef = new Label();
            lblBSRef.Name = "lblBSRef";
            lblBSRef.Text = bsText;
            lblBSRef.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            lblBSRef.ForeColor = Color.LightGray;
            lblBSRef.Location = new Point(10, 35);
            lblBSRef.Size = new Size(310, 240);
            pnlBS.Controls.Add(lblBSRef);

            Label lblBalanceStatus = new Label();
            lblBalanceStatus.Name = "lblBalanceStatus";
            lblBalanceStatus.Text = balanceStatusText;
            lblBalanceStatus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblBalanceStatus.ForeColor = bs.IsBalanced ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            lblBalanceStatus.Location = new Point(10, 295);
            lblBalanceStatus.Size = new Size(310, 20);
            pnlBS.Controls.Add(lblBalanceStatus);

            // Stopka ESC
            Label lblFooter = new Label();
            lblFooter.Text = "Wskazówka: Naciśnij ESC, aby zamknąć to okno.";
            lblFooter.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblFooter.ForeColor = Color.Gray;
            lblFooter.Location = new Point(20, 445);
            lblFooter.Size = new Size(400, 15);
            pnlFinanceReport.Controls.Add(lblFooter);
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
