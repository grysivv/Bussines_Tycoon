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
        private Panel pnlNewGameSettings = null!;
        private Panel pnlLoadGameMenu = null!;
        private Panel pnlSaveGameOverlay = null!;
        private Panel pnlEscapeMenu = null!;

        public enum MenuState
        {
            MainMenu,
            NewGameSettings,
            LoadGameMenu,
            Playing
        }

        private MenuState _currentMenuState = MenuState.MainMenu;

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

        private enum SelectedBlueprint { None, Farm, CoalMine, FoodWarehouse, MiningWarehouse }
        private SelectedBlueprint _selectedBlueprint = SelectedBlueprint.None;
        private Button btnBuildFarm = null!;
        private Button btnBuildCoalMine = null!;
        private Button btnBuildFoodWarehouse = null!;
        private Button btnBuildMiningWarehouse = null!;
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

            Button btnNewGame = new Button();
            btnNewGame.Text = "NOWA ROZGRYWKA";
            btnNewGame.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnNewGame.Location = new Point(30, 110);
            btnNewGame.Size = new Size(340, 45);
            btnNewGame.FlatStyle = FlatStyle.Flat;
            btnNewGame.FlatAppearance.BorderSize = 0;
            btnNewGame.BackColor = Color.FromArgb(50, 150, 250);
            btnNewGame.ForeColor = Color.White;
            btnNewGame.Cursor = Cursors.Hand;
            btnNewGame.Click += (s, e) => ChangeMenuState(MenuState.NewGameSettings);
            pnlStartCenter.Controls.Add(btnNewGame);

            Button btnLoadGameMenu = new Button();
            btnLoadGameMenu.Text = "WCZYTAJ GRĘ";
            btnLoadGameMenu.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnLoadGameMenu.Location = new Point(30, 170);
            btnLoadGameMenu.Size = new Size(340, 45);
            btnLoadGameMenu.FlatStyle = FlatStyle.Flat;
            btnLoadGameMenu.FlatAppearance.BorderSize = 1;
            btnLoadGameMenu.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnLoadGameMenu.BackColor = Color.FromArgb(30, 30, 30);
            btnLoadGameMenu.ForeColor = Color.FromArgb(50, 150, 250);
            btnLoadGameMenu.Cursor = Cursors.Hand;
            btnLoadGameMenu.Click += (s, e) => ChangeMenuState(MenuState.LoadGameMenu);
            pnlStartCenter.Controls.Add(btnLoadGameMenu);

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

            Button btnNavSave = new Button();
            btnNavSave.Text = "ZAPISZ GRĘ";
            btnNavSave.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnNavSave.Size = new Size(100, 30);
            btnNavSave.Location = new Point(pnlTopNav.Width - 230, 8);
            btnNavSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNavSave.FlatStyle = FlatStyle.Flat;
            btnNavSave.FlatAppearance.BorderSize = 1;
            btnNavSave.FlatAppearance.BorderColor = Color.FromArgb(100, 220, 100);
            btnNavSave.BackColor = Color.FromArgb(35, 35, 35);
            btnNavSave.ForeColor = Color.FromArgb(100, 220, 100);
            btnNavSave.Cursor = Cursors.Hand;
            btnNavSave.Click += (s, e) =>
            {
                if (_company != null)
                {
                    _gameTimer.Stop();
                    var txtSave = pnlSaveGameOverlay.Controls.Find("txtSaveName", true).FirstOrDefault() as TextBox;
                    if (txtSave != null)
                    {
                        txtSave.Text = $"{_company.Name}_Dzień{_gameManager?.CurrentDay ?? 1}";
                    }
                    CenterSaveGameOverlayPanel();
                    pnlSaveGameOverlay.Visible = true;
                    pnlSaveGameOverlay.BringToFront();
                    pnlSaveGameOverlay.Focus();
                }
            };
            pnlTopNav.Controls.Add(btnNavSave);

            Button btnNavLoad = new Button();
            btnNavLoad.Text = "WCZYTAJ GRĘ";
            btnNavLoad.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnNavLoad.Size = new Size(100, 30);
            btnNavLoad.Location = new Point(pnlTopNav.Width - 120, 8);
            btnNavLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNavLoad.FlatStyle = FlatStyle.Flat;
            btnNavLoad.FlatAppearance.BorderSize = 1;
            btnNavLoad.FlatAppearance.BorderColor = Color.FromArgb(240, 180, 50);
            btnNavLoad.BackColor = Color.FromArgb(35, 35, 35);
            btnNavLoad.ForeColor = Color.FromArgb(240, 180, 50);
            btnNavLoad.Cursor = Cursors.Hand;
            btnNavLoad.Click += (s, e) =>
            {
                ChangeMenuState(MenuState.LoadGameMenu);
            };
            pnlTopNav.Controls.Add(btnNavLoad);

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

            btnBuildFoodWarehouse = new Button();
            btnBuildFoodWarehouse.Text = "Magazyn Żywności\n(Koszt: $8k)";
            btnBuildFoodWarehouse.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildFoodWarehouse.Location = new Point(15, 180);
            btnBuildFoodWarehouse.Size = new Size(160, 50);
            btnBuildFoodWarehouse.FlatStyle = FlatStyle.Flat;
            btnBuildFoodWarehouse.FlatAppearance.BorderSize = 1;
            btnBuildFoodWarehouse.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildFoodWarehouse.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildFoodWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildFoodWarehouse.Cursor = Cursors.Hand;
            btnBuildFoodWarehouse.Click += (s, e) => SelectBlueprint(SelectedBlueprint.FoodWarehouse, btnBuildFoodWarehouse);
            pnlRight.Controls.Add(btnBuildFoodWarehouse);

            btnBuildMiningWarehouse = new Button();
            btnBuildMiningWarehouse.Text = "Magazyn Kopalniany\n(Koszt: $12k)";
            btnBuildMiningWarehouse.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildMiningWarehouse.Location = new Point(15, 240);
            btnBuildMiningWarehouse.Size = new Size(160, 50);
            btnBuildMiningWarehouse.FlatStyle = FlatStyle.Flat;
            btnBuildMiningWarehouse.FlatAppearance.BorderSize = 1;
            btnBuildMiningWarehouse.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildMiningWarehouse.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildMiningWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildMiningWarehouse.Cursor = Cursors.Hand;
            btnBuildMiningWarehouse.Click += (s, e) => SelectBlueprint(SelectedBlueprint.MiningWarehouse, btnBuildMiningWarehouse);
            pnlRight.Controls.Add(btnBuildMiningWarehouse);

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
            pnlFinanceReport.Size = new Size(800, 480);
            pnlFinanceReport.BackColor = Color.FromArgb(30, 30, 30);
            pnlFinanceReport.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Visible = false;
            pnlGameBoard.Controls.Add(pnlFinanceReport);
            pnlFinanceReport.BringToFront();

            // Zapewnienie automatycznego centrowania przy zmianie rozmiaru
            pnlGameBoard.SizeChanged += (s, e) => {
                CenterBuildingDetailsPanel();
                CenterFinanceReportPanel();
                CenterSaveGameOverlayPanel();
                CenterEscapeMenuPanel();
            };

            // Rejestracja zdarzeń mapy (jednorazowo)
            mapControl.OnTileSelected += OnTileSelectedOnMap;
            mapControl.OnTileHovered += OnTileHoveredOnMap;

            // Inicjalizacja nowych paneli nakładkowych i menu
            InitializeNewGameSettingsPanel();
            InitializeLoadGameMenuPanel();
            InitializeSaveGameOverlayPanel();
            InitializeEscapeMenu();

            // Automatyczne pozycjonowanie paneli przy starcie
            pnlStartScreen.SizeChanged += (s, e) =>
            {
                pnlStartCenter.Location = new Point(
                    (pnlStartScreen.Width - pnlStartCenter.Width) / 2,
                    (pnlStartScreen.Height - pnlStartCenter.Height) / 2
                );
                pnlNewGameSettings.Location = new Point(
                    (pnlStartScreen.Width - pnlNewGameSettings.Width) / 2,
                    (pnlStartScreen.Height - pnlNewGameSettings.Height) / 2
                );
                pnlLoadGameMenu.Location = new Point(
                    (pnlStartScreen.Width - pnlLoadGameMenu.Width) / 2,
                    (pnlStartScreen.Height - pnlLoadGameMenu.Height) / 2
                );
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

        private void StartGameWithSettings(string companyName, GameGenerationSettings settings)
        {
            // Inicjalizacja modeli silnika gry (Core)
            _company = new Company(companyName, settings.StartingCash);
            _company.Engine.TaxRate = settings.GlobalCorporateTax;
            _map = new Map(10, 10);
            _gameManager = new GameManager(_company, _map);

            // Subskrypcja zdarzeń
            _gameManager.OnTickPerformed += OnTickPerformed;

            // Inicjalizacja danych mapy w kontrolce
            mapControl.Initialize(_map, _gameManager);

            // Przejście scen: Ukrycie startowego, wyświetlenie gry
            ChangeMenuState(MenuState.Playing);

            // Domyślne uruchomienie gry z prędkością 1x (tick co 1 sekunda)
            SetGameSpeed(1000, btnSpeed1x);

            RefreshStats();
        }

        private void ToggleBuildMode(bool activate)
        {
            mapControl.SetBuildMode(activate);

            if (mapControl.GetBuildMode())
            {
                string name = _selectedBlueprint == SelectedBlueprint.Farm ? "farmę krów ($10 000)" : 
                              (_selectedBlueprint == SelectedBlueprint.CoalMine ? "kopalnię węgla ($15 000)" : 
                              (_selectedBlueprint == SelectedBlueprint.FoodWarehouse ? "magazyn żywności ($8 000)" : "magazyn kopalniany ($12 000)"));
                lblBottomStatus.Text = $"TRYB BUDOWANIA AKTYWNY: Kliknij na wolny zielony kafel trawy, aby wybudować {name}.";
            }
            else
            {
                _selectedBlueprint = SelectedBlueprint.None;
                btnBuildFarm.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFarm.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildCoalMine.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildCoalMine.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildFoodWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFoodWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildMiningWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildMiningWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
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
                btnBuildFoodWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFoodWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildMiningWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildMiningWarehouse.ForeColor = Color.FromArgb(50, 150, 250);

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
                    else if (_selectedBlueprint == SelectedBlueprint.FoodWarehouse)
                    {
                        buildingName = $"Magazyn Żywności #{_company.Buildings.Count + 1}";
                        building = new WarehouseBuilding(buildingName, ResourceCategory.Food);
                    }
                    else if (_selectedBlueprint == SelectedBlueprint.MiningWarehouse)
                    {
                        buildingName = $"Magazyn Kopalniany #{_company.Buildings.Count + 1}";
                        building = new WarehouseBuilding(buildingName, ResourceCategory.Mining);
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
                // 1. Jeśli otwarte są panele szczegółowe lub modalne, zamknij je w pierwszej kolejności
                if (pnlBuildingDetails.Visible || pnlFinanceReport.Visible || pnlSaveGameOverlay.Visible || pnlEscapeMenu.Visible)
                {
                    if (pnlEscapeMenu.Visible)
                    {
                        ToggleEscapeMenu();
                        return;
                    }
                    
                    CloseBuildingDetails();
                    CloseFinanceReport();
                    pnlSaveGameOverlay.Visible = false;
                    
                    if (_activeSpeedButton != btnSpeedPause && _currentMenuState == MenuState.Playing)
                    {
                        _gameTimer.Start();
                    }
                    return;
                }

                // 2. Jeśli jesteśmy w samej rozgrywce, wywołaj Escape Menu (Pauza)
                if (_currentMenuState == MenuState.Playing)
                {
                    ToggleEscapeMenu();
                }
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
            string pnlText = $"{ "Przychody ze sprzedaży:",-28 } {pnl.Revenue,16:C}\n" +
                             $"{ "Koszty surowców (COGS):",-28 } {pnl.RawMaterials,16:C}\n" +
                             $"{ "Logistyka i marketing:",-28 } {(pnl.Logistics + pnl.Marketing),16:C}\n" +
                             $"{ "Wynagrodzenia i płace:",-28 } {pnl.Salaries,16:C}\n" +
                             $"------------------------------------------------\n" +
                             $"{ "EBITDA:",-28 } {pnl.EBITDA,16:C}\n" +
                             $"{ "Amortyzacja (niegotówk.):",-28 } {pnl.Depreciation,16:C}\n" +
                             $"{ "EBIT (Zysk operacyjny):",-28 } {pnl.EBIT,16:C}\n" +
                             $"{ "Podatek dochodowy (CIT):",-28 } {pnl.CorporateTax,16:C}\n";

            string netIncomeText = $"{ "ZYSK NETTO:",-28 } {pnl.NetIncome,16:C}";

            string bsText = $"AKTYWA (Assets)\n" +
                            $"{ "  Gotówka:",-28 } {bs.Cash,16:C}\n" +
                            $"{ "  Zapasy (magazyn):",-28 } {bs.InventoryValue,16:C}\n" +
                            $"{ "  Nieruchomości (netto):",-28 } {bs.PropertyBookValue,16:C}\n" +
                            $"  --------------------------------------------\n" +
                            $"{ "  SUMA AKTYWÓW:",-28 } {bs.TotalAssets,16:C}\n\n" +
                            $"PASYWA (Liabilities & Equity)\n" +
                            $"{ "  Kredyty bankowe:",-28 } {bs.Loans,16:C}\n" +
                            $"{ "  Kapitał akcyjny:",-28 } {bs.ShareCapital,16:C}\n" +
                            $"{ "  Zyski zatrzymane:",-28 } {bs.RetainedEarnings,16:C}\n" +
                            $"  --------------------------------------------\n" +
                            $"{ "  SUMA PASYWÓW:",-28 } {bs.TotalLiabilitiesAndEquity,16:C}";

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
            pnlPnL.Size = new Size(360, 340);
            pnlPnL.BackColor = Color.FromArgb(25, 25, 25);
            pnlPnL.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Controls.Add(pnlPnL);

            Label lblPnLTitle = new Label();
            lblPnLTitle.Text = "RACHUNEK ZYSKÓW I STRAT (P&L)";
            lblPnLTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblPnLTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblPnLTitle.Location = new Point(10, 10);
            lblPnLTitle.Size = new Size(340, 15);
            pnlPnL.Controls.Add(lblPnLTitle);

            Label lblPnLDetails = new Label();
            lblPnLDetails.Name = "lblPnLDetails";
            lblPnLDetails.Text = pnlText;
            lblPnLDetails.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            lblPnLDetails.ForeColor = Color.LightGray;
            lblPnLDetails.Location = new Point(10, 35);
            lblPnLDetails.Size = new Size(340, 240);
            pnlPnL.Controls.Add(lblPnLDetails);

            Label lblNetIncome = new Label();
            lblNetIncome.Name = "lblNetIncome";
            lblNetIncome.Text = netIncomeText;
            lblNetIncome.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblNetIncome.ForeColor = pnl.NetIncome >= 0 ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            lblNetIncome.Location = new Point(10, 295);
            lblNetIncome.Size = new Size(340, 20);
            pnlPnL.Controls.Add(lblNetIncome);

            // Sekcja prawa: Balance Sheet
            Panel pnlBS = new Panel();
            pnlBS.Location = new Point(410, 75);
            pnlBS.Size = new Size(360, 340);
            pnlBS.BackColor = Color.FromArgb(25, 25, 25);
            pnlBS.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Controls.Add(pnlBS);

            Label lblBSTitle = new Label();
            lblBSTitle.Text = "BILANS (BALANCE SHEET)";
            lblBSTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblBSTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblBSTitle.Location = new Point(10, 10);
            lblBSTitle.Size = new Size(340, 15);
            pnlBS.Controls.Add(lblBSTitle);

            Label lblBSRef = new Label();
            lblBSRef.Name = "lblBSRef";
            lblBSRef.Text = bsText;
            lblBSRef.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
            lblBSRef.ForeColor = Color.LightGray;
            lblBSRef.Location = new Point(10, 35);
            lblBSRef.Size = new Size(340, 240);
            pnlBS.Controls.Add(lblBSRef);

            Label lblBalanceStatus = new Label();
            lblBalanceStatus.Name = "lblBalanceStatus";
            lblBalanceStatus.Text = balanceStatusText;
            lblBalanceStatus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblBalanceStatus.ForeColor = bs.IsBalanced ? Color.FromArgb(100, 220, 100) : Color.LightCoral;
            lblBalanceStatus.Location = new Point(10, 295);
            lblBalanceStatus.Size = new Size(340, 20);
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

        private void InitializeNewGameSettingsPanel()
        {
            pnlNewGameSettings = new Panel();
            pnlNewGameSettings.Size = new Size(650, 480);
            pnlNewGameSettings.BackColor = Color.FromArgb(30, 30, 30);
            pnlNewGameSettings.Visible = false;
            pnlStartScreen.Controls.Add(pnlNewGameSettings);

            // Obramowanie ozdobne na górze
            Panel pnlBorder = new Panel();
            pnlBorder.Dock = DockStyle.Top;
            pnlBorder.Height = 4;
            pnlBorder.BackColor = Color.FromArgb(50, 150, 250);
            pnlNewGameSettings.Controls.Add(pnlBorder);

            // Przycisk wstecz
            Button btnBack = new Button();
            btnBack.Text = "< Powrót";
            btnBack.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBack.Location = new Point(20, 20);
            btnBack.Size = new Size(90, 30);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnBack.ForeColor = Color.LightGray;
            btnBack.Cursor = Cursors.Hand;
            btnBack.Click += (s, e) => ChangeMenuState(MenuState.MainMenu);
            pnlNewGameSettings.Controls.Add(btnBack);

            // Tytuł
            Label lblTitle = new Label();
            lblTitle.Text = "USTAWIENIA NOWEJ ROZGRYWKI";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(120, 20);
            lblTitle.Size = new Size(410, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlNewGameSettings.Controls.Add(lblTitle);

            // KOLUMNA LEWA: Ustawienia Aktywne i Mapy
            // Nazwa firmy
            Label lblName = new Label();
            lblName.Text = "Nazwa Korporacji:";
            lblName.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblName.Location = new Point(40, 75);
            lblName.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblName);

            TextBox txtCompanyNameSettings = new TextBox();
            txtCompanyNameSettings.Name = "txtCompanyNameSettings";
            txtCompanyNameSettings.Text = "Moja Korporacja";
            txtCompanyNameSettings.Font = new Font("Segoe UI", 10);
            txtCompanyNameSettings.Location = new Point(40, 95);
            txtCompanyNameSettings.Size = new Size(250, 25);
            txtCompanyNameSettings.BackColor = Color.FromArgb(45, 45, 45);
            txtCompanyNameSettings.ForeColor = Color.White;
            txtCompanyNameSettings.BorderStyle = BorderStyle.FixedSingle;
            pnlNewGameSettings.Controls.Add(txtCompanyNameSettings);

            // Kapitał Startowy (Aktywny)
            Label lblCash = new Label();
            lblCash.Text = "Kapitał Startowy ($):";
            lblCash.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblCash.Location = new Point(40, 135);
            lblCash.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblCash);

            NumericUpDown numStartingCash = new NumericUpDown();
            numStartingCash.Name = "numStartingCash";
            numStartingCash.Minimum = 10000;
            numStartingCash.Maximum = 1000000;
            numStartingCash.Value = 50000;
            numStartingCash.Increment = 10000;
            numStartingCash.ThousandsSeparator = true;
            numStartingCash.Font = new Font("Segoe UI", 10);
            numStartingCash.Location = new Point(40, 155);
            numStartingCash.Size = new Size(250, 25);
            numStartingCash.BackColor = Color.FromArgb(45, 45, 45);
            numStartingCash.ForeColor = Color.White;
            pnlNewGameSettings.Controls.Add(numStartingCash);

            // Podatki CIT (Aktywne)
            Label lblTax = new Label();
            lblTax.Text = "Podatki Korporacyjne (CIT %):";
            lblTax.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblTax.Location = new Point(40, 195);
            lblTax.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblTax);

            NumericUpDown numTax = new NumericUpDown();
            numTax.Name = "numTax";
            numTax.Minimum = 0;
            numTax.Maximum = 50;
            numTax.Value = 19;
            numTax.Font = new Font("Segoe UI", 10);
            numTax.Location = new Point(40, 215);
            numTax.Size = new Size(250, 25);
            numTax.BackColor = Color.FromArgb(45, 45, 45);
            numTax.ForeColor = Color.White;
            pnlNewGameSettings.Controls.Add(numTax);

            // Parametry Mapy (Nieaktywne)
            Label lblMapSection = new Label();
            lblMapSection.Text = "PARAMETRY MAPY (ZABLOKOWANE):";
            lblMapSection.Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic);
            lblMapSection.ForeColor = Color.Gray;
            lblMapSection.Location = new Point(40, 260);
            lblMapSection.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblMapSection);

            Label lblCities = new Label();
            lblCities.Text = "Liczba miast:";
            lblCities.Font = new Font("Segoe UI", 8.5f);
            lblCities.ForeColor = Color.Gray;
            lblCities.Location = new Point(40, 280);
            lblCities.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblCities);

            NumericUpDown numCities = new NumericUpDown();
            numCities.Value = 1;
            numCities.Enabled = false;
            numCities.Location = new Point(170, 278);
            numCities.Size = new Size(120, 23);
            numCities.BackColor = Color.FromArgb(40, 40, 40);
            numCities.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(numCities);

            Label lblDensity = new Label();
            lblDensity.Text = "Gęstość populacji:";
            lblDensity.Font = new Font("Segoe UI", 8.5f);
            lblDensity.ForeColor = Color.Gray;
            lblDensity.Location = new Point(40, 310);
            lblDensity.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblDensity);

            ComboBox cmbDensity = new ComboBox();
            cmbDensity.Items.AddRange(new object[] { "Niska", "Średnia", "Wysoka" });
            cmbDensity.SelectedIndex = 1;
            cmbDensity.Enabled = false;
            cmbDensity.Location = new Point(170, 308);
            cmbDensity.Size = new Size(120, 23);
            cmbDensity.BackColor = Color.FromArgb(40, 40, 40);
            cmbDensity.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(cmbDensity);

            Label lblResources = new Label();
            lblResources.Text = "Surowce naturalne:";
            lblResources.Font = new Font("Segoe UI", 8.5f);
            lblResources.ForeColor = Color.Gray;
            lblResources.Location = new Point(40, 340);
            lblResources.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblResources);

            ComboBox cmbResources = new ComboBox();
            cmbResources.Items.AddRange(new object[] { "Uboga", "Standardowa", "Obfita" });
            cmbResources.SelectedIndex = 1;
            cmbResources.Enabled = false;
            cmbResources.Location = new Point(170, 338);
            cmbResources.Size = new Size(120, 23);
            cmbResources.BackColor = Color.FromArgb(40, 40, 40);
            cmbResources.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(cmbResources);

            // KOLUMNA PRAWA: Parametry Makro i Kapitał Startowy (Nieaktywne)
            Label lblMacroSection = new Label();
            lblMacroSection.Text = "PARAMETRY MAKRO (ZABLOKOWANE):";
            lblMacroSection.Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic);
            lblMacroSection.ForeColor = Color.Gray;
            lblMacroSection.Location = new Point(360, 75);
            lblMacroSection.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblMacroSection);

            Label lblStartYear = new Label();
            lblStartYear.Text = "Rok startowy:";
            lblStartYear.Font = new Font("Segoe UI", 8.5f);
            lblStartYear.ForeColor = Color.Gray;
            lblStartYear.Location = new Point(360, 95);
            lblStartYear.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblStartYear);

            NumericUpDown numStartYear = new NumericUpDown();
            numStartYear.Minimum = 2000;
            numStartYear.Maximum = 3000;
            numStartYear.Value = 2026;
            numStartYear.Enabled = false;
            numStartYear.Location = new Point(490, 93);
            numStartYear.Size = new Size(120, 23);
            numStartYear.BackColor = Color.FromArgb(40, 40, 40);
            numStartYear.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(numStartYear);

            Label lblInflation = new Label();
            lblInflation.Text = "Inflacja bazowa (%):";
            lblInflation.Font = new Font("Segoe UI", 8.5f);
            lblInflation.ForeColor = Color.Gray;
            lblInflation.Location = new Point(360, 125);
            lblInflation.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblInflation);

            NumericUpDown numInflation = new NumericUpDown();
            numInflation.DecimalPlaces = 1;
            numInflation.Value = 2.0m;
            numInflation.Enabled = false;
            numInflation.Location = new Point(490, 123);
            numInflation.Size = new Size(120, 23);
            numInflation.BackColor = Color.FromArgb(40, 40, 40);
            numInflation.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(numInflation);

            Label lblAggressiveness = new Label();
            lblAggressiveness.Text = "Agresywność AI:";
            lblAggressiveness.Font = new Font("Segoe UI", 8.5f);
            lblAggressiveness.ForeColor = Color.Gray;
            lblAggressiveness.Location = new Point(360, 155);
            lblAggressiveness.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblAggressiveness);

            ComboBox cmbAggressiveness = new ComboBox();
            cmbAggressiveness.Items.AddRange(new object[] { "Niska", "Normalna", "Agresywna" });
            cmbAggressiveness.SelectedIndex = 1;
            cmbAggressiveness.Enabled = false;
            cmbAggressiveness.Location = new Point(490, 153);
            cmbAggressiveness.Size = new Size(120, 23);
            cmbAggressiveness.BackColor = Color.FromArgb(40, 40, 40);
            cmbAggressiveness.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(cmbAggressiveness);

            Label lblCapitalSection = new Label();
            lblCapitalSection.Text = "KAPITAŁ STARTOWY (ZABLOKOWANE):";
            lblCapitalSection.Font = new Font("Segoe UI", 9, FontStyle.Bold | FontStyle.Italic);
            lblCapitalSection.ForeColor = Color.Gray;
            lblCapitalSection.Location = new Point(360, 195);
            lblCapitalSection.Size = new Size(250, 20);
            pnlNewGameSettings.Controls.Add(lblCapitalSection);

            Label lblDebt = new Label();
            lblDebt.Text = "Zadłużenie na starcie:";
            lblDebt.Font = new Font("Segoe UI", 8.5f);
            lblDebt.ForeColor = Color.Gray;
            lblDebt.Location = new Point(360, 215);
            lblDebt.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblDebt);

            NumericUpDown numDebt = new NumericUpDown();
            numDebt.Value = 0;
            numDebt.Enabled = false;
            numDebt.Location = new Point(490, 213);
            numDebt.Size = new Size(120, 23);
            numDebt.BackColor = Color.FromArgb(40, 40, 40);
            numDebt.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(numDebt);

            Label lblShares = new Label();
            lblShares.Text = "Akcje własne:";
            lblShares.Font = new Font("Segoe UI", 8.5f);
            lblShares.ForeColor = Color.Gray;
            lblShares.Location = new Point(360, 245);
            lblShares.Size = new Size(120, 20);
            pnlNewGameSettings.Controls.Add(lblShares);

            NumericUpDown numShares = new NumericUpDown();
            numShares.Minimum = 0;
            numShares.Maximum = 1000000;
            numShares.Value = 1000;
            numShares.Enabled = false;
            numShares.Location = new Point(490, 243);
            numShares.Size = new Size(120, 23);
            numShares.BackColor = Color.FromArgb(40, 40, 40);
            numShares.ForeColor = Color.Gray;
            pnlNewGameSettings.Controls.Add(numShares);

            // Przycisk zatwierdzenia gry
            Button btnCreateGame = new Button();
            btnCreateGame.Text = "ROZPOCZNIJ ROZGRYWKĘ";
            btnCreateGame.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnCreateGame.Location = new Point(150, 395);
            btnCreateGame.Size = new Size(350, 50);
            btnCreateGame.FlatStyle = FlatStyle.Flat;
            btnCreateGame.FlatAppearance.BorderSize = 0;
            btnCreateGame.BackColor = Color.FromArgb(100, 220, 100);
            btnCreateGame.ForeColor = Color.White;
            btnCreateGame.Cursor = Cursors.Hand;
            btnCreateGame.Click += (s, e) =>
            {
                string companyName = txtCompanyNameSettings.Text.Trim();
                if (string.IsNullOrWhiteSpace(companyName))
                {
                    MessageBox.Show("Nazwa korporacji nie może być pusta!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var settings = new GameGenerationSettings
                {
                    StartingCash = numStartingCash.Value,
                    GlobalCorporateTax = numTax.Value / 100.0m
                };

                StartGameWithSettings(companyName, settings);
            };
            pnlNewGameSettings.Controls.Add(btnCreateGame);
        }

        private void InitializeLoadGameMenuPanel()
        {
            pnlLoadGameMenu = new Panel();
            pnlLoadGameMenu.Size = new Size(650, 480);
            pnlLoadGameMenu.BackColor = Color.FromArgb(30, 30, 30);
            pnlLoadGameMenu.Visible = false;
            pnlStartScreen.Controls.Add(pnlLoadGameMenu);

            // Obramowanie ozdobne na górze
            Panel pnlBorder = new Panel();
            pnlBorder.Dock = DockStyle.Top;
            pnlBorder.Height = 4;
            pnlBorder.BackColor = Color.FromArgb(50, 150, 250);
            pnlLoadGameMenu.Controls.Add(pnlBorder);

            // Przycisk wstecz
            Button btnBack = new Button();
            btnBack.Text = "< Powrót";
            btnBack.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBack.Location = new Point(20, 20);
            btnBack.Size = new Size(90, 30);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.FlatAppearance.BorderSize = 1;
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnBack.ForeColor = Color.LightGray;
            btnBack.Cursor = Cursors.Hand;
            btnBack.Click += (s, e) =>
            {
                if (_company != null)
                {
                    ChangeMenuState(MenuState.Playing);
                }
                else
                {
                    ChangeMenuState(MenuState.MainMenu);
                }
            };
            pnlLoadGameMenu.Controls.Add(btnBack);

            // Tytuł
            Label lblTitle = new Label();
            lblTitle.Text = "WCZYTAJ ZAPISANĄ GRĘ";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(120, 20);
            lblTitle.Size = new Size(410, 30);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlLoadGameMenu.Controls.Add(lblTitle);

            // Panel scrollowany na listę zapisów
            Panel pnlSavesList = new Panel();
            pnlSavesList.Name = "pnlSavesList";
            pnlSavesList.Location = new Point(30, 70);
            pnlSavesList.Size = new Size(590, 380);
            pnlSavesList.BackColor = Color.FromArgb(24, 24, 24);
            pnlSavesList.AutoScroll = true;
            pnlLoadGameMenu.Controls.Add(pnlSavesList);
        }

        private void RefreshSavesList()
        {
            var pnlSavesList = pnlLoadGameMenu.Controls.Find("pnlSavesList", true).FirstOrDefault() as Panel;
            if (pnlSavesList == null) return;

            pnlSavesList.Controls.Clear();

            List<System.IO.FileInfo> saveFiles;
            try
            {
                saveFiles = SaveGameManager.GetSaveFiles();
                // Posortuj od najnowszego zapisu na dysku
                saveFiles.Sort((x, y) => y.LastWriteTime.CompareTo(x.LastWriteTime));
            }
            catch (Exception ex)
            {
                Label lblError = new Label();
                lblError.Text = $"Błąd odczytu katalogu: {ex.Message}";
                lblError.ForeColor = Color.Red;
                lblError.Location = new Point(20, 20);
                lblError.Size = new Size(500, 30);
                pnlSavesList.Controls.Add(lblError);
                return;
            }

            if (saveFiles.Count == 0)
            {
                Label lblNoSaves = new Label();
                lblNoSaves.Text = "Brak zapisanych gier w folderze Dokumenty.";
                lblNoSaves.Font = new Font("Segoe UI", 10, FontStyle.Italic);
                lblNoSaves.ForeColor = Color.Gray;
                lblNoSaves.Location = new Point(20, 20);
                lblNoSaves.Size = new Size(500, 30);
                pnlSavesList.Controls.Add(lblNoSaves);
                return;
            }

            int yOffset = 10;
            foreach (var file in saveFiles)
            {
                SaveGameMetadata meta;
                try
                {
                    meta = SaveGameManager.GetSaveMetadata(file.FullName);
                }
                catch
                {
                    continue;
                }

                Panel pnlRow = new Panel();
                pnlRow.Size = new Size(550, 75);
                pnlRow.Location = new Point(10, yOffset);
                pnlRow.BackColor = Color.FromArgb(35, 35, 35);
                pnlRow.BorderStyle = BorderStyle.FixedSingle;

                // Logo firmy placeholder
                Panel pnlLogo = new Panel();
                pnlLogo.Size = new Size(55, 55);
                pnlLogo.Location = new Point(10, 9);
                pnlLogo.BackColor = Color.FromArgb(50, 150, 250);
                
                Label lblLogoLetter = new Label();
                lblLogoLetter.Text = string.IsNullOrEmpty(meta.CorporationName) ? "C" : meta.CorporationName.Substring(0, 1).ToUpper();
                lblLogoLetter.Font = new Font("Segoe UI", 18, FontStyle.Bold);
                lblLogoLetter.ForeColor = Color.White;
                lblLogoLetter.TextAlign = ContentAlignment.MiddleCenter;
                lblLogoLetter.Dock = DockStyle.Fill;
                pnlLogo.Controls.Add(lblLogoLetter);
                pnlRow.Controls.Add(pnlLogo);

                // Szczegóły zapisu
                Label lblCorp = new Label();
                lblCorp.Text = meta.CorporationName;
                lblCorp.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                lblCorp.ForeColor = Color.White;
                lblCorp.Location = new Point(75, 8);
                lblCorp.Size = new Size(200, 20);
                pnlRow.Controls.Add(lblCorp);

                Label lblNetWorth = new Label();
                lblNetWorth.Text = $"Net Worth: {meta.NetWorth:C}";
                lblNetWorth.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                lblNetWorth.ForeColor = Color.FromArgb(100, 220, 100);
                lblNetWorth.Location = new Point(75, 32);
                lblNetWorth.Size = new Size(200, 20);
                pnlRow.Controls.Add(lblNetWorth);

                Label lblTime = new Label();
                lblTime.Text = $"Dzień {meta.CurrentDay} ({meta.CurrentHour:D2}:00)";
                lblTime.Font = new Font("Segoe UI", 9);
                lblTime.ForeColor = Color.FromArgb(240, 180, 50);
                lblTime.Location = new Point(285, 8);
                lblTime.Size = new Size(140, 20);
                pnlRow.Controls.Add(lblTime);

                Label lblRealDate = new Label();
                lblRealDate.Text = $"Data zapisu: {meta.RealWorldSaveTime:dd.MM.yyyy HH:mm}";
                lblRealDate.Font = new Font("Segoe UI", 8, FontStyle.Italic);
                lblRealDate.ForeColor = Color.DarkGray;
                lblRealDate.Location = new Point(285, 32);
                lblRealDate.Size = new Size(150, 20);
                pnlRow.Controls.Add(lblRealDate);

                // Przycisk Wczytaj
                Button btnLoad = new Button();
                btnLoad.Text = "Wczytaj";
                btnLoad.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                btnLoad.Location = new Point(445, 17);
                btnLoad.Size = new Size(90, 40);
                btnLoad.FlatStyle = FlatStyle.Flat;
                btnLoad.FlatAppearance.BorderSize = 0;
                btnLoad.BackColor = Color.FromArgb(50, 150, 250);
                btnLoad.ForeColor = Color.White;
                btnLoad.Cursor = Cursors.Hand;
                
                string filePath = file.FullName;
                btnLoad.Click += (s, e) => LoadGameFromFile(filePath);
                pnlRow.Controls.Add(btnLoad);

                pnlSavesList.Controls.Add(pnlRow);
                yOffset += 85;
            }
        }

        private void InitializeSaveGameOverlayPanel()
        {
            pnlSaveGameOverlay = new Panel();
            pnlSaveGameOverlay.Size = new Size(350, 180);
            pnlSaveGameOverlay.BackColor = Color.FromArgb(30, 30, 30);
            pnlSaveGameOverlay.BorderStyle = BorderStyle.FixedSingle;
            pnlSaveGameOverlay.Visible = false;
            pnlGameBoard.Controls.Add(pnlSaveGameOverlay);
            pnlSaveGameOverlay.BringToFront();

            // Obramowanie ozdobne na górze
            Panel pnlTopLine = new Panel();
            pnlTopLine.Dock = DockStyle.Top;
            pnlTopLine.Height = 4;
            pnlTopLine.BackColor = Color.FromArgb(50, 150, 250);
            pnlSaveGameOverlay.Controls.Add(pnlTopLine);

            Label lblTitle = new Label();
            lblTitle.Text = "ZAPISZ GRĘ";
            lblTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(310, 25);
            pnlSaveGameOverlay.Controls.Add(lblTitle);

            Label lblPrompt = new Label();
            lblPrompt.Text = "Wprowadź nazwę zapisu:";
            lblPrompt.Font = new Font("Segoe UI", 9);
            lblPrompt.Location = new Point(20, 45);
            lblPrompt.Size = new Size(310, 20);
            pnlSaveGameOverlay.Controls.Add(lblPrompt);

            TextBox txtSaveName = new TextBox();
            txtSaveName.Name = "txtSaveName";
            txtSaveName.Font = new Font("Segoe UI", 10);
            txtSaveName.Location = new Point(20, 70);
            txtSaveName.Size = new Size(310, 25);
            txtSaveName.BackColor = Color.FromArgb(45, 45, 45);
            txtSaveName.ForeColor = Color.White;
            txtSaveName.BorderStyle = BorderStyle.FixedSingle;
            pnlSaveGameOverlay.Controls.Add(txtSaveName);

            Button btnSave = new Button();
            btnSave.Text = "Zapisz";
            btnSave.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSave.Location = new Point(20, 115);
            btnSave.Size = new Size(140, 35);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.BackColor = Color.FromArgb(100, 220, 100);
            btnSave.ForeColor = Color.White;
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += (s, e) =>
            {
                string saveName = txtSaveName.Text.Trim();
                if (string.IsNullOrWhiteSpace(saveName))
                {
                    MessageBox.Show("Nazwa zapisu nie może być pusta!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_company != null && _map != null && _gameManager != null)
                {
                    try
                    {
                        SaveGameManager.SaveGame(saveName, _company, _map, _gameManager);
                        lblBottomStatus.Text = $"Stan gry został pomyślnie zapisany jako: '{saveName}'";
                        pnlSaveGameOverlay.Visible = false;
                        
                        // Zrestartuj zegar gry jeśli był uruchomiony
                        if (_activeSpeedButton != null && _activeSpeedButton != btnSpeedPause)
                        {
                            _gameTimer.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisu: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            pnlSaveGameOverlay.Controls.Add(btnSave);

            Button btnCancel = new Button();
            btnCancel.Text = "Anuluj";
            btnCancel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCancel.Location = new Point(190, 115);
            btnCancel.Size = new Size(140, 35);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnCancel.ForeColor = Color.LightGray;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) =>
            {
                pnlSaveGameOverlay.Visible = false;
                // Zrestartuj zegar gry jeśli był uruchomiony
                if (_activeSpeedButton != null && _activeSpeedButton != btnSpeedPause)
                {
                    _gameTimer.Start();
                }
            };
            pnlSaveGameOverlay.Controls.Add(btnCancel);
        }

        private void CenterSaveGameOverlayPanel()
        {
            if (pnlSaveGameOverlay != null && pnlGameBoard != null)
            {
                pnlSaveGameOverlay.Location = new Point(
                    (pnlGameBoard.Width - pnlSaveGameOverlay.Width) / 2,
                    (pnlGameBoard.Height - pnlSaveGameOverlay.Height) / 2
                );
            }
        }

        private void ChangeMenuState(MenuState newState)
        {
            _currentMenuState = newState;

            // Pokaż/ukryj nadrzędne sceny
            pnlStartScreen.Visible = (newState == MenuState.MainMenu || newState == MenuState.NewGameSettings || newState == MenuState.LoadGameMenu);
            pnlGameBoard.Visible = (newState == MenuState.Playing);

            // Pokaż/ukryj pod-panele w pnlStartScreen
            pnlStartCenter.Visible = (newState == MenuState.MainMenu);
            pnlNewGameSettings.Visible = (newState == MenuState.NewGameSettings);
            pnlLoadGameMenu.Visible = (newState == MenuState.LoadGameMenu);

            if (newState == MenuState.Playing)
            {
                // Unpause if speed isn't pause
                if (_activeSpeedButton != btnSpeedPause)
                {
                    _gameTimer.Start();
                }
            }
            else
            {
                _gameTimer.Stop();
            }

            if (newState == MenuState.LoadGameMenu)
            {
                RefreshSavesList();
            }
        }

        private void LoadGameFromFile(string filePath)
        {
            try
            {
                var container = SaveGameManager.LoadGame(filePath);
                if (container == null || container.State == null)
                {
                    MessageBox.Show("Nie udało się wczytać pliku zapisu!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var state = container.State;

                // Stop timer
                _gameTimer.Stop();

                // 1. Recreate Company & restore FinancialEngine state
                _company = new Company(state.CompanyName, state.Cash);
                _company.Engine.RestoreState(
                    state.Cash, 
                    state.ShareCapital, 
                    state.RetainedEarnings, 
                    state.Loans, 
                    state.CurrentMonthIndex, 
                    state.TaxRate
                );

                // 2. Recreate Map
                _map = new Map(10, 10);

                // 3. Recreate Buildings
                foreach (var bData in state.Buildings)
                {
                    Building building;
                    if (bData.Type == "Farm")
                    {
                        building = new Farm(bData.Name);
                    }
                    else if (bData.Type == "CoalMine")
                    {
                        building = new CoalMine(bData.Name);
                    }
                    else if (bData.Type == "FoodWarehouse")
                    {
                        building = new WarehouseBuilding(bData.Name, ResourceCategory.Food);
                    }
                    else if (bData.Type == "MiningWarehouse")
                    {
                        building = new WarehouseBuilding(bData.Name, ResourceCategory.Mining);
                    }
                    else
                    {
                        continue;
                    }

                    // Restore properties
                    building.FacilityId = bData.FacilityId;
                    building.AutoSell = bData.AutoSell;
                    building.AccumulatedDepreciation = bData.AccumulatedDepreciation;

                    // Restore warehouse
                    foreach (var item in bData.Warehouse)
                    {
                        if (building.Warehouse.ContainsKey(item.Key))
                        {
                            building.Warehouse[item.Key] = item.Value;
                        }
                    }

                    // Register in Company and Engine
                    _company.Buildings.Add(building);
                    _company.Engine.RegisterFacility(building);

                    // Place on Map
                    _map.BuildBuildingOnTile(bData.X, bData.Y, building);
                }

                // 4. Recreate GameManager & restore day/hour
                _gameManager = new GameManager(_company, _map);
                _gameManager.RestoreState(state.CurrentDay, state.CurrentHour);

                // 5. Re-subscribe events
                _gameManager.OnTickPerformed += OnTickPerformed;

                // 6. Initialize Map Control
                mapControl.Initialize(_map, _gameManager);

                // 7. Transition to Playing State
                ChangeMenuState(MenuState.Playing);

                // 8. Domyślne uruchomienie gry z prędkością 1x
                SetGameSpeed(1000, btnSpeed1x);

                // 9. Refresh stats & UI
                CloseBuildingDetails();
                CloseFinanceReport();
                RefreshStats();

                lblBottomStatus.Text = $"Gra została pomyślnie wczytana z pliku: {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania gry: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeEscapeMenu()
        {
            pnlEscapeMenu = new Panel();
            pnlEscapeMenu.Size = new Size(250, 260);
            pnlEscapeMenu.BackColor = Color.FromArgb(30, 30, 30);
            pnlEscapeMenu.BorderStyle = BorderStyle.FixedSingle;
            pnlEscapeMenu.Visible = false;
            pnlGameBoard.Controls.Add(pnlEscapeMenu);
            pnlEscapeMenu.BringToFront();

            // Obramowanie ozdobne na górze
            Panel pnlTopLine = new Panel();
            pnlTopLine.Dock = DockStyle.Top;
            pnlTopLine.Height = 4;
            pnlTopLine.BackColor = Color.FromArgb(50, 150, 250);
            pnlEscapeMenu.Controls.Add(pnlTopLine);

            Label lblTitle = new Label();
            lblTitle.Text = "PAUZA";
            lblTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(210, 25);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            pnlEscapeMenu.Controls.Add(lblTitle);

            // Przycisk Kontynuuj
            Button btnContinue = new Button();
            btnContinue.Text = "Kontynuuj";
            btnContinue.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnContinue.Location = new Point(25, 55);
            btnContinue.Size = new Size(200, 35);
            btnContinue.FlatStyle = FlatStyle.Flat;
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.BackColor = Color.FromArgb(50, 150, 250);
            btnContinue.ForeColor = Color.White;
            btnContinue.Cursor = Cursors.Hand;
            btnContinue.Click += (s, e) => ToggleEscapeMenu();
            pnlEscapeMenu.Controls.Add(btnContinue);

            // Przycisk Opcje (Wyłączony)
            Button btnOptions = new Button();
            btnOptions.Text = "Opcje rozgrywki";
            btnOptions.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnOptions.Location = new Point(25, 100);
            btnOptions.Size = new Size(200, 35);
            btnOptions.FlatStyle = FlatStyle.Flat;
            btnOptions.FlatAppearance.BorderSize = 1;
            btnOptions.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnOptions.BackColor = Color.FromArgb(25, 25, 25);
            btnOptions.ForeColor = Color.Gray;
            btnOptions.Enabled = false;
            pnlEscapeMenu.Controls.Add(btnOptions);

            // Przycisk Menu Główne
            Button btnMainMenu = new Button();
            btnMainMenu.Text = "Menu Główne";
            btnMainMenu.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnMainMenu.Location = new Point(25, 145);
            btnMainMenu.Size = new Size(200, 35);
            btnMainMenu.FlatStyle = FlatStyle.Flat;
            btnMainMenu.FlatAppearance.BorderSize = 1;
            btnMainMenu.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnMainMenu.ForeColor = Color.LightGray;
            btnMainMenu.Cursor = Cursors.Hand;
            btnMainMenu.Click += (s, e) =>
            {
                var result = MessageBox.Show("Czy na pewno chcesz wyjść do menu głównego?\nNiezapisany postęp zostanie utracony.", "Powrót do Menu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    pnlEscapeMenu.Visible = false;
                    ChangeMenuState(MenuState.MainMenu);
                }
            };
            pnlEscapeMenu.Controls.Add(btnMainMenu);

            // Przycisk Wyjście z gry
            Button btnExit = new Button();
            btnExit.Text = "Wyjście z Gry";
            btnExit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExit.Location = new Point(25, 190);
            btnExit.Size = new Size(200, 35);
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.BackColor = Color.FromArgb(220, 80, 80);
            btnExit.ForeColor = Color.White;
            btnExit.Cursor = Cursors.Hand;
            btnExit.Click += (s, e) =>
            {
                var result = MessageBox.Show("Czy na pewno chcesz zamknąć grę?", "Wyjście z gry", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            pnlEscapeMenu.Controls.Add(btnExit);
        }

        private void CenterEscapeMenuPanel()
        {
            if (pnlEscapeMenu != null && pnlGameBoard != null)
            {
                pnlEscapeMenu.Location = new Point(
                    (pnlGameBoard.Width - pnlEscapeMenu.Width) / 2,
                    (pnlGameBoard.Height - pnlEscapeMenu.Height) / 2
                );
            }
        }

        private void ToggleEscapeMenu()
        {
            if (pnlEscapeMenu == null) return;

            if (pnlEscapeMenu.Visible)
            {
                pnlEscapeMenu.Visible = false;
                if (_activeSpeedButton != btnSpeedPause && _currentMenuState == MenuState.Playing)
                {
                    _gameTimer.Start();
                }
            }
            else
            {
                _gameTimer.Stop();
                CenterEscapeMenuPanel();
                pnlEscapeMenu.Visible = true;
                pnlEscapeMenu.BringToFront();
                pnlEscapeMenu.Focus();
            }
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
