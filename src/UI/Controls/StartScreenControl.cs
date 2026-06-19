using System;
using System.Drawing;
using System.Windows.Forms;

namespace Conglomerate.UI.Controls
{
    public class StartScreenControl : UserControl
    {
        public event EventHandler? StartNewGameClicked;
        public event EventHandler? LoadGameClicked;
        public event EventHandler? ExitClicked;

        private Panel pnlCenter;

        public StartScreenControl()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            this.BackColor = Color.FromArgb(20, 20, 20); // Głębszy kolor dla ekranu startowego
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;

            pnlCenter = new Panel();
            pnlCenter.Size = new Size(400, 300);
            pnlCenter.BackColor = ThemeManager.PanelBackground;
            this.Controls.Add(pnlCenter);

            this.SizeChanged += (s, e) =>
            {
                pnlCenter.Location = new Point(
                    (this.Width - pnlCenter.Width) / 2,
                    (this.Height - pnlCenter.Height) / 2
                );
            };

            Panel pnlTopBorder = new Panel();
            pnlTopBorder.Dock = DockStyle.Top;
            pnlTopBorder.Height = 5;
            pnlTopBorder.BackColor = ThemeManager.AccentColor;
            pnlCenter.Controls.Add(pnlTopBorder);

            Label lblTitle = new Label();
            lblTitle.Text = "CONGLOMERATE";
            lblTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.AccentColor;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 60;
            lblTitle.Padding = new Padding(0, 10, 0, 0);
            pnlCenter.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Capitalism Simulator";
            lblSubtitle.Font = ThemeManager.HeaderFont;
            lblSubtitle.ForeColor = ThemeManager.MutedTextColor;
            lblSubtitle.TextAlign = ContentAlignment.TopCenter;
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.Height = 40;
            pnlCenter.Controls.Add(lblSubtitle);

            Button btnNewGame = new Button();
            btnNewGame.Text = "Nowa Gra";
            btnNewGame.Size = new Size(200, 40);
            btnNewGame.Location = new Point(100, 120);
            ThemeManager.ApplyButtonTheme(btnNewGame);
            btnNewGame.Click += (s, e) => StartNewGameClicked?.Invoke(this, EventArgs.Empty);
            pnlCenter.Controls.Add(btnNewGame);

            Button btnLoadGame = new Button();
            btnLoadGame.Text = "Wczytaj Grę";
            btnLoadGame.Size = new Size(200, 40);
            btnLoadGame.Location = new Point(100, 170);
            ThemeManager.ApplySecondaryButtonTheme(btnLoadGame);
            btnLoadGame.Click += (s, e) => LoadGameClicked?.Invoke(this, EventArgs.Empty);
            pnlCenter.Controls.Add(btnLoadGame);

            Button btnExit = new Button();
            btnExit.Text = "Wyjście";
            btnExit.Size = new Size(200, 40);
            btnExit.Location = new Point(100, 220);
            ThemeManager.ApplySecondaryButtonTheme(btnExit);
            btnExit.Click += (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty);
            pnlCenter.Controls.Add(btnExit);
        }
    }
}
