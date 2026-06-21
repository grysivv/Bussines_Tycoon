using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Conglomerate;
using Conglomerate.Simulation;

namespace Conglomerate.UI.Controls
{
    public enum SelectedBlueprint
    {
        None,
        Farm,
        CoalMine,
        FoodWarehouse,
        MiningWarehouse,
        CheeseFactory,
        GeneralStore,
        CopperMine,
        CopperFoundry,
        RNDCenter
    }

    public class BuildPanelControl : UserControl
    {
        private GameManager _gameManager;
        private IsometricMapControl _mapControl;

        public event EventHandler<SelectedBlueprint>? OnBlueprintSelected;

        public BuildPanelControl(GameManager gameManager, IsometricMapControl mapControl)
        {
            _gameManager = gameManager;
            _mapControl  = mapControl;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size        = new Size(260, 520);
            this.BackColor   = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = ThemeManager.HeaderBackground
            };
            pnlHeader.Paint += (s, e) =>
            {
                var p = (Panel)s;
                ThemeManager.DrawWindowHeader(e.Graphics, p.ClientRectangle, "Budowa", ThemeManager.HeaderFont);
            };

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(28, 28),
                Location = new Point(this.Width - 36, 8),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right,
                AccessibleName = "Zamknij"
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.ToolTipText("Zamknij");
            btnClose.Click    += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            // ── TabControl z kategoriami ─────────────────────────────────────────
            TabControl tabs = new TabControl
            {
                Dock        = DockStyle.Fill,
                Appearance  = TabAppearance.FlatButtons,
                DrawMode    = TabDrawMode.OwnerDrawFixed,
                ItemSize    = new Size(58, 26),
                SizeMode    = TabSizeMode.Fixed,
                BackColor   = ThemeManager.PanelBackground,
                Font        = ThemeManager.SmallFont
            };
            tabs.DrawItem  += Tabs_DrawItem;
            tabs.SelectedIndexChanged += (s, e) => tabs.Invalidate();

            tabs.TabPages.Add(CreateCategory("Surowce",   BuildSurowceTab));
            tabs.TabPages.Add(CreateCategory("Produkcja", BuildProdukcjaTab));
            tabs.TabPages.Add(CreateCategory("Handel",    BuildHandelTab));

            // ── Składanie kontrolki ──────────────────────────────────────────────
            this.Controls.Add(tabs);
            this.Controls.Add(pnlHeader);

            ThemeManager.MakeDraggable(pnlHeader, this);

