using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Financials;
using Conglomerate.Logistics;
using Conglomerate.Retail;
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

        // Panele logistyczne (floating overlays)
        private Panel pnlLogisticsManager = null!;  // Zarządzanie trasami dla wybranego budynku
        private Panel pnlMarketBuyer = null!;        // Zakup surowców na wolnym rynku

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

        private enum SelectedBlueprint { None, Farm, CoalMine, FoodWarehouse, MiningWarehouse, CheeseFactory, GeneralStore, CopperMine, CopperFoundry }
        private SelectedBlueprint _selectedBlueprint = SelectedBlueprint.None;
        private Button btnBuildFarm = null!;
        private Button btnBuildCoalMine = null!;
        private Button btnBuildFoodWarehouse = null!;
        private Button btnBuildMiningWarehouse = null!;
        private Button btnBuildCheeseFactory = null!;
        private Button btnBuildCopperFoundry = null!;
        private Button btnBuildGeneralStore = null!;
        private Button btnBuildCopperMine = null!;

        // Kontrolki fabryki — przechowujemy referencję do dropdownu przepisu
        private ComboBox? _activeRecipeComboBox = null;

        // Przyciski kontroli prędkości czasu
        private Button btnSpeedPause = null!;
        private Button btnSpeed1x = null!;
        private Button btnSpeed2x = null!;
        private Button btnSpeed5x = null!;
        private Button? _activeSpeedButton = null;

        private XnaPoint? _hoveredTile = null;
        private Building? _inspectingBuilding = null;
        private Logistics.SupplyRoute? _editingRoute = null;
        private Dictionary<string, string> _enteredSellQuantities = new Dictionary<string, string>();
        private Panel pnlFinanceReport = null!;

        // HUD Components
        private Panel pnlTopNav = null!;
        private Label lblDate = null!;
        private Label lblCashTrend = null!;
        private Label lblNetWorth = null!;
        private Queue<decimal> _cashHistory = new Queue<decimal>();

        private Panel pnlRightShortcutBar = null!;

        private ToolTip _toolTip = new ToolTip();

        private Panel pnlNewsTicker = null!;
        private Label lblNewsMarquee = null!;
        private System.Windows.Forms.Timer _newsTickerTimer = null!;
        private List<string> _newsQueue = new List<string>();

        private Panel pnlContextInspector = null!;
        private Label lblContextTitle = null!;
        private Label lblContextPnL = null!;
        private Label lblContextUtilization = null!;
        private Panel pnlContextUtilProgressBg = null!;
        private Panel pnlContextUtilProgressFg = null!;
        private Label lblContextInv = null!;
        private Panel pnlContextInvBars = null!;
        private Button btnCtxCenter = null!;
        private Button btnCtxMarket = null!;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                return cp;
            }
        }

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

            // 2.0 PANEL GÓRNY NAWIGACJI (TOP BAR)
            pnlTopNav = new Panel();
            pnlTopNav.Dock = DockStyle.Top;
            pnlTopNav.Height = 60;
            pnlTopNav.BackColor = Color.FromArgb(20, 20, 20);
            pnlTopNav.Padding = new Padding(15, 0, 15, 0);
            pnlGameBoard.Controls.Add(pnlTopNav);

            // Dolne ozdobne obramowanie paska górnego
            Panel pnlTopNavBottomBorder = new Panel();
            pnlTopNavBottomBorder.Dock = DockStyle.Bottom;
            pnlTopNavBottomBorder.Height = 1;
            pnlTopNavBottomBorder.BackColor = Color.FromArgb(45, 45, 45);
            pnlTopNav.Controls.Add(pnlTopNavBottomBorder);

            // Zegar / Data gry (teraz dwuliniowy: data + godzina)
            lblDate = new Label();
            lblDate.Text = "12 Czerwca 2026\n08:00";
            lblDate.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblDate.ForeColor = Color.FromArgb(240, 180, 50);
            lblDate.Location = new Point(15, 10);
            lblDate.Size = new Size(165, 40);
            pnlTopNav.Controls.Add(lblDate);

            // Kontrolki prędkości w pasku górnym
            Panel pnlSpeedControls = new Panel();
            pnlSpeedControls.Location = new Point(185, 15);
            pnlSpeedControls.Size = new Size(160, 32);
            pnlTopNav.Controls.Add(pnlSpeedControls);

            btnSpeedPause = CreateSpeedButton("||", 0, "Zatrzymaj czas (Pauza)", (s, e) => SetGameSpeed(0, btnSpeedPause));
            btnSpeedPause.Size = new Size(35, 30);
            btnSpeedPause.Location = new Point(0, 0);

            btnSpeed1x = CreateSpeedButton("1x", 0, "Standardowa prędkość", (s, e) => SetGameSpeed(1000, btnSpeed1x));
            btnSpeed1x.Size = new Size(35, 30);
            btnSpeed1x.Location = new Point(40, 0);

            btnSpeed2x = CreateSpeedButton("2x", 0, "Podwójna prędkość", (s, e) => SetGameSpeed(500, btnSpeed2x));
            btnSpeed2x.Size = new Size(35, 30);
            btnSpeed2x.Location = new Point(80, 0);

            btnSpeed5x = CreateSpeedButton("5x", 0, "Szybka prędkość (5x)", (s, e) => SetGameSpeed(200, btnSpeed5x));
            btnSpeed5x.Size = new Size(35, 30);
            btnSpeed5x.Location = new Point(120, 0);

            pnlSpeedControls.Controls.Add(btnSpeedPause);
            pnlSpeedControls.Controls.Add(btnSpeed1x);
            pnlSpeedControls.Controls.Add(btnSpeed2x);
            pnlSpeedControls.Controls.Add(btnSpeed5x);

            // Wyświetlacz Wolnej Gotówki (Free Cash)
            Label lblCashTitle = new Label();
            lblCashTitle.Text = "GOTÓWKA:";
            lblCashTitle.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblCashTitle.ForeColor = Color.DarkGray;
            lblCashTitle.Location = new Point(360, 10);
            lblCashTitle.Size = new Size(160, 15);
            pnlTopNav.Controls.Add(lblCashTitle);

            lblCash = new Label();
            lblCash.Text = "0,00 zł";
            lblCash.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            lblCash.ForeColor = Color.FromArgb(100, 220, 100);
            lblCash.Location = new Point(360, 25);
            lblCash.Size = new Size(180, 25);
            pnlTopNav.Controls.Add(lblCash);

            // Wskaźnik Trendu Gotówkowego (Trend Indicator)
            lblCashTrend = new Label();
            lblCashTrend.Text = "+0,00 zł / min";
            lblCashTrend.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblCashTrend.ForeColor = Color.FromArgb(100, 220, 100);
            lblCashTrend.Location = new Point(545, 27);
            lblCashTrend.Size = new Size(110, 20);
            pnlTopNav.Controls.Add(lblCashTrend);

            // Corporate Net Worth Display
            lblNetWorth = new Label();
            lblNetWorth.Text = "Net Worth: ...";
            lblNetWorth.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblNetWorth.ForeColor = Color.FromArgb(50, 150, 250);
            lblNetWorth.Location = new Point(665, 18);
            lblNetWorth.Size = new Size(220, 25);
            pnlTopNav.Controls.Add(lblNetWorth);

            // Przycisk Zapisu Gry
            Button btnNavSave = new Button();
            btnNavSave.Text = "ZAPISZ GRĘ";
            btnNavSave.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnNavSave.Size = new Size(100, 30);
            btnNavSave.Location = new Point(pnlTopNav.Width - 230, 15);
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
                    HideAllOverlays(pnlSaveGameOverlay);
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

            // Przycisk Wczytania Gry
            Button btnNavLoad = new Button();
            btnNavLoad.Text = "WCZYTAJ GRĘ";
            btnNavLoad.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnNavLoad.Size = new Size(100, 30);
            btnNavLoad.Location = new Point(pnlTopNav.Width - 120, 15);
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

            // Wyświetlacz loga w tle (mały)
            Label lblCompanyNameDummy = new Label();
            lblCompanyNameDummy.Location = new Point(0, 0);
            lblCompanyNameDummy.Visible = false;
            lblCompanyName = lblCompanyNameDummy; // Zachowaj referencję dla kompatybilności wstecznej

            lblDay = new Label(); // Dummy dla kompatybilności wstecznej

            // Wyłączenie starego panelu lewego (statystyki zostały zintegrowane z HUD)
            pnlLeft = new Panel();
            pnlLeft.Size = new Size(0, 0);
            pnlLeft.Visible = false;

            // Etykieta szczegółów kafela (Hover Info w lewym dolnym rogu)
            lblSelectedTileInfo = new Label();
            lblSelectedTileInfo.Text = "Najedź myszką na mapę,\naby zobaczyć szczegóły.";
            lblSelectedTileInfo.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblSelectedTileInfo.ForeColor = Color.FromArgb(200, 200, 200);
            lblSelectedTileInfo.Location = new Point(10, 10);
            lblSelectedTileInfo.Size = new Size(180, 80);

            Panel pnlHoverInfo = new Panel();
            pnlHoverInfo.Size = new Size(200, 100);
            pnlHoverInfo.Location = new Point(15, this.ClientSize.Height - 30 - 100 - 15); // Nad tickerem newsowym
            pnlHoverInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            pnlHoverInfo.BackColor = Color.FromArgb(180, 20, 20, 20); // Półprzeźroczyste ciemne tło
            pnlHoverInfo.BorderStyle = BorderStyle.FixedSingle;
            pnlHoverInfo.Controls.Add(lblSelectedTileInfo);
            pnlGameBoard.Controls.Add(pnlHoverInfo);
            pnlHoverInfo.BringToFront();

            // 2.1a PASEK NEWSÓW (BOTTOM TICKER)
            pnlNewsTicker = new Panel();
            pnlNewsTicker.Dock = DockStyle.Bottom;
            pnlNewsTicker.Height = 30;
            pnlNewsTicker.BackColor = Color.FromArgb(15, 15, 15);
            pnlGameBoard.Controls.Add(pnlNewsTicker);

            Panel pnlNewsTopBorder = new Panel();
            pnlNewsTopBorder.Dock = DockStyle.Top;
            pnlNewsTopBorder.Height = 1;
            pnlNewsTopBorder.BackColor = Color.FromArgb(45, 45, 45);
            pnlNewsTicker.Controls.Add(pnlNewsTopBorder);

            // Kolejka newsów i etykieta przewijana
            _newsQueue = new List<string>
            {
                "MAKRO: Globalna inflacja stabilizuje się na poziomie 2.1%. Analitycy przewidują stabilny wzrost gospodarczy.",
                "KONKURENCJA: AI Megacorp otwiera nowy sklep detaliczny w pobliżu Twojego sektora!",
                "PODATKI: Kongres rozważa obniżenie globalnego podatku dochodowego (CIT) w przyszłym roku podatkowym.",
                "SUROWCE: Popyt na węgiel wzrasta z powodu mroźnej zimy, ceny surowców energetycznych rosną o 5%!",
                "ROLNICTWO: Rekordowa produkcja mleka w tym kwartale. Nadchodzi korekta cen produktów spożywczych."
            };

            lblNewsMarquee = new Label();
            lblNewsMarquee.Text = "   |   " + string.Join("   |   ", _newsQueue) + "   |   ";
            lblNewsMarquee.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblNewsMarquee.ForeColor = Color.LightGray;
            lblNewsMarquee.AutoSize = true;
            lblNewsMarquee.Location = new Point(800, 8);
            pnlNewsTicker.Controls.Add(lblNewsMarquee);

            // Timer do przewijania newsów
            _newsTickerTimer = new System.Windows.Forms.Timer();
            _newsTickerTimer.Interval = 30;
            _newsTickerTimer.Tick += (s, e) =>
            {
                if (pnlGameBoard.Visible)
                {
                    lblNewsMarquee.Left -= 1;
                    if (lblNewsMarquee.Right < 0)
                    {
                        lblNewsMarquee.Left = pnlNewsTicker.Width;
                    }
                }
            };
            _newsTickerTimer.Start();

            // 2.1b KONTEKSTOWY INSPEKTOR BUDYNKU (SELECTED OBJECT INSPECTOR)
            pnlContextInspector = new Panel(); typeof(Panel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, pnlContextInspector, new object[] { true });
            pnlContextInspector.Size = new Size(800, 130);
            pnlContextInspector.BackColor = Color.FromArgb(25, 25, 25);
            pnlContextInspector.BorderStyle = BorderStyle.FixedSingle;
            pnlContextInspector.Visible = false;
            pnlGameBoard.Controls.Add(pnlContextInspector);

            // Obramowanie ozdobne na górze inspektora
            Panel pnlCtxTopLine = new Panel();
            pnlCtxTopLine.Dock = DockStyle.Top;
            pnlCtxTopLine.Height = 3;
            pnlCtxTopLine.BackColor = Color.FromArgb(50, 150, 250);
            pnlContextInspector.Controls.Add(pnlCtxTopLine);

            // Przycisk zamknięcia [X]
            Button btnCtxClose = new Button();
            btnCtxClose.Text = "X";
            btnCtxClose.AccessibleName = "Zamknij inspektor";
            btnCtxClose.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btnCtxClose.Size = new Size(20, 20);
            btnCtxClose.Location = new Point(770, 8);
            btnCtxClose.FlatStyle = FlatStyle.Flat;
            btnCtxClose.FlatAppearance.BorderSize = 0;
            btnCtxClose.ForeColor = Color.Gray;
            btnCtxClose.Cursor = Cursors.Hand;
            btnCtxClose.Click += (s, e) => CloseContextInspector();
            _toolTip.SetToolTip(btnCtxClose, "Zamknij inspektor");
            pnlContextInspector.Controls.Add(btnCtxClose);

            // Tytuł i P&L w pierwszej linii
            lblContextTitle = new Label();
            lblContextTitle.Text = "Farma Krów (Gracz)";
            lblContextTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblContextTitle.ForeColor = Color.FromArgb(50, 150, 250);
            lblContextTitle.Location = new Point(15, 10);
            lblContextTitle.Size = new Size(300, 22);
            pnlContextInspector.Controls.Add(lblContextTitle);

            lblContextPnL = new Label();
            lblContextPnL.Text = "Wynik (P&L): +0,00 zł";
            lblContextPnL.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblContextPnL.ForeColor = Color.FromArgb(100, 220, 100);
            lblContextPnL.Location = new Point(320, 10);
            lblContextPnL.Size = new Size(240, 22);
            lblContextPnL.TextAlign = ContentAlignment.TopRight;
            pnlContextInspector.Controls.Add(lblContextPnL);

            // Kolumna 1: Wydajność (Capacity Utilization)
            lblContextUtilization = new Label();
            lblContextUtilization.Text = "Wykorzystanie Mocy: 100%";
            lblContextUtilization.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblContextUtilization.ForeColor = Color.DarkGray;
            lblContextUtilization.Location = new Point(15, 45);
            lblContextUtilization.Size = new Size(250, 15);
            pnlContextInspector.Controls.Add(lblContextUtilization);

            pnlContextUtilProgressBg = new Panel();
            pnlContextUtilProgressBg.Location = new Point(15, 65);
            pnlContextUtilProgressBg.Size = new Size(250, 14);
            pnlContextUtilProgressBg.BackColor = Color.FromArgb(45, 45, 45);
            pnlContextInspector.Controls.Add(pnlContextUtilProgressBg);

            pnlContextUtilProgressFg = new Panel();
            pnlContextUtilProgressFg.Location = new Point(0, 0);
            pnlContextUtilProgressFg.Size = new Size(250, 14);
            pnlContextUtilProgressFg.BackColor = Color.FromArgb(50, 150, 250);
            pnlContextUtilProgressBg.Controls.Add(pnlContextUtilProgressFg);

            // Przycisk do wycentrowania widoku wewnątrz inspektora (bardzo przydatny)
            btnCtxCenter = new Button();
            btnCtxCenter.Text = "Pozycjonuj Kamerę";
            btnCtxCenter.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            btnCtxCenter.Size = new Size(130, 22);
            btnCtxCenter.Location = new Point(15, 90);
            btnCtxCenter.FlatStyle = FlatStyle.Flat;
            btnCtxCenter.FlatAppearance.BorderSize = 1;
            btnCtxCenter.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnCtxCenter.BackColor = Color.FromArgb(35, 35, 35);
            btnCtxCenter.ForeColor = Color.LightGray;
            btnCtxCenter.Cursor = Cursors.Hand;
            btnCtxCenter.Click += (s, e) => mapControl.CenterCamera();
            pnlContextInspector.Controls.Add(btnCtxCenter);


            // Przycisk Rynku
            btnCtxMarket = new Button();
            btnCtxMarket.Text = "📈 Rynek";
            btnCtxMarket.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnCtxMarket.Size = new Size(85, 22);
            btnCtxMarket.Location = new Point(265, 90);
            btnCtxMarket.FlatStyle = FlatStyle.Flat;
            btnCtxMarket.FlatAppearance.BorderSize = 1;
            btnCtxMarket.FlatAppearance.BorderColor = Color.FromArgb(240, 140, 20);
            btnCtxMarket.BackColor = Color.FromArgb(35, 35, 35);
            btnCtxMarket.ForeColor = Color.FromArgb(240, 140, 20);
            btnCtxMarket.Cursor = Cursors.Hand;
            btnCtxMarket.Click += (s, e) => OpenMarketBuyer(_inspectingBuilding);
            pnlContextInspector.Controls.Add(btnCtxMarket);

            // Kolumna 2: Stan Magazynu i fill-bary
            lblContextInv = new Label();
            lblContextInv.Text = "Stan Magazynu: 0 / 30 szt.";
            lblContextInv.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblContextInv.ForeColor = Color.DarkGray;
            lblContextInv.Location = new Point(300, 45);
            lblContextInv.Size = new Size(250, 15);
            pnlContextInspector.Controls.Add(lblContextInv);

            pnlContextInvBars = new Panel(); typeof(Panel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, pnlContextInvBars, new object[] { true });
            pnlContextInvBars.Location = new Point(300, 65);
            pnlContextInvBars.Size = new Size(480, 50);
            pnlContextInvBars.BackColor = Color.FromArgb(20, 20, 20);
            pnlContextInvBars.AutoScroll = true;
            pnlContextInspector.Controls.Add(pnlContextInvBars);

            // 2.1c PIONOWY PASEK SKRÓTÓW (RIGHT SHORTCUT SIDEBAR)
            pnlRightShortcutBar = new Panel();
            pnlRightShortcutBar.Dock = DockStyle.Right;
            pnlRightShortcutBar.Width = 60;
            pnlRightShortcutBar.BackColor = Color.FromArgb(15, 15, 15);
            pnlGameBoard.Controls.Add(pnlRightShortcutBar);

            // Ozdobna linia boczna
            Panel pnlRightShortcutBorder = new Panel();
            pnlRightShortcutBorder.Dock = DockStyle.Left;
            pnlRightShortcutBorder.Width = 1;
            pnlRightShortcutBorder.BackColor = Color.FromArgb(45, 45, 45);
            pnlRightShortcutBar.Controls.Add(pnlRightShortcutBorder);

            // Stack przycisków skrótów
            Button btnShortcutFinance = CreateShortcutButton("$", 15, Color.FromArgb(100, 220, 100), "Raport Finansowy", (s, e) => ToggleFinanceReport());

            Button btnShortcutResearch = CreateShortcutButton("🔬", 80, Color.FromArgb(50, 150, 250), "Badania i Rozwój (R&D)", (s, e) => MessageBox.Show("Moduł Badawczo-Rozwojowy (R&D) jest zablokowany w tej wersji demonstracyjnej.\n\nAby odblokować drzewo technologiczne i badania, zakup pełną wersję gry.", "Moduł R&D Zablokowany", MessageBoxButtons.OK, MessageBoxIcon.Information));

            Button btnShortcutMarketing = CreateShortcutButton("📢", 145, Color.FromArgb(240, 180, 50), "Marketing i Marka", (s, e) => MessageBox.Show("Moduł Marketingu i Zarządzania Marką jest zablokowany w tej wersji demonstracyjnej.\n\nAby prowadzić kampanie reklamowe i budować wizerunek firmy, zakup pełną wersję gry.", "Moduł Marketingu Zablokowany", MessageBoxButtons.OK, MessageBoxIcon.Information));

            Button btnShortcutHR = CreateShortcutButton("👥", 210, Color.FromArgb(220, 100, 220), "Zasoby Ludzkie (HR)", (s, e) => MessageBox.Show("Moduł HR i Zarządzania Kadrą Kierowniczą jest zablokowany w tej wersji demonstracyjnej.\n\nAby zatrudniać dyrektorów (CEO, CFO, COO) i zarządzać płacami, zakup pełną wersję gry.", "Moduł HR Zablokowany", MessageBoxButtons.OK, MessageBoxIcon.Information));

            Button btnShortcutLogistics = CreateShortcutButton("🚚", 275, Color.FromArgb(240, 180, 50), "Logistyka i Flota", (s, e) => ToggleLogisticsManagerPanel());

            pnlRightShortcutBar.Controls.Add(btnShortcutFinance);
            pnlRightShortcutBar.Controls.Add(btnShortcutResearch);
            pnlRightShortcutBar.Controls.Add(btnShortcutMarketing);
            pnlRightShortcutBar.Controls.Add(btnShortcutHR);
            pnlRightShortcutBar.Controls.Add(btnShortcutLogistics);

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

            btnBuildCopperMine = new Button();
            btnBuildCopperMine.Text = "Kopalnia Miedzi\n(Koszt: $20k)";
            btnBuildCopperMine.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildCopperMine.Location = new Point(15, 180);
            btnBuildCopperMine.Size = new Size(160, 50);
            btnBuildCopperMine.FlatStyle = FlatStyle.Flat;
            btnBuildCopperMine.FlatAppearance.BorderSize = 1;
            btnBuildCopperMine.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildCopperMine.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildCopperMine.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildCopperMine.Cursor = Cursors.Hand;
            btnBuildCopperMine.Click += (s, e) => SelectBlueprint(SelectedBlueprint.CopperMine, btnBuildCopperMine);
            pnlRight.Controls.Add(btnBuildCopperMine);

            btnBuildFoodWarehouse = new Button();
            btnBuildFoodWarehouse.Text = "Magazyn Żywności\n(Koszt: $8k)";
            btnBuildFoodWarehouse.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildFoodWarehouse.Location = new Point(15, 240);
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
            btnBuildMiningWarehouse.Location = new Point(15, 300);
            btnBuildMiningWarehouse.Size = new Size(160, 50);
            btnBuildMiningWarehouse.FlatStyle = FlatStyle.Flat;
            btnBuildMiningWarehouse.FlatAppearance.BorderSize = 1;
            btnBuildMiningWarehouse.FlatAppearance.BorderColor = Color.FromArgb(50, 150, 250);
            btnBuildMiningWarehouse.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildMiningWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
            btnBuildMiningWarehouse.Cursor = Cursors.Hand;
            btnBuildMiningWarehouse.Click += (s, e) => SelectBlueprint(SelectedBlueprint.MiningWarehouse, btnBuildMiningWarehouse);
            pnlRight.Controls.Add(btnBuildMiningWarehouse);

            // Separator — Fabryki przetwórcze
            Label lblFactoriesHeader = new Label();
            lblFactoriesHeader.Text = "FABRYKI";
            lblFactoriesHeader.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblFactoriesHeader.ForeColor = Color.FromArgb(240, 180, 50);
            lblFactoriesHeader.Location = new Point(15, 305);
            lblFactoriesHeader.Size = new Size(160, 18);
            pnlRight.Controls.Add(lblFactoriesHeader);

            btnBuildCheeseFactory = new Button();
            btnBuildCheeseFactory.Text = "Mleczarnia / Ser\n(Koszt: $25k)";
            btnBuildCheeseFactory.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildCheeseFactory.Location = new Point(15, 385);
            btnBuildCheeseFactory.Size = new Size(160, 50);
            btnBuildCheeseFactory.FlatStyle = FlatStyle.Flat;
            btnBuildCheeseFactory.FlatAppearance.BorderSize = 1;
            btnBuildCheeseFactory.FlatAppearance.BorderColor = Color.FromArgb(240, 180, 50);
            btnBuildCheeseFactory.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildCheeseFactory.ForeColor = Color.FromArgb(240, 180, 50);
            btnBuildCheeseFactory.Cursor = Cursors.Hand;
            btnBuildCheeseFactory.Click += (s, e) => SelectBlueprint(SelectedBlueprint.CheeseFactory, btnBuildCheeseFactory);
            pnlRight.Controls.Add(btnBuildCheeseFactory);

            btnBuildCopperFoundry = new Button();
            btnBuildCopperFoundry.Text = "Huta Miedzi\n(Koszt: $30k)";
            btnBuildCopperFoundry.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildCopperFoundry.Location = new Point(15, 445);
            btnBuildCopperFoundry.Size = new Size(160, 50);
            btnBuildCopperFoundry.FlatStyle = FlatStyle.Flat;
            btnBuildCopperFoundry.FlatAppearance.BorderSize = 1;
            btnBuildCopperFoundry.FlatAppearance.BorderColor = Color.FromArgb(240, 180, 50);
            btnBuildCopperFoundry.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildCopperFoundry.ForeColor = Color.FromArgb(240, 180, 50);
            btnBuildCopperFoundry.Cursor = Cursors.Hand;
            btnBuildCopperFoundry.Click += (s, e) => SelectBlueprint(SelectedBlueprint.CopperFoundry, btnBuildCopperFoundry);
            pnlRight.Controls.Add(btnBuildCopperFoundry);

            // ── HANDEL DETALICZNY ──
            var lblRetailSection = new Label();
            lblRetailSection.Text = "──  HANDEL  ──";
            lblRetailSection.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lblRetailSection.ForeColor = Color.FromArgb(80, 220, 120);
            lblRetailSection.Location = new Point(15, 505);
            lblRetailSection.Size = new Size(160, 16);
            lblRetailSection.TextAlign = ContentAlignment.MiddleCenter;
            pnlRight.Controls.Add(lblRetailSection);

            btnBuildGeneralStore = new Button();
            btnBuildGeneralStore.Text = "🛒 Sklep Ogólny\n(Koszt: $25k)";
            btnBuildGeneralStore.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnBuildGeneralStore.Location = new Point(15, 525);
            btnBuildGeneralStore.Size = new Size(160, 50);
            btnBuildGeneralStore.FlatStyle = FlatStyle.Flat;
            btnBuildGeneralStore.FlatAppearance.BorderSize = 1;
            btnBuildGeneralStore.FlatAppearance.BorderColor = Color.FromArgb(80, 220, 120);
            btnBuildGeneralStore.BackColor = Color.FromArgb(35, 35, 35);
            btnBuildGeneralStore.ForeColor = Color.FromArgb(80, 220, 120);
            btnBuildGeneralStore.Cursor = Cursors.Hand;
            btnBuildGeneralStore.Click += (s, e) => SelectBlueprint(SelectedBlueprint.GeneralStore, btnBuildGeneralStore);
            pnlRight.Controls.Add(btnBuildGeneralStore);

            // 2.3 PANEL DOLNY (Status) - ukryty w celu uzyskania nowoczesnego HUDu
            pnlBottom = new Panel();
            pnlBottom.Visible = false;

            lblBottomStatus = new Label();
            lblBottomStatus.Text = "";

            // 2.4 CENTRALNY MONOGAME PANEL (Renderer)
            mapControl = new IsometricMapControl();
            mapControl.Dock = DockStyle.Fill;
            pnlGameBoard.Controls.Add(mapControl);
            mapControl.BringToFront();

            // 2.6 PANEL RAPORTU FINANSOWEGO (Floating Overlay Panel)
            pnlFinanceReport = new Panel();
            pnlFinanceReport.Size = new Size(1000, 800);
            pnlFinanceReport.BackColor = Color.FromArgb(30, 30, 30);
            pnlFinanceReport.BorderStyle = BorderStyle.FixedSingle;
            pnlFinanceReport.Visible = false;
            pnlGameBoard.Controls.Add(pnlFinanceReport);
            pnlFinanceReport.BringToFront();

            // Zapewnienie automatycznego centrowania przy zmianie rozmiaru
            pnlGameBoard.SizeChanged += (s, e) => {
                CenterFinanceReportPanel();
                CenterSaveGameOverlayPanel();
                CenterEscapeMenuPanel();
                CenterContextInspectorPanel();
                CenterPanel(pnlLogisticsManager);
                CenterPanel(pnlMarketBuyer);
            };

            this.Resize += MainForm_Resize;
            // Rejestracja zdarzeń mapy (jednorazowo)
            mapControl.OnTileSelected += OnTileSelectedOnMap;
            mapControl.OnTileHovered += OnTileHoveredOnMap;

            // Inicjalizacja nowych paneli nakładkowych i menu
            InitializeNewGameSettingsPanel();
            InitializeLoadGameMenuPanel();
            InitializeSaveGameOverlayPanel();
            InitializeEscapeMenu();
            InitializeLogisticsManagerPanel();
            InitializeMarketBuyerPanel();

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

        private Button CreateSpeedButton(string text, int x, string tooltipText, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.AccessibleName = tooltipText;
            btn.Size = new Size(40, 32);
            btn.Location = new Point(x, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(50, 50, 50);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            _toolTip.SetToolTip(btn, tooltipText);
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
                // Reset all buttons to their default colors
                btnBuildFarm.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFarm.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildCoalMine.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildCoalMine.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildFoodWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildFoodWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildMiningWarehouse.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildMiningWarehouse.ForeColor = Color.FromArgb(50, 150, 250);
                btnBuildCheeseFactory.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildCheeseFactory.ForeColor = Color.FromArgb(240, 180, 50);
                btnBuildGeneralStore.BackColor = Color.FromArgb(35, 35, 35);
                btnBuildGeneralStore.ForeColor = Color.FromArgb(80, 220, 120);

                _selectedBlueprint = blueprint;
                // Podświetl kliknięty przycisk z odpowiednim kolorem akcentu
                Color accent = blueprint switch
                {
                    SelectedBlueprint.CheeseFactory => Color.FromArgb(240, 180, 50),
                    SelectedBlueprint.GeneralStore  => Color.FromArgb(80, 220, 120),
                    _                               => Color.FromArgb(50, 150, 250)
                };
                clickedButton.BackColor = accent;
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
                    else if (_selectedBlueprint == SelectedBlueprint.CopperMine)
                    {
                        buildingName = $"Kopalnia Miedzi #{_company.Buildings.Count + 1}";
                        building = new CopperMine(buildingName);
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
                    else if (_selectedBlueprint == SelectedBlueprint.CheeseFactory)
                    {
                        buildingName = $"Mleczarnia #{_company.Buildings.Count + 1}";
                        building = new CheeseFactory(buildingName);
                    }
                    else if (_selectedBlueprint == SelectedBlueprint.CopperFoundry)
                    {
                        buildingName = $"Huta Miedzi #{_company.Buildings.Count + 1}";
                        building = new CopperFoundry(buildingName);
                    }
                    else if (_selectedBlueprint == SelectedBlueprint.GeneralStore)
                    {
                        buildingName = $"Sklep Ogólny #{_company.Buildings.Count + 1}";
                        building = new GeneralStore(buildingName);
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
                    ShowContextInspector(tile.Building);
                }
                else
                {
                    CloseContextInspector();
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
            if (pnlContextInspector.Visible && _inspectingBuilding != null)
            {
                UpdateContextInspector();
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
            lblCash.ForeColor = _company.Balance >= 0 ? Color.FromArgb(100, 220, 100) : Color.FromArgb(240, 80, 80);
            
            // Format zegara: dd MMMM yyyy + godzina HH:00
            var currentInGameDate = new DateTime(2026, 6, 12).AddDays(_gameManager.CurrentDay - 1);
            string dateLine = currentInGameDate.ToString("dd MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
            string timeLine = $"{_gameManager.CurrentHour:D2}:00";
            lblDate.Text = $"{dateLine}\n{timeLine}";

            // Trend gotówki w oparciu o historię ostatnich 24 ticków (1 doba gry)
            _cashHistory.Enqueue(_company.Balance);
            if (_cashHistory.Count > 24) _cashHistory.Dequeue();

            decimal hourlyTrend = 0m;
            if (_cashHistory.Count > 1)
            {
                decimal firstCash = _cashHistory.Peek();
                decimal lastCash = _company.Balance;
                hourlyTrend = (lastCash - firstCash) / (_cashHistory.Count - 1);
            }
            decimal minTrend = hourlyTrend / 60m;
            lblCashTrend.Text = $"{(minTrend >= 0 ? "+" : "")}{minTrend:C}/min";
            lblCashTrend.ForeColor = minTrend >= 0 ? Color.FromArgb(100, 220, 100) : Color.FromArgb(240, 80, 80);

            // Corporate Net Worth
            var bs = _company.Engine.CalculateCurrentBalanceSheet();
            decimal netWorth = bs.TotalAssets - bs.TotalLiabilities;
            lblNetWorth.Text = $"Net Worth: {netWorth:C}";

            // Live Context Inspector Update
            if (pnlContextInspector.Visible)
            {
                UpdateContextInspector();
            }
        }
private void HideAllOverlays(Panel? exceptPanel = null)
        {
            if (pnlFinanceReport != null && pnlFinanceReport != exceptPanel && pnlFinanceReport.Visible)
            {
                pnlFinanceReport.Visible = false;
            }
            if (pnlLogisticsManager != null && pnlLogisticsManager != exceptPanel && pnlLogisticsManager.Visible)
            {
                pnlLogisticsManager.Visible = false;
            }
            if (pnlMarketBuyer != null && pnlMarketBuyer != exceptPanel && pnlMarketBuyer.Visible)
            {
                pnlMarketBuyer.Visible = false;
            }
            if (pnlEscapeMenu != null && pnlEscapeMenu != exceptPanel && pnlEscapeMenu.Visible)
            {
                pnlEscapeMenu.Visible = false;
            }
            if (pnlSaveGameOverlay != null && pnlSaveGameOverlay != exceptPanel && pnlSaveGameOverlay.Visible)
            {
                pnlSaveGameOverlay.Visible = false;
            }
        }
        private void CenterContextInspectorPanel()
        {
            if (pnlContextInspector != null && pnlGameBoard != null)
            {
                pnlContextInspector.Width = (int)(pnlGameBoard.Width * 0.60);
                pnlContextInspector.Height = (int)(pnlGameBoard.Height * 0.60);
                pnlContextInspector.Location = new Point(
                    (pnlGameBoard.Width - pnlContextInspector.Width) / 2,
                    (pnlGameBoard.Height - pnlContextInspector.Height) / 2
                );

                var btnClose = pnlContextInspector.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "X");
                if (btnClose != null)
                {
                    btnClose.Location = new Point(pnlContextInspector.Width - 30, 8);
                }
            }
        }

        private void ShowContextInspector(Building building)
        {
            // Jeśli zmieniamy budynek — wyczyść sekcję fabryki (może być nieaktualna)
            if (_inspectingBuilding != building)
            {
                var oldSection = pnlContextInspector.Controls.Find("pnlFactorySection", false).FirstOrDefault();
                if (oldSection != null)
                {
                    pnlContextInspector.Controls.Remove(oldSection);
                    oldSection.Dispose();
                }
                _activeRecipeComboBox = null;
            }

            _inspectingBuilding = building;
            pnlContextInspector.Visible = true;
            pnlContextInspector.BringToFront();
            CenterContextInspectorPanel();
            UpdateContextInspector();
        }

        private void CloseContextInspector()
        {
            if (pnlContextInspector != null)
            {
                pnlContextInspector.Visible = false;
            }
        }

        private void UpdateContextInspector()
        {
            if (_inspectingBuilding == null || _company == null) return;

            bool isPlayerOwned = _company.Buildings.Contains(_inspectingBuilding);
            string ownerSuffix = isPlayerOwned ? "(Gracz)" : "(Konkurent)";
            lblContextTitle.Text = $"{_inspectingBuilding.Name} {ownerSuffix}";

            // ──────────────────────────────────────────────
            //  SKLEP DETALICZNY: Specjalny tryb inspektora z półkami
            // ──────────────────────────────────────────────
            if (_inspectingBuilding is RetailBuilding store && isPlayerOwned)
            {
                CenterContextInspectorPanel();

                btnCtxCenter.Location = new Point(20, 90);
                btnCtxCenter.Size = new Size(160, 22);
                btnCtxMarket.Location = new Point(190, 90);
                btnCtxMarket.Size = new Size(160, 22);

                lblContextInv.Location = new Point(410, 130);
                if (pnlContextInvBars != null)
                {
                    pnlContextInvBars.Location = new Point(410, 150);
                    pnlContextInvBars.Size = new Size(pnlContextInspector.Width - 430, pnlContextInspector.Height - 170);
                }

                btnCtxCenter.Visible = true;
                btnCtxMarket.Visible = true;

                UpdateRetailInspector(store);
                
                var retailSection = pnlContextInspector.Controls.Find("pnlRetailSection", false).FirstOrDefault();
                if (retailSection != null)
                {
                    retailSection.Location = new Point(20, 130);
                    retailSection.Size = new Size(380, pnlContextInspector.Height - 150);
                }
                
                return;
            }

            // ──────────────────────────────────────────────
            //  FABRYKA: Specjalny tryb inspektora z przepisami
            // ──────────────────────────────────────────────
            if (_inspectingBuilding is FactoryBuilding factory && isPlayerOwned)
            {
                CenterContextInspectorPanel();

                btnCtxCenter.Location = new Point(20, 150);
                btnCtxCenter.Size = new Size(160, 22);
                btnCtxMarket.Location = new Point(190, 150);
                btnCtxMarket.Size = new Size(160, 22);

                lblContextInv.Location = new Point(360, 90);
                if (pnlContextInvBars != null)
                {
                    pnlContextInvBars.Location = new Point(360, 110);
                    pnlContextInvBars.Size = new Size(pnlContextInspector.Width - 380, pnlContextInspector.Height - 130);
                }

                btnCtxCenter.Visible = true;
                btnCtxMarket.Visible = true;

                UpdateFactoryInspector(factory);
                return;
            }

            // ──────────────────────────────────────────────
            //  STANDARDOWY Inspektor (Extractory, Magazyny)
            // ──────────────────────────────────────────────
            CenterContextInspectorPanel();

            // Usuń kontrolki fabryki jeśli były wcześniej
            var existingFactoryPanel = pnlContextInspector.Controls.Find("pnlFactorySection", false).FirstOrDefault();
            if (existingFactoryPanel != null)
            {
                pnlContextInspector.Controls.Remove(existingFactoryPanel);
                existingFactoryPanel.Dispose();
            }
            _activeRecipeComboBox = null;
            // Przywróć standardowe pozycje (dla okna 60%)
            btnCtxCenter.Location = new Point(20, 90);
            btnCtxCenter.Size = new Size(160, 22);
            btnCtxMarket.Location = new Point(190, 90);
            btnCtxMarket.Size = new Size(160, 22);

            lblContextInv.Location = new Point(20, 130);
            if (pnlContextInvBars != null)
            {
                pnlContextInvBars.Location = new Point(20, 150);
                pnlContextInvBars.Size = new Size(pnlContextInspector.Width - 40, pnlContextInspector.Height - 170);
            }

            btnCtxCenter.Visible = true;
            btnCtxMarket.Visible = isPlayerOwned;

            // 1. Capacity Utilization
            int utilization = 100;
            string utilText = "Wykorzystanie: 100%";

            if (isPlayerOwned)
            {
                if (_inspectingBuilding.GetTotalStock() >= _inspectingBuilding.WarehouseCapacity)
                {
                    utilization = 0;
                    utilText = "Moc: 0% (Magazyn Pełny)";
                }
                else if (_company.Balance < _inspectingBuilding.MaintenanceCost)
                {
                    utilization = 0;
                    utilText = "Moc: 0% (Brak Środków)";
                }
                else if (_inspectingBuilding is WarehouseBuilding)
                {
                    int totalStock = _inspectingBuilding.GetTotalStock();
                    int cap = _inspectingBuilding.WarehouseCapacity;
                    utilization = cap > 0 ? (int)((double)totalStock / cap * 100) : 0;
                    utilText = $"Zapełnienie: {utilization}%";
                }
                else
                {
                    utilText = "Moc: 100% (Praca)";
                }
            }
            else
            {
                utilization = 85;
                utilText = "Moc: 85% (Praca AI)";
            }

            lblContextUtilization.Text = utilText;
            pnlContextUtilProgressFg.Width = (int)((utilization / 100.0) * pnlContextUtilProgressBg.Width);

            // 2. Inventory Status
            int currentStock = _inspectingBuilding.GetTotalStock();
            int maxStock = _inspectingBuilding.WarehouseCapacity;
            lblContextInv.Text = $"Stan Magazynu: {currentStock} / {maxStock} szt.";

            RenderInventoryBars(_inspectingBuilding);

            // 3. Local P&L
            decimal pnlValue = isPlayerOwned
                ? _company.Engine.CalculateFacilityMonthlyPnL(_inspectingBuilding.FacilityId)
                : 4250m;

            lblContextPnL.Text = $"Wynik (P&L): {pnlValue:C}";
            lblContextPnL.ForeColor = pnlValue >= 0 ? Color.FromArgb(100, 220, 100) : Color.FromArgb(240, 80, 80);
        }

        /// <summary>
        /// Specjalny widok inspektora dla FactoryBuilding — pokazuje:
        /// - Stan produkcji (FacilityState)
        /// - Selector przepisu (ComboBox)
        /// - Pasek postępu bieżącego cyklu
        /// - Stany magazynowe surowców wejście/wyjście
        /// - P&L
        /// </summary>
        private void UpdateFactoryInspector(FactoryBuilding factory)
        {
            // ── Stan (wskaźnik kolorowy) ──
            (string stateText, Color stateColor) = factory.State switch
            {
                Conglomerate.Production.FacilityState.Producing        => ("▶ PRODUKUJE", Color.FromArgb(100, 220, 100)),
                Conglomerate.Production.FacilityState.WaitingForInputs  => ("⏸ Brak surowców", Color.FromArgb(240, 180, 50)),
                Conglomerate.Production.FacilityState.OutputStorageFull => ("⏸ Magazyn pełny", Color.FromArgb(240, 80, 80)),
                Conglomerate.Production.FacilityState.InsufficientFunds => ("⛔ Brak środków", Color.FromArgb(240, 80, 80)),
                Conglomerate.Production.FacilityState.Maintenance       => ("🔧 Konserwacja", Color.DimGray),
                _                                                        => ("⏹ Bezczynna", Color.Gray)
            };

            lblContextUtilization.Text = stateText;
            lblContextUtilization.ForeColor = stateColor;

            // Pasek postępu cyklu
            float progress = factory.ProductionProgressNormalized;
            pnlContextUtilProgressFg.Width = (int)(progress * pnlContextUtilProgressBg.Width);
            pnlContextUtilProgressFg.BackColor = stateColor;

            // ── Magazyn ──
            int currentStock = factory.GetTotalStock();
            int maxStock = factory.WarehouseCapacity;
            lblContextInv.Text = $"Magazyn: {currentStock} / {maxStock} szt.";
            RenderInventoryBars(factory);

            // ── Sekcja fabryki (ComboBox + informacje przepisu) ──
            //    Używamy Panel o nazwie "pnlFactorySection" — usuwamy stary i tworzymy nowy przy zmianie budynku
            var oldSection = pnlContextInspector.Controls.Find("pnlFactorySection", false).FirstOrDefault();
            bool needsRebuild = oldSection == null;

            if (needsRebuild)
            {
                if (oldSection != null)
                {
                    pnlContextInspector.Controls.Remove(oldSection);
                    oldSection.Dispose();
                }

                Panel pnlFactorySection = new Panel();
                pnlFactorySection.Name = "pnlFactorySection";
                pnlFactorySection.Location = new Point(15, 92);
                pnlFactorySection.Size = new Size(310, 48);
                pnlFactorySection.BackColor = Color.Transparent;

                Label lblRecipeLabel = new Label();
                lblRecipeLabel.Text = "Przepis:";
                lblRecipeLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                lblRecipeLabel.ForeColor = Color.DarkGray;
                lblRecipeLabel.Location = new Point(0, 5);
                lblRecipeLabel.Size = new Size(55, 18);
                pnlFactorySection.Controls.Add(lblRecipeLabel);

                ComboBox cmbRecipe = new ComboBox();
                cmbRecipe.Name = "cmbRecipe";
                cmbRecipe.Location = new Point(60, 2);
                cmbRecipe.Size = new Size(240, 23);
                cmbRecipe.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbRecipe.BackColor = Color.FromArgb(40, 40, 40);
                cmbRecipe.ForeColor = Color.White;
                cmbRecipe.Font = new Font("Segoe UI", 9);
                cmbRecipe.FlatStyle = FlatStyle.Flat;

                // Opcja "Brak (Idle)"
                cmbRecipe.Items.Add("— Brak (Bezczynna) —");
                foreach (var recipe in factory.AvailableRecipes)
                    cmbRecipe.Items.Add(recipe.DisplayName);

                // Ustaw aktualny wybór
                if (factory.ActiveRecipe == null)
                    cmbRecipe.SelectedIndex = 0;
                else
                {
                    int idx = factory.AvailableRecipes.FindIndex(r => r.Id == factory.ActiveRecipe.Id);
                    cmbRecipe.SelectedIndex = idx >= 0 ? idx + 1 : 0;
                }

                cmbRecipe.SelectedIndexChanged += (s, e) =>
                {
                    int sel = cmbRecipe.SelectedIndex;
                    if (sel <= 0)
                        factory.SetRecipe(null);
                    else
                        factory.SetRecipe(factory.AvailableRecipes[sel - 1]);
                    UpdateContextInspector();
                };

                pnlFactorySection.Controls.Add(cmbRecipe);
                _activeRecipeComboBox = cmbRecipe;

                // Etykieta opisu przepisu
                Label lblRecipeDesc = new Label();
                lblRecipeDesc.Name = "lblRecipeDesc";
                lblRecipeDesc.Font = new Font("Segoe UI", 8, FontStyle.Italic);
                lblRecipeDesc.ForeColor = Color.Gray;
                lblRecipeDesc.Location = new Point(60, 28);
                lblRecipeDesc.Size = new Size(240, 16);
                pnlFactorySection.Controls.Add(lblRecipeDesc);

                pnlContextInspector.Controls.Add(pnlFactorySection);
                pnlContextInspector.Visible = true;
            }

            // Zaktualizuj opis przepisu w istniejącej sekcji
            var section = pnlContextInspector.Controls.Find("pnlFactorySection", false).FirstOrDefault() as Panel;
            if (section != null)
            {
                var descLabel = section.Controls.Find("lblRecipeDesc", false).FirstOrDefault() as Label;
                if (descLabel != null)
                    descLabel.Text = factory.ActiveRecipe?.Description ?? "Wybierz przepis aby uruchomić produkcję";

                // Synchronizuj ComboBox jeśli zmienił się z zewnątrz (bez eventów)
                var cmb = section.Controls.Find("cmbRecipe", false).FirstOrDefault() as ComboBox;
                if (cmb != null && _activeRecipeComboBox == cmb)
                {
                    int expectedIdx = factory.ActiveRecipe == null ? 0
                        : factory.AvailableRecipes.FindIndex(r => r.Id == factory.ActiveRecipe.Id) + 1;
                    if (cmb.SelectedIndex != expectedIdx)
                        cmb.SelectedIndex = expectedIdx;
                }
            }

            // ── P&L ──
            decimal pnlValue = _company!.Engine.CalculateFacilityMonthlyPnL(factory.FacilityId);
            lblContextPnL.Text = $"Wynik (P&L): {pnlValue:C}";
            lblContextPnL.ForeColor = pnlValue >= 0 ? Color.FromArgb(100, 220, 100) : Color.FromArgb(240, 80, 80);

            // ── Cykle ukończone ──
            string cycleInfo = $"Cykle: {factory.TotalCyclesCompleted}";
            if (factory.ActiveRecipe != null)
                cycleInfo += $"  |  Czas cyklu: {factory.ActiveRecipe.CycleDurationHours}h";
            lblContextTitle.Text = $"{factory.Name} (Gracz)  —  {cycleInfo}";
        }

        /// <summary>Renderuje fill-bary dla każdego surowca w magazynie budynku.</summary>
        private void RenderInventoryBars(Building building)
        {
            pnlContextInvBars.AutoScroll = true;
            int barY = 0;
            int maxStock = building.WarehouseCapacity;
            bool isPlayerOwned = _company != null && _company.Buildings.Contains(building);

            var resources = building.Warehouse.ToList();
            
            // Check if we need to rebuild controls
            int expectedControls = isPlayerOwned ? 7 : 3;
            bool needsRebuild = pnlContextInvBars.Controls.Count != resources.Count * expectedControls;

            if (needsRebuild)
            {
                pnlContextInvBars.Controls.Clear();
                foreach (var kvp in resources)
                {
                    string resName = kvp.Key;
                    decimal price = building.ResourcePrices.ContainsKey(resName) ? building.ResourcePrices[resName] : 0m;

                    Label lblRes = new Label();
                    lblRes.Name = $"lblRes_{resName}";
                    lblRes.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                    lblRes.ForeColor = Color.LightGray;
                    lblRes.Location = new Point(5, barY + 3);
                    lblRes.Size = new Size(110, 15);
                    pnlContextInvBars.Controls.Add(lblRes);

                    Panel pnlBg = new Panel();
                    pnlBg.Name = $"pnlBg_{resName}";
                    pnlBg.Location = new Point(120, barY + 5);
                    pnlBg.Size = new Size(80, 10);
                    pnlBg.BackColor = Color.FromArgb(45, 45, 45);
                    pnlContextInvBars.Controls.Add(pnlBg);

                    Panel pnlFg = new Panel();
                    pnlFg.Name = $"pnlFg_{resName}";
                    pnlFg.Location = new Point(0, 0);
                    pnlFg.Size = new Size(0, 10);
                    pnlFg.BackColor = Color.FromArgb(100, 220, 100);
                    pnlBg.Controls.Add(pnlFg);

                    if (isPlayerOwned)
                    {
                        TextBox txtSellQty = new TextBox();
                        txtSellQty.Name = $"txtSellQty_{resName}";
                        txtSellQty.Location = new Point(210, barY);
                        txtSellQty.Size = new Size(50, 20);
                        txtSellQty.BackColor = Color.FromArgb(45, 45, 45);
                        txtSellQty.ForeColor = Color.White;
                        txtSellQty.BorderStyle = BorderStyle.FixedSingle;
                        txtSellQty.TextChanged += (s, e) => { _enteredSellQuantities[resName] = txtSellQty.Text; };
                        pnlContextInvBars.Controls.Add(txtSellQty);

                        Button btnAll = new Button();
                        btnAll.Name = $"btnAll_{resName}";
                        btnAll.Text = "ALL";
                        btnAll.Location = new Point(265, barY - 1);
                        btnAll.Size = new Size(35, 22);
                        btnAll.FlatStyle = FlatStyle.Flat;
                        btnAll.BackColor = Color.FromArgb(35, 35, 35);
                        btnAll.ForeColor = Color.FromArgb(50, 150, 250);
                        btnAll.Click += (s, e) => { 
                            var t = pnlContextInvBars.Controls.Find($"txtSellQty_{resName}", false).FirstOrDefault() as TextBox;
                            if (t != null) t.Text = building.Warehouse[resName].ToString(); 
                        };
                        pnlContextInvBars.Controls.Add(btnAll);

                        Button btnSell = new Button();
                        btnSell.Name = $"btnSell_{resName}";
                        btnSell.Location = new Point(305, barY - 1);
                        btnSell.Size = new Size(110, 22);
                        btnSell.FlatStyle = FlatStyle.Flat;
                        btnSell.BackColor = Color.FromArgb(100, 220, 100);
                        btnSell.ForeColor = Color.White;
                        btnSell.Click += (s, e) => { 
                            var t = pnlContextInvBars.Controls.Find($"txtSellQty_{resName}", false).FirstOrDefault() as TextBox;
                            if (t != null && int.TryParse(t.Text, out int sellQty) && sellQty > 0)
                            {
                                if (building.SellResource(resName, sellQty, _company!, _gameManager!.CurrentDay, _gameManager!.CurrentHour))
                                {
                                    lblBottomStatus.Text = $"Sprzedano {sellQty} szt. {resName} za {(price * sellQty):C}!";
                                    _enteredSellQuantities[resName] = "0";
                                    t.Text = "0";
                                    RefreshStats();
                                }
                                else
                                {
                                    lblBottomStatus.Text = "❌ Błąd: Niepoprawna ilość do sprzedaży!";
                                }
                            }
                        };
                        pnlContextInvBars.Controls.Add(btnSell);

                        CheckBox chkAuto = new CheckBox();
                        chkAuto.Name = $"chkAuto_{resName}";
                        chkAuto.Text = "Auto";
                        chkAuto.Location = new Point(420, barY);
                        chkAuto.Size = new Size(50, 20);
                        chkAuto.ForeColor = Color.FromArgb(100, 220, 100);
                        chkAuto.CheckedChanged += (s, e) => {
                            if (chkAuto.Checked) {
                                building.AutoSellResources.Add(resName);
                                building.AutoSell = true;
                            } else {
                                building.AutoSellResources.Remove(resName);
                                if (building.AutoSellResources.Count == 0) building.AutoSell = false;
                            }
                        };
                        pnlContextInvBars.Controls.Add(chkAuto);
                    }
                    barY += 25;
                }
            }

            // Update existing controls without recreating them
            foreach (var kvp in resources)
            {
                string resName = kvp.Key;
                int qty = kvp.Value;
                decimal price = building.ResourcePrices.ContainsKey(resName) ? building.ResourcePrices[resName] : 0m;

                var lbl = pnlContextInvBars.Controls.Find($"lblRes_{resName}", false).FirstOrDefault() as Label;
                if (lbl != null) lbl.Text = $"{resName}: {qty}";

                var pnlBg = pnlContextInvBars.Controls.Find($"pnlBg_{resName}", false).FirstOrDefault() as Panel;
                if (pnlBg != null && pnlBg.Controls.Count > 0)
                {
                    var pnlFg = pnlBg.Controls[0] as Panel;
                    if (pnlFg != null)
                    {
                        int fgWidth = maxStock > 0 ? (int)((double)qty / maxStock * 80) : 0;
                        pnlFg.Size = new Size(Math.Min(80, fgWidth), 10);
                    }
                }

                if (isPlayerOwned)
                {
                    var btnSell = pnlContextInvBars.Controls.Find($"btnSell_{resName}", false).FirstOrDefault() as Button;
                    if (btnSell != null) btnSell.Text = $"Sprzedaj ({price:C})";

                    var chkAuto = pnlContextInvBars.Controls.Find($"chkAuto_{resName}", false).FirstOrDefault() as CheckBox;
                    if (chkAuto != null) chkAuto.Checked = building.AutoSellResources.Contains(resName);
                    
                    var txtSellQty = pnlContextInvBars.Controls.Find($"txtSellQty_{resName}", false).FirstOrDefault() as TextBox;
                    if (txtSellQty != null && !txtSellQty.Focused)
                    {
                        if (_enteredSellQuantities.ContainsKey(resName))
                            txtSellQty.Text = _enteredSellQuantities[resName];
                        else
                            txtSellQty.Text = "0";
                    }
                }
            }
        }


        private Button CreateShortcutButton(string iconText, int yPos, Color accentColor, string tooltipText, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = iconText;
            btn.AccessibleName = tooltipText;
            btn.Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold);
            btn.Size = new Size(46, 46);
            btn.Location = new Point(7, yPos);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 40);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 35);
            btn.BackColor = Color.FromArgb(25, 25, 25);
            btn.ForeColor = accentColor;
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            _toolTip.SetToolTip(btn, tooltipText);
            return btn;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                // 1. Jeśli otwarte są panele szczegółowe lub modalne, zamknij je w pierwszej kolejności
                if (pnlFinanceReport.Visible || pnlSaveGameOverlay.Visible || pnlEscapeMenu.Visible || pnlContextInspector.Visible || pnlLogisticsManager.Visible || pnlMarketBuyer.Visible)
                {
                    if (pnlEscapeMenu.Visible)
                    {
                        ToggleEscapeMenu();
                        return;
                    }
                    CloseFinanceReport();
                    CloseContextInspector();
                    pnlLogisticsManager.Visible = false;
                    pnlMarketBuyer.Visible = false;
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
                CloseContextInspector(); // Zamknij szczegóły budynku jeśli były otwarte
                OpenFinanceReport();
            }
        }

        private void OpenFinanceReport()
        {
            HideAllOverlays(pnlFinanceReport);
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
            btnClose.AccessibleName = "Zamknij raport";
            btnClose.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnClose.Size = new Size(25, 25);
            btnClose.Location = new Point(pnlFinanceReport.Width - 35, 10);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.Gray;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => CloseFinanceReport();
            _toolTip.SetToolTip(btnClose, "Zamknij raport");
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
                    else if (bData.Type == "CopperMine")
                    {
                        building = new CopperMine(bData.Name);
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
                    building.X = bData.X;
                    building.Y = bData.Y;
                    _map.BuildBuildingOnTile(bData.X, bData.Y, building);
                }

                // 4. Recreate GameManager & restore day/hour
                _gameManager = new GameManager(_company, _map);
                _gameManager.RestoreState(state.CurrentDay, state.CurrentHour);
                if (state.SupplyRoutes != null)
                {
                    _gameManager.Logistics.RestoreRoutes(state.SupplyRoutes);
                }

                // 5. Re-subscribe events
                _gameManager.OnTickPerformed += OnTickPerformed;

                // 6. Initialize Map Control
                mapControl.Initialize(_map, _gameManager);

                // 7. Transition to Playing State
                ChangeMenuState(MenuState.Playing);

                // 8. Domyślne uruchomienie gry z prędkością 1x
                SetGameSpeed(1000, btnSpeed1x);

                // 9. Refresh stats & UI
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
                HideAllOverlays(pnlEscapeMenu);
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

        // ═══════════════════════════════════════════════════════════════════
        //  SYSTEM LOGISTYCZNY — UI
        // ═══════════════════════════════════════════════════════════════════

        private void InitializeLogisticsManagerPanel()
        {
            pnlLogisticsManager = new Panel();
            pnlLogisticsManager.Size = new Size(850, 580);
            pnlLogisticsManager.BackColor = Color.FromArgb(22, 22, 22);
            pnlLogisticsManager.BorderStyle = BorderStyle.FixedSingle;
            pnlLogisticsManager.Visible = false;
            pnlGameBoard.Controls.Add(pnlLogisticsManager);
            pnlLogisticsManager.BringToFront();
        }

        private void InitializeMarketBuyerPanel()
        {
            pnlMarketBuyer = new Panel();
            pnlMarketBuyer.Size = new Size(600, 420);
            pnlMarketBuyer.BackColor = Color.FromArgb(22, 22, 22);
            pnlMarketBuyer.BorderStyle = BorderStyle.FixedSingle;
            pnlMarketBuyer.Visible = false;
            pnlGameBoard.Controls.Add(pnlMarketBuyer);
            pnlMarketBuyer.BringToFront();
        }

        /// <summary>Centruje dowolny panel na planszy.</summary>
        private void CenterPanel(Panel? panel)
        {
            if (panel == null || pnlGameBoard == null) return;
            panel.Location = new Point(
                (pnlGameBoard.Width - panel.Width) / 2,
                (pnlGameBoard.Height - panel.Height) / 2);
        }

        // ───────────────────────────────────────────────────────────────────
        //  PANEL ZARZĄDZANIA TRASAMI LOGISTYCZNYMI (GLOBALNY)
        // ───────────────────────────────────────────────────────────────────

        private void ToggleLogisticsManagerPanel()
        {
            if (_gameManager == null || _company == null) return;

            if (pnlLogisticsManager.Visible)
            {
                pnlLogisticsManager.Visible = false;
                _editingRoute = null;
                if (_activeSpeedButton != btnSpeedPause && _currentMenuState == MenuState.Playing)
                    _gameTimer.Start();
            }
            else
            {
                OpenLogisticsManagerGlobal();
            }
        }

        private void OpenLogisticsManagerGlobal()
        {
            if (_gameManager == null || _company == null) return;

            _gameTimer.Stop();
            HideAllOverlays(pnlLogisticsManager);
            _editingRoute = null;
            BuildLogisticsPanelGlobal();
            CenterPanel(pnlLogisticsManager);
            pnlLogisticsManager.Visible = true;
            pnlLogisticsManager.BringToFront();
        }

        private void BuildLogisticsPanelGlobal()
        {
            pnlLogisticsManager.Controls.Clear();

            // Górna linia akcentu
            var topLine = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Color.FromArgb(240, 140, 20) };
            pnlLogisticsManager.Controls.Add(topLine);

            // Tytuł
            var lblTitle = new Label
            {
                Text = "🚚  LOGISTYKA I FLOTA IMPERIUM",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 180, 50),
                Location = new Point(15, 15),
                Size = new Size(500, 24)
            };
            pnlLogisticsManager.Controls.Add(lblTitle);

            // Przycisk zamknięcia
            var btnClose = new Button
            {
                Text = "✕",
                AccessibleName = "Zamknij panel logistyki",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(30, 28),
                Location = new Point(810, 10),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                pnlLogisticsManager.Visible = false;
                _editingRoute = null;
                if (_activeSpeedButton != btnSpeedPause && _currentMenuState == MenuState.Playing)
                    _gameTimer.Start();
            };
            _toolTip.SetToolTip(btnClose, "Zamknij panel logistyki");
            pnlLogisticsManager.Controls.Add(btnClose);

            int activeTripsCount = _gameManager!.Logistics.ActiveTrips.Count;
            int totalFleet = _gameManager.Logistics.TotalFleetSize;

            // Nagłówek lewej kolumny
            var lblLeftHeader = new Label
            {
                Text = $"AKTYWNE TRASY (AKTYWNE DOSTAWY: {activeTripsCount} / {totalFleet})",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.DarkGray,
                Location = new Point(15, 50),
                Size = new Size(490, 20)
            };
            pnlLogisticsManager.Controls.Add(lblLeftHeader);

            // Kontener listy tras (scrollable)
            var pnlRoutesList = new Panel
            {
                Location = new Point(15, 75),
                Size = new Size(490, 440),
                AutoScroll = true,
                BackColor = Color.FromArgb(16, 16, 16),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLogisticsManager.Controls.Add(pnlRoutesList);

            var routes = _gameManager.Logistics.Routes;
            int routeY = 5;

            var buildingMap = _company!.Buildings.ToDictionary(b => b.FacilityId, b => b);

            if (routes.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Brak aktywnych tras. Utwórz trasę w panelu po prawej stronie.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(10, 20),
                    Size = new Size(470, 40),
                    TextAlign = ContentAlignment.TopCenter
                };
                pnlRoutesList.Controls.Add(lblEmpty);
            }
            else
            {
                foreach (var route in routes)
                {
                    // Wiersz trasy
                    var pnlRow = new Panel
                    {
                        Location = new Point(5, routeY),
                        Size = new Size(460, 65),
                        BackColor = Color.FromArgb(28, 28, 28)
                    };

                    // Wyznaczenie nazwy dostawcy i odbiorcy
                    string sourceName = route.SourceType == RouteSourceType.Market
                        ? "🏪 Rynek"
                        : (buildingMap.TryGetValue(route.SourceFacilityId, out var src) ? $"🏭 {src.Name}" : "?");
                    string destName = buildingMap.TryGetValue(route.TargetFacilityId, out var dst) ? dst.Name : "?";

                    // Wyznaczenie statusu
                    string statusText = "Oczekiwanie";
                    Color statusColor = Color.Gray;

                    if (!route.IsEnabled)
                    {
                        statusText = "Wstrzymana";
                        statusColor = Color.DarkGray;
                    }
                    else if (route.LastTripResult.Contains("W drodze"))
                    {
                        statusText = "Aktywna (W drodze)";
                        statusColor = Color.FromArgb(100, 220, 100);
                    }
                    else if (route.LastTripResult.Contains("Brak środków"))
                    {
                        statusText = "Brak środków (Głód)";
                        statusColor = Color.FromArgb(240, 80, 80);
                    }
                    else if (route.LastTripResult.Contains("Oczekiwanie na wolną flotę"))
                    {
                        statusText = "Kolejka (Brak floty)";
                        statusColor = Color.FromArgb(240, 180, 50);
                    }
                    else if (route.LastTripResult.Contains("Oczekiwanie na towar") || route.LastTripResult.Contains("Magazyn docelowy pełny"))
                    {
                        statusText = "Oczekiwanie na towar (Idle)";
                        statusColor = Color.FromArgb(200, 180, 50);
                    }
                    else if (route.LastTripResult.StartsWith("✅"))
                    {
                        statusText = "Aktywna (Zgodna z budżetem)";
                        statusColor = Color.FromArgb(100, 220, 100);
                    }
                    else
                    {
                        statusText = route.LastTripResult;
                        statusColor = Color.LightGray;
                    }

                    var lblRouteTitle = new Label
                    {
                        Text = $"{sourceName}  ➔  {destName}",
                        Font = new Font("Segoe UI", 9, FontStyle.Bold),
                        ForeColor = Color.White,
                        Location = new Point(8, 6),
                        Size = new Size(320, 18),
                        AutoEllipsis = true
                    };
                    pnlRow.Controls.Add(lblRouteTitle);

                    string vehicleLabel = route.VehicleTypeName == "Van" ? "Van" : "Truck";
                    string loadRuleText = route.LoadRule switch
                    {
                        LoadThresholdRule.FullOnly => "Tylko pełny",
                        LoadThresholdRule.MinCargo50 => "Min 50%",
                        _ => $"Timer ({route.IntervalHours}h)"
                    };

                    var lblRouteDetails = new Label
                    {
                        Text = $"{route.ResourceName} ×{route.AmountPerTrip} | {vehicleLabel} ({loadRuleText}) | Priorytet: {route.Priority}",
                        Font = new Font("Segoe UI", 8f),
                        ForeColor = Color.LightGray,
                        Location = new Point(8, 24),
                        Size = new Size(320, 16)
                    };
                    pnlRow.Controls.Add(lblRouteDetails);

                    var lblStatus = new Label
                    {
                        Text = $"● {statusText}",
                        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                        ForeColor = statusColor,
                        Location = new Point(8, 42),
                        Size = new Size(320, 16)
                    };
                    pnlRow.Controls.Add(lblStatus);

                    // Przyciski akcji na wierszu
                    var btnEdit = new Button
                    {
                        Text = "✏",
                        AccessibleName = "Edytuj trasę",
                        Font = new Font("Segoe UI", 8.5f),
                        Size = new Size(24, 24),
                        Location = new Point(365, 20),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(50, 150, 250),
                        BackColor = Color.FromArgb(40, 40, 40),
                        Cursor = Cursors.Hand
                    };
                    btnEdit.FlatAppearance.BorderSize = 0;
                    btnEdit.Click += (s, e) =>
                    {
                        _editingRoute = route;
                        BuildLogisticsPanelGlobal();
                    };
                    _toolTip.SetToolTip(btnEdit, "Edytuj trasę");
                    pnlRow.Controls.Add(btnEdit);

                    var btnToggle = new Button
                    {
                        Text = route.IsEnabled ? "⏸" : "▶",
                        AccessibleName = route.IsEnabled ? "Wstrzymaj trasę" : "Wznów trasę",
                        Font = new Font("Segoe UI", 9),
                        Size = new Size(24, 24),
                        Location = new Point(395, 20),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = route.IsEnabled ? Color.FromArgb(100, 220, 100) : Color.Gray,
                        BackColor = Color.FromArgb(40, 40, 40),
                        Cursor = Cursors.Hand
                    };
                    btnToggle.FlatAppearance.BorderSize = 0;
                    btnToggle.Click += (s, e) =>
                    {
                        route.IsEnabled = !route.IsEnabled;
                        BuildLogisticsPanelGlobal();
                    };
                    _toolTip.SetToolTip(btnToggle, route.IsEnabled ? "Wstrzymaj trasę" : "Wznów trasę");
                    pnlRow.Controls.Add(btnToggle);

                    var btnRemove = new Button
                    {
                        Text = "✕",
                        AccessibleName = "Usuń trasę",
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        Size = new Size(24, 24),
                        Location = new Point(425, 20),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.FromArgb(240, 80, 80),
                        BackColor = Color.FromArgb(40, 40, 40),
                        Cursor = Cursors.Hand
                    };
                    btnRemove.FlatAppearance.BorderSize = 0;
                    string routeId = route.Id;
                    btnRemove.Click += (s, e) =>
                    {
                        var confirm = MessageBox.Show("Czy na pewno chcesz usunąć tę trasę?", "Usuwanie trasy", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (confirm == DialogResult.Yes)
                        {
                            _gameManager.Logistics.RemoveRoute(routeId);
                            if (_editingRoute?.Id == routeId) _editingRoute = null;
                            BuildLogisticsPanelGlobal();
                        }
                    };
                    pnlRow.Controls.Add(btnRemove);

                    pnlRoutesList.Controls.Add(pnlRow);
                    routeY += 70;
                }
            }

            // Przycisk "Dodaj nową trasę" na dole lewej kolumny
            var btnAddNewRoute = new Button
            {
                Text = "➕  DODAJ NOWĄ TRASĘ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(15, 528),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 150, 250),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAddNewRoute.FlatAppearance.BorderSize = 0;
            btnAddNewRoute.Click += (s, e) =>
            {
                _editingRoute = null;
                BuildLogisticsPanelGlobal();
            };
            pnlLogisticsManager.Controls.Add(btnAddNewRoute);

            // ──────────────────────────────────────────────
            //  PRAWA KOLUMNA: EDYTOR TRASY
            // ──────────────────────────────────────────────
            var lblRightHeader = new Label
            {
                Text = _editingRoute == null ? "KREATOR NOWEJ TRASY" : "EDYCJA TRASY (USTAWIENIA)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = _editingRoute == null ? Color.FromArgb(50, 150, 250) : Color.FromArgb(240, 180, 50),
                Location = new Point(520, 50),
                Size = new Size(310, 20)
            };
            pnlLogisticsManager.Controls.Add(lblRightHeader);

            var pnlEditor = new Panel
            {
                Location = new Point(520, 75),
                Size = new Size(310, 489),
                BackColor = Color.FromArgb(22, 22, 22)
            };
            pnlLogisticsManager.Controls.Add(pnlEditor);

            // Kontrolki edytora
            int editY = 5;

            // 1. Dostawca (Source)
            var lblSource = new Label { Text = "Dostawca (Źródło):", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(300, 16) };
            pnlEditor.Controls.Add(lblSource);
            editY += 18;

            var cmbSource = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(295, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbSource.Items.Add("🏪 Wolny Rynek");
            foreach (var b in _company.Buildings)
            {
                cmbSource.Items.Add(b);
            }
            cmbSource.DisplayMember = "Name";
            pnlEditor.Controls.Add(cmbSource);
            editY += 30;

            // 2. Odbiorca (Destination)
            var lblDest = new Label { Text = "Odbiorca (Cel):", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(300, 16) };
            pnlEditor.Controls.Add(lblDest);
            editY += 18;

            var cmbDest = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(295, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            foreach (var b in _company.Buildings)
            {
                cmbDest.Items.Add(b);
            }
            cmbDest.DisplayMember = "Name";
            pnlEditor.Controls.Add(cmbDest);
            editY += 30;

            // 3. Towar (Resource)
            var lblResource = new Label { Text = "Towar:", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(300, 16) };
            pnlEditor.Controls.Add(lblResource);
            editY += 18;

            var cmbResource = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(295, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            pnlEditor.Controls.Add(cmbResource);
            editY += 30;

            // Helper to populate resources based on destination
            Action updateResources = () =>
            {
                cmbResource.Items.Clear();
                if (cmbDest.SelectedItem is Building destBuilding)
                {
                    if (destBuilding is FactoryBuilding fb && fb.ActiveRecipe != null)
                    {
                        foreach (var inputRes in fb.ActiveRecipe.Inputs.Keys)
                        {
                            cmbResource.Items.Add(inputRes);
                        }
                    }
                    else
                    {
                        // Warehouse or Retail - list all possible listings
                        foreach (var resKey in _gameManager.Market.Listings.Keys)
                        {
                            cmbResource.Items.Add(resKey);
                        }
                    }
                }
                if (cmbResource.Items.Count > 0)
                {
                    cmbResource.SelectedIndex = 0;
                }
            };

            cmbDest.SelectedIndexChanged += (s, e) => updateResources();

            // 4. Środek transportu (Vehicle Type)
            var lblVehicle = new Label { Text = "Środek transportu:", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(300, 16) };
            pnlEditor.Controls.Add(lblVehicle);
            editY += 18;

            var cmbVehicle = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(295, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            foreach (var vt in VehicleRegistry.VehicleTypes)
            {
                cmbVehicle.Items.Add(vt.Name);
            }
            if (cmbVehicle.Items.Count > 0) cmbVehicle.SelectedIndex = 0;
            pnlEditor.Controls.Add(cmbVehicle);
            editY += 30;

            // 5. Reguła załadunku (Load Threshold Rule)
            var lblLoadRule = new Label { Text = "Reguła załadunku:", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(300, 16) };
            pnlEditor.Controls.Add(lblLoadRule);
            editY += 18;

            var cmbLoadRule = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(295, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbLoadRule.Items.Add("Wysyłaj co określony czas (Timer)");
            cmbLoadRule.Items.Add("Tylko pełny załadunek (100%)");
            cmbLoadRule.Items.Add("Min. załadunek 50%");
            cmbLoadRule.SelectedIndex = 0;
            pnlEditor.Controls.Add(cmbLoadRule);
            editY += 30;

            // 6. Parametry (Timer & Ilość) - side by side
            var lblInterval = new Label { Text = "Co (godzin):", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(140, 16) };
            pnlEditor.Controls.Add(lblInterval);

            var lblAmount = new Label { Text = "Ilość/kurs:", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(155, editY), Size = new Size(140, 16) };
            pnlEditor.Controls.Add(lblAmount);
            editY += 18;

            var numInterval = new NumericUpDown
            {
                Location = new Point(0, editY),
                Size = new Size(140, 23),
                Minimum = 1, Maximum = 168, Value = 24,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            pnlEditor.Controls.Add(numInterval);

            var numAmount = new NumericUpDown
            {
                Location = new Point(155, editY),
                Size = new Size(140, 23),
                Minimum = 1, Maximum = 60, Value = 10,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            pnlEditor.Controls.Add(numAmount);
            editY += 30;

            // 7. Parametry (Priorytet i automatyczny koszt) - side by side
            var lblPriority = new Label { Text = "Priorytet trasy:", Font = new Font("Segoe UI", 8f), ForeColor = Color.DarkGray, Location = new Point(0, editY), Size = new Size(140, 16) };
            pnlEditor.Controls.Add(lblPriority);

            var lblCalculatedUnitCost = new Label { Text = "Koszt jedn. (auto):", Font = new Font("Segoe UI", 7.5f), ForeColor = Color.LightGray, Location = new Point(155, editY), Size = new Size(140, 32) };
            pnlEditor.Controls.Add(lblCalculatedUnitCost);
            editY += 18;

            var cmbPriority = new ComboBox
            {
                Location = new Point(0, editY),
                Size = new Size(140, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbPriority.Items.Add("Niski");
            cmbPriority.Items.Add("Średni");
            cmbPriority.Items.Add("Wysoki");
            cmbPriority.SelectedIndex = 1;
            pnlEditor.Controls.Add(cmbPriority);
            editY += 30;

            // 8. Podgląd OPEX
            var pnlOpex = new Panel
            {
                Location = new Point(0, editY),
                Size = new Size(295, 65),
                BackColor = Color.FromArgb(28, 28, 28)
            };
            pnlEditor.Controls.Add(pnlOpex);

            var lblOpexBase = new Label
            {
                Text = "Koszt pojazdu (OPEX): 40.00 zł",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.LightGray,
                Location = new Point(8, 6),
                Size = new Size(280, 16)
            };
            pnlOpex.Controls.Add(lblOpexBase);

            var lblOpexTotal = new Label
            {
                Text = "Szacowany koszt kursu: 140.00 zł",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 180, 50),
                Location = new Point(8, 26),
                Size = new Size(280, 16)
            };
            pnlOpex.Controls.Add(lblOpexTotal);

            var lblOpexWarning = new Label
            {
                Text = "* Kupując na rynku doliczana jest cena rynkowa",
                Font = new Font("Segoe UI", 7f, FontStyle.Italic),
                ForeColor = Color.DarkGray,
                Location = new Point(8, 44),
                Size = new Size(280, 14)
            };
            pnlOpex.Controls.Add(lblOpexWarning);
            editY += 75;

            // Helper to get distance & unit cost
            Func<(double Distance, decimal UnitCost)> getRouteCostDetails = () =>
            {
                if (cmbDest.SelectedItem == null) return (0.0, 0.0m);
                var destBuilding = (Building)cmbDest.SelectedItem;

                double distance = 6.0;
                if (cmbSource.SelectedIndex > 0 && cmbSource.SelectedItem is Building srcBuilding)
                {
                    distance = Math.Sqrt(Math.Pow(destBuilding.X - srcBuilding.X, 2) + Math.Pow(destBuilding.Y - srcBuilding.Y, 2));
                }
                
                decimal uCost = Math.Max(0.5m, Math.Round((decimal)distance * 0.3m, 1));
                return (distance, uCost);
            };

            // Funkcja aktualizacji parametrów pojazdu i kosztów
            Action updateOpexPreview = () =>
            {
                if (cmbVehicle.SelectedItem == null) return;
                string vName = cmbVehicle.SelectedItem.ToString()!;
                var config = VehicleRegistry.Get(vName);
                
                // Dostosuj maksymalną ilość ładunku do pojemności pojazdu
                numAmount.Maximum = config.Capacity;
                if (numAmount.Value > config.Capacity) numAmount.Value = config.Capacity;

                var (dist, unitCost) = getRouteCostDetails();
                decimal baseCost = config.BaseCostPerTrip;
                decimal totalCost = baseCost + (numAmount.Value * unitCost);

                lblCalculatedUnitCost.Text = $"Koszt jedn. (auto):\n{unitCost:F1} zł/szt. (Dyst: {dist:F1})";
                lblOpexBase.Text = $"Koszt pojazdu (OPEX): {baseCost:F2} zł [Pojemność: {config.Capacity}]";
                lblOpexTotal.Text = $"Szacowany koszt kursu: {totalCost:F2} zł";
            };

            cmbVehicle.SelectedIndexChanged += (s, e) => updateOpexPreview();
            numAmount.UpDownAlign = LeftRightAlignment.Right; // Standard alignment
            numAmount.ValueChanged += (s, e) => updateOpexPreview();
            cmbSource.SelectedIndexChanged += (s, e) => updateOpexPreview();

            // Ustaw enable/disable dla parametrów na podstawie reguły załadunku
            cmbLoadRule.SelectedIndexChanged += (s, e) =>
            {
                bool isTimer = cmbLoadRule.SelectedIndex == 0;
                numInterval.Enabled = isTimer;
                if (cmbVehicle.SelectedItem != null)
                {
                    var config = VehicleRegistry.Get(cmbVehicle.SelectedItem.ToString()!);
                    if (cmbLoadRule.SelectedIndex == 1) // FullOnly
                    {
                        numAmount.Value = config.Capacity;
                        numAmount.Enabled = false;
                    }
                    else
                    {
                        numAmount.Enabled = true;
                    }
                }
                updateOpexPreview();
            };

            // Przycisk Zapisu / Utworzenia
            var btnSave = new Button
            {
                Text = _editingRoute == null ? "➕  DODAJ TRASĘ" : "💾  ZAPISZ ZMIANY",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, editY),
                Size = new Size(295, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = _editingRoute == null ? Color.FromArgb(50, 150, 250) : Color.FromArgb(240, 180, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (cmbDest.SelectedItem == null || cmbResource.SelectedItem == null || cmbVehicle.SelectedItem == null)
                {
                    MessageBox.Show("Upewnij się, że wszystkie pola zostały wypełnione poprawnie.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var targetBuilding = (Building)cmbDest.SelectedItem;
                
                string sourceId = string.Empty;
                string sourceDisplayName = string.Empty;
                RouteSourceType sourceType = RouteSourceType.Market;

                if (cmbSource.SelectedIndex > 0 && cmbSource.SelectedItem is Building srcBuilding)
                {
                    if (srcBuilding.FacilityId == targetBuilding.FacilityId)
                    {
                        MessageBox.Show("Dostawca i odbiorca nie mogą być tym samym budynkiem.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    sourceType = RouteSourceType.Building;
                    sourceId = srcBuilding.FacilityId;
                    sourceDisplayName = srcBuilding.Name;
                }
                else
                {
                    sourceType = RouteSourceType.Market;
                    sourceDisplayName = "Wolny Rynek";
                }

                var loadRule = cmbLoadRule.SelectedIndex switch
                {
                    1 => LoadThresholdRule.FullOnly,
                    2 => LoadThresholdRule.MinCargo50,
                    _ => LoadThresholdRule.TimerOnly
                };

                var priority = cmbPriority.SelectedIndex switch
                {
                    0 => RoutePriority.Low,
                    2 => RoutePriority.High,
                    _ => RoutePriority.Medium
                };

                // Automatyczne wyliczenie kosztu transportu
                var (_, transportCost) = getRouteCostDetails();

                if (_editingRoute == null)
                {
                    // Tworzenie nowej trasy
                    var newRoute = new SupplyRoute
                    {
                        SourceType = sourceType,
                        SourceFacilityId = sourceId,
                        TargetFacilityId = targetBuilding.FacilityId,
                        ResourceName = cmbResource.SelectedItem.ToString()!,
                        AmountPerTrip = (int)numAmount.Value,
                        IntervalHours = (int)numInterval.Value,
                        TransportCostPerUnit = transportCost,
                        VehicleTypeName = cmbVehicle.SelectedItem.ToString()!,
                        LoadRule = loadRule,
                        Priority = priority,
                        IsEnabled = true
                    };
                    _gameManager.Logistics.AddRoute(newRoute);
                    MessageBox.Show("Trasa logistyczna została dodana pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Edycja istniejącej trasy
                    _editingRoute.SourceType = sourceType;
                    _editingRoute.SourceFacilityId = sourceId;
                    _editingRoute.TargetFacilityId = targetBuilding.FacilityId;
                    _editingRoute.ResourceName = cmbResource.SelectedItem.ToString()!;
                    _editingRoute.AmountPerTrip = (int)numAmount.Value;
                    _editingRoute.IntervalHours = (int)numInterval.Value;
                    _editingRoute.TransportCostPerUnit = transportCost;
                    _editingRoute.VehicleTypeName = cmbVehicle.SelectedItem.ToString()!;
                    _editingRoute.LoadRule = loadRule;
                    _editingRoute.Priority = priority;
                    _editingRoute.HoursSinceLastTrip = 0; // zresetuj timer
                    _editingRoute.LastTripResult = "Zaktualizowano";
                    MessageBox.Show("Zmiany w trasie zostały zapisane.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                _editingRoute = null;
                BuildLogisticsPanelGlobal();
            };
            pnlEditor.Controls.Add(btnSave);

            // Wypełnij formularz wartościami, jeśli edytujemy trasę
            var routeToEdit = _editingRoute;
            if (routeToEdit != null)
            {
                // Dopasuj dostawcę
                if (routeToEdit.SourceType == RouteSourceType.Market)
                {
                    cmbSource.SelectedIndex = 0;
                }
                else
                {
                    for (int i = 1; i < cmbSource.Items.Count; i++)
                    {
                        if (cmbSource.Items[i] is Building b && b.FacilityId == routeToEdit.SourceFacilityId)
                        {
                            cmbSource.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Dopasuj odbiorcę
                for (int i = 0; i < cmbDest.Items.Count; i++)
                {
                    if (cmbDest.Items[i] is Building b && b.FacilityId == routeToEdit.TargetFacilityId)
                    {
                        cmbDest.SelectedIndex = i;
                        break;
                    }
                }

                // Pobierz towary
                updateResources();

                // Dopasuj towar
                for (int i = 0; i < cmbResource.Items.Count; i++)
                {
                    var item = cmbResource.Items[i];
                    if (item != null && item.ToString() == routeToEdit.ResourceName)
                    {
                        cmbResource.SelectedIndex = i;
                        break;
                    }
                }

                // Dopasuj pojazd
                for (int i = 0; i < cmbVehicle.Items.Count; i++)
                {
                    var item = cmbVehicle.Items[i];
                    if (item != null && item.ToString() == routeToEdit.VehicleTypeName)
                    {
                        cmbVehicle.SelectedIndex = i;
                        break;
                    }
                }

                // Dopasuj regułę załadunku
                cmbLoadRule.SelectedIndex = routeToEdit.LoadRule switch
                {
                    LoadThresholdRule.FullOnly => 1,
                    LoadThresholdRule.MinCargo50 => 2,
                    _ => 0
                };

                // Dopasuj parametry
                numInterval.Value = routeToEdit.IntervalHours;
                
                // Ustaw odpowiednią max pojemność zanim przypiszemy wartość
                var vConfig = VehicleRegistry.Get(routeToEdit.VehicleTypeName);
                numAmount.Maximum = vConfig.Capacity;
                numAmount.Value = Math.Min(routeToEdit.AmountPerTrip, vConfig.Capacity);

                // Dopasuj priorytet
                cmbPriority.SelectedIndex = routeToEdit.Priority switch
                {
                    RoutePriority.Low => 0,
                    RoutePriority.High => 2,
                    _ => 1
                };

                updateOpexPreview();
            }
            else
            {
                // Domyślne wartości dla kreatora nowej trasy
                if (cmbSource.Items.Count > 0) cmbSource.SelectedIndex = 0;
                if (cmbDest.Items.Count > 0) cmbDest.SelectedIndex = 0;
                updateResources();
                updateOpexPreview();
            }
        }

        // ───────────────────────────────────────────────────────────────────
        //  PANEL WOLNEGO RYNKU — ZAKUP SUROWCÓW
        // ───────────────────────────────────────────────────────────────────

        private void OpenMarketBuyer(Building? targetBuilding)
        {
            if (_gameManager == null || _company == null) return;
            _gameTimer.Stop();
            HideAllOverlays(pnlMarketBuyer);

            pnlMarketBuyer.Controls.Clear();
            BuildMarketPanel(targetBuilding);
            CenterPanel(pnlMarketBuyer);
            pnlMarketBuyer.Visible = true;
            pnlMarketBuyer.BringToFront();
        }

        private void BuildMarketPanel(Building? defaultTarget)
        {
            // Górna linia akcentu
            var topLine = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Color.FromArgb(240, 140, 20) };
            pnlMarketBuyer.Controls.Add(topLine);

            var lblTitle = new Label
            {
                Text = "📈  WOLNY RYNEK",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 180, 50),
                Location = new Point(15, 15),
                Size = new Size(400, 24)
            };
            pnlMarketBuyer.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Ceny aktualizują się każdego dnia gry. Zakup jest natychmiastowy.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(15, 40),
                Size = new Size(560, 16)
            };
            pnlMarketBuyer.Controls.Add(lblSubtitle);

            // Przycisk zamknięcia
            var btnClose = new Button
            {
                Text = "✕",
                AccessibleName = "Zamknij rynek",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(30, 28), Location = new Point(562, 10),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, BackColor = Color.Transparent, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                pnlMarketBuyer.Visible = false;
                if (_activeSpeedButton != btnSpeedPause && _currentMenuState == MenuState.Playing)
                    _gameTimer.Start();
            };
            _toolTip.SetToolTip(btnClose, "Zamknij rynek");
            pnlMarketBuyer.Controls.Add(btnClose);

            // Tabela rynku
            int rowY = 65;
            var market = _gameManager!.Market;

            // Nagłówki kolumn
            void AddHeader(string text, int x, int w)
            {
                pnlMarketBuyer.Controls.Add(new Label
                {
                    Text = text, Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.DarkGray, Location = new Point(x, rowY), Size = new Size(w, 16)
                });
            }
            AddHeader("SUROWIEC", 15, 120);
            AddHeader("CENA RYNK.", 140, 90);
            AddHeader("DOSTĘPNOŚĆ", 235, 90);
            AddHeader("CEL DOSTAWY", 330, 150);
            AddHeader("ILOŚĆ", 485, 55);
            rowY += 20;

            foreach (var kvp in market.Listings)
            {
                var listing = kvp.Value;
                bool available = listing.RemainingToday > 0;

                Color rowColor = available ? Color.FromArgb(28, 28, 28) : Color.FromArgb(22, 22, 22);
                var pnlRow = new Panel { Location = new Point(8, rowY), Size = new Size(580, 32), BackColor = rowColor };

                // Nazwa surowca
                pnlRow.Controls.Add(new Label
                {
                    Text = kvp.Key, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = available ? Color.White : Color.DimGray,
                    Location = new Point(5, 8), Size = new Size(120, 18)
                });

                // Cena
                pnlRow.Controls.Add(new Label
                {
                    Text = $"{listing.CurrentPrice:C}",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(240, 180, 50),
                    Location = new Point(125, 8), Size = new Size(100, 18)
                });

                // Dostępność
                pnlRow.Controls.Add(new Label
                {
                    Text = available ? $"{listing.RemainingToday} szt." : "Wyczerpane",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = available ? Color.FromArgb(100, 220, 100) : Color.FromArgb(100, 100, 100),
                    Location = new Point(230, 8), Size = new Size(90, 18)
                });

                if (available)
                {
                    // Dropdown budynku docelowego
                    var cmbTarget = new ComboBox
                    {
                        Location = new Point(325, 5), Size = new Size(150, 22),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8)
                    };
                    foreach (var b in _company!.Buildings) cmbTarget.Items.Add(b);
                    cmbTarget.DisplayMember = "Name";
                    if (defaultTarget != null)
                    {
                        int idx = _company.Buildings.IndexOf(defaultTarget);
                        if (idx >= 0) cmbTarget.SelectedIndex = idx;
                    }
                    else if (cmbTarget.Items.Count > 0) cmbTarget.SelectedIndex = 0;
                    pnlRow.Controls.Add(cmbTarget);

                    // Spinner ilości
                    var numQty = new NumericUpDown
                    {
                        Location = new Point(480, 5), Size = new Size(55, 22),
                        Minimum = 1, Maximum = listing.RemainingToday, Value = Math.Min(10, listing.RemainingToday),
                        BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                        Font = new Font("Segoe UI", 8)
                    };
                    pnlRow.Controls.Add(numQty);

                    // Przycisk KUP
                    var btnBuy = new Button
                    {
                        Text = "KUP", Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        Location = new Point(540, 5), Size = new Size(36, 22),
                        FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 140, 20),
                        ForeColor = Color.White, Cursor = Cursors.Hand
                    };
                    btnBuy.FlatAppearance.BorderSize = 0;
                    string resName = kvp.Key;
                    btnBuy.Click += (s, e) =>
                    {
                        if (cmbTarget.SelectedItem is Building tgt)
                        {
                            bool ok = market.BuyResource(resName, (int)numQty.Value, tgt, _company!, _gameManager!.CurrentDay, _gameManager!.CurrentHour);
                            if (ok)
                            {
                                RefreshStats();
                                OpenMarketBuyer(defaultTarget); // Odśwież panel
                            }
                            else
                            {
                                MessageBox.Show("Zakup nie powiódł się.\nSprawdź czy masz wystarczające środki i czy magazyn ma wolne miejsce.",
                                    "Błąd zakupu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    };
                    pnlRow.Controls.Add(btnBuy);
                }

                pnlMarketBuyer.Controls.Add(pnlRow);
                rowY += 36;
            }

            // Cena premium info
            var lblInfo = new Label
            {
                Text = "💡 Ceny rynkowe są o 40–120% wyższe niż koszt własnej produkcji.\n    Opłaca się zorganizować własną farmę lub kopalnię jako źródło surowców.",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(15, rowY + 10),
                Size = new Size(570, 36)
            };
            pnlMarketBuyer.Controls.Add(lblInfo);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RETAIL INSPECTOR — Specjalny widok inspektora dla sklepów
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Renderuje specjalny widok inspektora dla RetailBuilding.
        /// Zastępuje standardowy inspektor (extractor/warehouse).
        /// Wywoływany przy każdym OnTickPerformed gdy inspektor jest widoczny.
        /// </summary>
        private void UpdateRetailInspector(RetailBuilding store)
        {
            // ── Pokaż typ budynku w polu "utilization" ──
            lblContextUtilization.Text = $"Handel Detaliczny | {store.MaxSlots} sloty";
            pnlContextUtilProgressFg.Width = pnlContextUtilProgressBg.Width; // Pełny pasek dla sklepu
            pnlContextUtilProgressFg.BackColor = Color.FromArgb(80, 220, 120);

            // Ukryj standardowy pasek magazynu
            lblContextInv.Text = $"Magazyn budynku: {store.GetTotalStock()} / {store.WarehouseCapacity} szt.";

            RenderInventoryBars(store);

            // ── Główna sekcja retail ──
            var oldPanel = pnlContextInspector.Controls.Find("pnlRetailSection", false).FirstOrDefault() as Panel;
            bool needsRebuild = oldPanel == null || oldPanel.Tag != store;

            if (needsRebuild)
            {
                if (oldPanel != null)
                {
                    pnlContextInspector.Controls.Remove(oldPanel);
                    oldPanel.Dispose();
                }
                _activeRecipeComboBox = null;

                var pnlRetailSection = new Panel
                {
                    Name     = "pnlRetailSection",
                    Location = new Point(0, 160),
                    Size     = new Size(375, 380),
                    BackColor = Color.Transparent,
                    Tag = store
                };
                pnlContextInspector.Controls.Add(pnlRetailSection);

                int y = 0;

                // Nagłówek sekcji
                pnlRetailSection.Controls.Add(new Label
                {
                    Text = "PÓŁKI SPRZEDAŻOWE",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.FromArgb(80, 220, 120),
                    Location = new Point(10, y), Size = new Size(200, 16)
                });

                y += 22;

                // ── Sloty sprzedażowe ──
                foreach (var slot in store.Slots)
                {
                    BuildRetailSlotRow(pnlRetailSection, store, slot, ref y);
                    y += 4; // spacing między slotami
                }

                y += 8;

                // ── Separator ──
                pnlRetailSection.Controls.Add(new Panel
                {
                    Location = new Point(0, y), Size = new Size(370, 1),
                    BackColor = Color.FromArgb(45, 45, 45)
                });

                y += 10;

                // ── RAPORT 24h ──
                pnlRetailSection.Controls.Add(new Label
                {
                    Text = "RAPORT 24H",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.DarkGray,
                    Location = new Point(10, y), Size = new Size(200, 16)
                });

                y += 20;

                var lblTotalUnitsSold = new Label
                {
                    Name = "lblTotalUnitsSold",
                    Text = $"Łączna sprzedaż:  {store.TotalUnitsSoldLast24h} szt.",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.White,
                    Location = new Point(10, y), Size = new Size(350, 18)
                };
                pnlRetailSection.Controls.Add(lblTotalUnitsSold);

                y += 20;

                var lblTotalRevenue = new Label
                {
                    Name = "lblTotalRevenue",
                    Text = $"Przychód 24h:       {store.TotalRevenueLast24h:C}",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = store.TotalRevenueLast24h > 0 ? Color.FromArgb(80, 220, 120) : Color.Gray,
                    Location = new Point(10, y), Size = new Size(350, 18)
                };
                pnlRetailSection.Controls.Add(lblTotalRevenue);

                y += 20;

                var pnlReportDetail = new Panel
                {
                    Name = "pnlReportDetail",
                    Location = new Point(0, y),
                    Size = new Size(375, 120),
                    BackColor = Color.Transparent
                };
                pnlRetailSection.Controls.Add(pnlReportDetail);

                UpdateReportDetail(pnlReportDetail, store);
            }
            else
            {
                if (oldPanel == null) return;
                // Aktualizacja w miejscu bez usuwania panelu głównego
                for (int i = 0; i < store.Slots.Count; i++)
                {
                    UpdateRetailSlotRow(oldPanel, store, store.Slots[i], i);
                }

                var lblTotalUnitsSold = oldPanel.Controls.Find("lblTotalUnitsSold", false).FirstOrDefault() as Label;
                if (lblTotalUnitsSold != null)
                {
                    lblTotalUnitsSold.Text = $"Łączna sprzedaż:  {store.TotalUnitsSoldLast24h} szt.";
                }

                var lblTotalRevenue = oldPanel.Controls.Find("lblTotalRevenue", false).FirstOrDefault() as Label;
                if (lblTotalRevenue != null)
                {
                    lblTotalRevenue.Text = $"Przychód 24h:       {store.TotalRevenueLast24h:C}";
                    lblTotalRevenue.ForeColor = store.TotalRevenueLast24h > 0 ? Color.FromArgb(80, 220, 120) : Color.Gray;
                }

                var pnlReportDetail = oldPanel.Controls.Find("pnlReportDetail", false).FirstOrDefault() as Panel;
                if (pnlReportDetail != null)
                {
                    UpdateReportDetail(pnlReportDetail, store);
                }
            }
        }

        private void UpdateReportDetail(Panel pnlReportDetail, RetailBuilding store)
        {
            var activeSlots = store.Slots.Where(s => s.IsActive).ToList();
            
            // Usunięcie nadmiarowych etykiet, jeśli liczba slotów się zmniejszyła
            while (pnlReportDetail.Controls.Count > activeSlots.Count)
            {
                var ctrl = pnlReportDetail.Controls[pnlReportDetail.Controls.Count - 1];
                pnlReportDetail.Controls.Remove(ctrl);
                ctrl.Dispose();
            }

            int subY = 0;
            for (int i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                string newText = $"• {slot.ProductName}: {slot.UnitsSoldLast24h} szt. | {slot.RevenueLast24h:C} | Atrakcyjność: {slot.LastAttractiveness:F2}";
                Color newColor = slot.IsStockout ? Color.FromArgb(240, 80, 80) : Color.LightGray;

                if (i < pnlReportDetail.Controls.Count && pnlReportDetail.Controls[i] is Label lbl)
                {
                    if (lbl.Text != newText) lbl.Text = newText;
                    if (lbl.ForeColor != newColor) lbl.ForeColor = newColor;
                }
                else
                {
                    pnlReportDetail.Controls.Add(new Label
                    {
                        Text = newText,
                        Font = new Font("Segoe UI", 8, FontStyle.Italic),
                        ForeColor = newColor,
                        Location = new Point(14, subY), 
                        Size = new Size(355, 16),
                        AutoEllipsis = true
                    });
                }
                subY += 17;
            }
        }

        private void UpdateRetailSlotRow(Panel parent, RetailBuilding store, SalesSlot slot, int slotIdx)
        {
            Color slotColor = slot.IsStockout ? Color.FromArgb(240, 80, 80)
                            : slot.IsActive    ? Color.FromArgb(80, 220, 120)
                            : Color.DimGray;

            string slotHeader = slot.IsActive
                ? $"Slot {slot.SlotIndex + 1}: {slot.ProductName}"
                : $"Slot {slot.SlotIndex + 1}: [Pusty]";

            if (slot.IsStockout) slotHeader += " ⚠ BRAK TOWARU";

            var lblSlotHeader = parent.Controls.Find($"lblSlotHeader_{slotIdx}", true).FirstOrDefault() as Label;
            if (lblSlotHeader != null)
            {
                lblSlotHeader.Text = slotHeader;
                lblSlotHeader.ForeColor = slotColor;
            }

            var cmbProduct = parent.Controls.Find($"cmbProduct_{slotIdx}", true).FirstOrDefault() as ComboBox;
            if (cmbProduct != null && !cmbProduct.Focused)
            {
                int expectedIdx = slot.IsActive ? Math.Max(0, cmbProduct.Items.IndexOf(slot.ProductName)) : 0;
                if (cmbProduct.SelectedIndex != expectedIdx)
                {
                    cmbProduct.SelectedIndex = expectedIdx;
                }
            }

            var numMultiplier = parent.Controls.Find($"numMultiplier_{slotIdx}", true).FirstOrDefault() as NumericUpDown;
            if (numMultiplier != null && !numMultiplier.Focused)
            {
                if (numMultiplier.Value != slot.PriceMultiplier)
                {
                    numMultiplier.Value = slot.PriceMultiplier;
                }
            }

            var numDirect = parent.Controls.Find($"numDirect_{slotIdx}", true).FirstOrDefault() as NumericUpDown;
            if (numDirect != null && !numDirect.Focused)
            {
                if (numDirect.Value != slot.DirectRetailPrice)
                {
                    numDirect.Value = slot.DirectRetailPrice;
                }
            }

            var pnlStockFg = parent.Controls.Find($"pnlStockFg_{slotIdx}", true).FirstOrDefault() as Panel;
            if (pnlStockFg != null)
            {
                float fill = slot.ShelfCapacity > 0 ? (float)slot.CurrentStock / slot.ShelfCapacity : 0f;
                pnlStockFg.Width = (int)(fill * 100);
                pnlStockFg.BackColor = slot.IsStockout ? Color.FromArgb(180, 50, 50) : Color.FromArgb(50, 180, 80);
            }

            var lblStockText = parent.Controls.Find($"lblStockText_{slotIdx}", true).FirstOrDefault() as Label;
            if (lblStockText != null)
            {
                lblStockText.Text = $"{slot.CurrentStock}/{slot.ShelfCapacity}";
            }

            var lblEffective = parent.Controls.Find($"lblEffective_{slotIdx}", true).FirstOrDefault() as Label;
            if (lblEffective != null)
            {
                decimal basePrice = ResourceRegistry.GetPrice(slot.ProductName);
                decimal effectivePreview = slot.DirectRetailPrice > 0 ? slot.DirectRetailPrice : basePrice * slot.PriceMultiplier;
                lblEffective.Text = $"→ {effectivePreview:C}/szt.";
            }
        }

        private void BuildRetailSlotRow(Panel parent, RetailBuilding store, SalesSlot slot, ref int y)
        {
            // Nagłówek slotu
            Color slotColor = slot.IsStockout ? Color.FromArgb(240, 80, 80)
                            : slot.IsActive    ? Color.FromArgb(80, 220, 120)
                            : Color.DimGray;

            string slotHeader = slot.IsActive
                ? $"Slot {slot.SlotIndex + 1}: {slot.ProductName}"
                : $"Slot {slot.SlotIndex + 1}: [Pusty]";

            if (slot.IsStockout) slotHeader += " ⚠ BRAK TOWARU";

            int slotIdx = slot.SlotIndex;

            var lblHeader = new Label
            {
                Name = $"lblSlotHeader_{slotIdx}",
                Text = slotHeader,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = slotColor,
                Location = new Point(10, y), Size = new Size(355, 17)
            };
            parent.Controls.Add(lblHeader);
            y += 18;

            // Dropdown wyboru produktu
            var cmbProduct = new ComboBox
            {
                Name = $"cmbProduct_{slotIdx}",
                Location = new Point(10, y), Size = new Size(145, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8)
            };
            cmbProduct.Items.Add("— Brak —");
            foreach (var res in new[] { "Mleko", "Mięso", "Ser", "Masło" })
                cmbProduct.Items.Add(res);
            cmbProduct.SelectedIndex = slot.IsActive
                ? Math.Max(0, cmbProduct.Items.IndexOf(slot.ProductName))
                : 0;
            parent.Controls.Add(cmbProduct);

            // Pasek zapasów
            var pnlStockBg = new Panel
            {
                Name = $"pnlStockBg_{slotIdx}",
                Location = new Point(165, y + 3), Size = new Size(100, 16),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            parent.Controls.Add(pnlStockBg);

            float fill = slot.ShelfCapacity > 0 ? (float)slot.CurrentStock / slot.ShelfCapacity : 0f;
            var pnlStockFg = new Panel
            {
                Name = $"pnlStockFg_{slotIdx}",
                Location = new Point(0, 0),
                Size = new Size((int)(fill * 100), 16),
                BackColor = slot.IsStockout ? Color.FromArgb(180, 50, 50) : Color.FromArgb(50, 180, 80)
            };
            pnlStockBg.Controls.Add(pnlStockFg);

            var lblStockText = new Label
            {
                Name = $"lblStockText_{slotIdx}",
                Text = $"{slot.CurrentStock}/{slot.ShelfCapacity}",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.LightGray,
                Location = new Point(270, y + 3), Size = new Size(90, 16)
            };
            parent.Controls.Add(lblStockText);

            y += 26;

            // Cena detaliczna — mnożnik i bezpośrednia
            parent.Controls.Add(new Label
            {
                Text = "Cena (×baza):",
                Font = new Font("Segoe UI", 8), ForeColor = Color.Gray,
                Location = new Point(10, y + 3), Size = new Size(90, 16)
            });

            var numMultiplier = new NumericUpDown
            {
                Name = $"numMultiplier_{slotIdx}",
                Location = new Point(102, y), Size = new Size(60, 22),
                Minimum = 0.5m, Maximum = 5.0m, Increment = 0.1m,
                DecimalPlaces = 1,
                Value = slot.PriceMultiplier,
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                Font = new Font("Segoe UI", 8)
            };
            parent.Controls.Add(numMultiplier);

            parent.Controls.Add(new Label
            {
                Text = "lub zł/szt.:",
                Font = new Font("Segoe UI", 8), ForeColor = Color.Gray,
                Location = new Point(167, y + 3), Size = new Size(72, 16)
            });

            var numDirect = new NumericUpDown
            {
                Name = $"numDirect_{slotIdx}",
                Location = new Point(242, y), Size = new Size(70, 22),
                Minimum = 0, Maximum = 10000, DecimalPlaces = 0,
                Value = slot.DirectRetailPrice,
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                Font = new Font("Segoe UI", 8)
            };
            parent.Controls.Add(numDirect);

            // Cena efektywna — podgląd
            decimal basePrice = ResourceRegistry.GetPrice(slot.ProductName);
            decimal effectivePreview = slot.DirectRetailPrice > 0 ? slot.DirectRetailPrice : basePrice * slot.PriceMultiplier;
            var lblEffective = new Label
            {
                Name = $"lblEffective_{slotIdx}",
                Text = $"→ {effectivePreview:C}/szt.",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 180, 50),
                Location = new Point(10, y + 24), Size = new Size(200, 16)
            };
            parent.Controls.Add(lblEffective);

            // Przycisk Zatwierdź
            var btnApply = new Button
            {
                Text = "Zatwierdź",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Location = new Point(220, y + 22), Size = new Size(75, 22),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 100, 60), ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) =>
            {
                string selectedProduct = cmbProduct.SelectedIndex > 0 ? cmbProduct.SelectedItem!.ToString()! : "";

                if (string.IsNullOrEmpty(selectedProduct))
                    store.ClearSlot(slotIdx);
                else
                {
                    store.AssignProduct(slotIdx, selectedProduct, numMultiplier.Value);
                    store.Slots[slotIdx].DirectRetailPrice = numDirect.Value;
                }

                // Przelicz podgląd ceny
                decimal bp = ResourceRegistry.GetPrice(selectedProduct);
                decimal ep = numDirect.Value > 0 ? numDirect.Value : bp * numMultiplier.Value;
                lblEffective.Text = $"→ {ep:C}/szt.";
            };
            parent.Controls.Add(btnApply);

            y += 48;
        }
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (pnlStartScreen != null) pnlStartScreen.Size = this.ClientSize;
            if (pnlGameBoard != null) pnlGameBoard.Size = this.ClientSize;
            if (mapControl != null) mapControl.Size = this.ClientSize;

            if (pnlBottom != null)
            {
                pnlBottom.Location = new Point(0, this.ClientSize.Height - 30);
                pnlBottom.Width = this.ClientSize.Width;
            }
            if (pnlTopNav != null)
            {
                pnlTopNav.Width = this.ClientSize.Width;
            }
            if (pnlNewsTicker != null)
            {
                pnlNewsTicker.Width = this.ClientSize.Width;
                pnlNewsTicker.Location = new Point(0, this.ClientSize.Height - 30);
            }
            if (pnlRightShortcutBar != null)
            {
                pnlRightShortcutBar.Location = new Point(this.ClientSize.Width - 60, 60);
                pnlRightShortcutBar.Height = this.ClientSize.Height - 100;
            }

            CenterSaveGameOverlayPanel();
            CenterEscapeMenuPanel();
            CenterContextInspectorPanel();
            CenterFinanceReportPanel();
        }
    }
}