            // Ramka okna
            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };
        }

        private TabPage CreateCategory(string name, Action<Panel> populate)
        {
            var page = new TabPage(name)
            {
                BackColor = ThemeManager.PanelBackground,
                BorderStyle = BorderStyle.None
            };

            var scroll = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                Padding   = new Padding(8, 6, 8, 6),
                BackColor = ThemeManager.PanelBackground
            };
            populate(scroll);
            page.Controls.Add(scroll);
            return page;
        }

        private void BuildSurowceTab(Panel p)
        {
            int y = 4;
            p.Controls.Add(SectionLabel("Rolnictwo", ref y));
            p.Controls.Add(BuildEntry("Farma Krów",     "Mleko, Wołowina", "$10,000",   Color.FromArgb(60, 130, 60),   SelectedBlueprint.Farm,           ref y));

            p.Controls.Add(SectionLabel("Górnictwo", ref y));
            p.Controls.Add(BuildEntry("Kopalnia Węgla",  "Węgiel",          "$15,000",  Color.FromArgb(80, 80, 90),    SelectedBlueprint.CoalMine,       ref y));
            p.Controls.Add(BuildEntry("Kopalnia Miedzi", "Miedź",           "$20,000",  Color.FromArgb(180, 100, 50),  SelectedBlueprint.CopperMine,     ref y));

            p.Controls.Add(SectionLabel("Magazyny", ref y));
            p.Controls.Add(BuildEntry("Magazyn Żywności",    "Maks. 1000 ton", "$8,000",  Color.FromArgb(60, 100, 140),  SelectedBlueprint.FoodWarehouse,  ref y));
            p.Controls.Add(BuildEntry("Magazyn Kopalniany",  "Maks. 1000 ton", "$12,000", Color.FromArgb(60, 100, 140),  SelectedBlueprint.MiningWarehouse, ref y));
        }

        private void BuildProdukcjaTab(Panel p)
        {
            int y = 4;
            p.Controls.Add(SectionLabel("Przetwórstwo", ref y));
            p.Controls.Add(BuildEntry("Fabryka Sera",  "Mleko → Ser",         "$25,000", Color.FromArgb(200, 170, 40),  SelectedBlueprint.CheeseFactory,  ref y));
            p.Controls.Add(BuildEntry("Huta Miedzi",   "Miedź → Druty/Blachy","$30,000", Color.FromArgb(200, 100, 50),  SelectedBlueprint.CopperFoundry,  ref y));

            p.Controls.Add(SectionLabel("Badania i Rozwój", ref y));
            p.Controls.Add(BuildEntry("Centrum R&D",   "Rozwój technologii",  "$500,000", Color.FromArgb(200, 100, 250), SelectedBlueprint.RNDCenter,      ref y));
        }

        private void BuildHandelTab(Panel p)
        {
            int y = 4;
            p.Controls.Add(SectionLabel("Sklepy Detaliczne", ref y));
            p.Controls.Add(BuildEntry("Sklep Wielobranżowy", "Sprzedaż ogólna", "$50,000", Color.FromArgb(80, 130, 200), SelectedBlueprint.GeneralStore, ref y));
        }

        private Label SectionLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text      = text.ToUpper(),
                ForeColor = ThemeManager.GoldColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(220, 18),
                Location  = new Point(0, y),
                TextAlign = ContentAlignment.MiddleLeft
            };
            // Linia pod etykietą sekcji
            y += 20;
            return lbl;
        }

        private Panel BuildEntry(string name, string desc, string cost, Color accentColor, SelectedBlueprint blueprint, ref int y)
        {
            var entry = new Panel
            {
                Size      = new Size(220, 54),
                Location  = new Point(0, y),
                BackColor = Color.FromArgb(16, 30, 50),
                Cursor    = Cursors.Hand
            };
            entry.Paint += (s, e) =>
            {
                var p = (Panel)s;
                // Lewy pasek koloru kategorii
                using var brush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(brush, 0, 0, 4, p.Height);
                // Ramka
                using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };

            Label lblName = new Label
            {
                Text      = name,
                ForeColor = ThemeManager.TextColor,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(180, 18),
                Location  = new Point(10, 4),
                BackColor = Color.Transparent
            };
            Label lblDesc = new Label
            {
                Text      = desc,
                ForeColor = ThemeManager.MutedTextColor,
                Font      = ThemeManager.SmallFont,
                AutoSize  = false,
                Size      = new Size(180, 14),
                Location  = new Point(10, 20),
                BackColor = Color.Transparent
            };
            Label lblCost = new Label
            {
                Text      = cost,
                ForeColor = ThemeManager.GoldColor,
                Font      = new Font("Consolas", 9, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(100, 16),
                Location  = new Point(10, 34),
                BackColor = Color.Transparent
            };
            Button btnBuild = new Button
            {
                Text      = "Buduj",
                Size      = new Size(52, 22),
                Location  = new Point(164, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeManager.HeaderBackground,
                ForeColor = ThemeManager.TextColor,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnBuild.FlatAppearance.BorderColor = accentColor;
            btnBuild.FlatAppearance.BorderSize  = 1;
            btnBuild.Click += (s, e) =>
            {
                OnBlueprintSelected?.Invoke(this, blueprint);
                _mapControl.SetBuildMode(true);
            };

            // Kliknięcie całego panelu też buduje
            entry.Click += (s, e) =>
            {
                OnBlueprintSelected?.Invoke(this, blueprint);
                _mapControl.SetBuildMode(true);
            };

            // Hover
            entry.MouseEnter += (s, e) => { entry.BackColor = Color.FromArgb(22, 42, 68); entry.Invalidate(); };
            entry.MouseLeave += (s, e) => { entry.BackColor = Color.FromArgb(16, 30, 50); entry.Invalidate(); };

            entry.Controls.Add(lblName);
            entry.Controls.Add(lblDesc);
            entry.Controls.Add(lblCost);
            entry.Controls.Add(btnBuild);

            // Kontrolki wewnętrzne nie łapią kliku panelu - propagacja
            foreach (Control c in entry.Controls)
                c.Click += (s, e) => { if (!(s is Button)) { OnBlueprintSelected?.Invoke(this, blueprint); _mapControl.SetBuildMode(true); } };

            // Przycisk „Buduj" na wierzch, by całość była klikalna mimo nachodzących etykiet.
            btnBuild.BringToFront();

            y += 60;
            return entry;
        }

        private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tc) return;
            bool selected = (e.Index == tc.SelectedIndex);

            var g = e.Graphics;
            var tabRect = tc.GetTabRect(e.Index);

            Color bg = selected ? ThemeManager.HeaderBackground : ThemeManager.ToolbarBackground;
            using var bgBrush = new SolidBrush(bg);
            g.FillRectangle(bgBrush, tabRect);

            if (selected)
            {
                using var accentPen = new Pen(ThemeManager.GoldColor, 2);
                g.DrawLine(accentPen, tabRect.Left, tabRect.Top, tabRect.Right, tabRect.Top);
            }

            var textColor = selected ? ThemeManager.GoldColor : ThemeManager.MutedTextColor;
            using var textBrush = new SolidBrush(textColor);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(tc.TabPages[e.Index].Text, ThemeManager.SmallFont, textBrush, tabRect, sf);

            using var borderPen = new Pen(ThemeManager.SeparatorColor, 1);
            g.DrawRectangle(borderPen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
        }
    }
}
