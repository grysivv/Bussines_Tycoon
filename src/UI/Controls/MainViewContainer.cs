using System;
using System.Drawing;
using System.Windows.Forms;

namespace Conglomerate.UI.Controls
{
    public class MainViewContainer : UserControl
    {
        public Panel TopToolbar { get; private set; }
        public Panel LeftSidebar { get; private set; }
        public Panel MainWorkspace { get; private set; }
        public Panel BottomStatusBar { get; private set; }

        public MainViewContainer()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeManager.BackgroundColor;

            TopToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = ThemeManager.HeaderBackground
            };
            
            // Bottom line for toolbar
            Panel toolbarBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ThemeManager.BorderColor };
            TopToolbar.Controls.Add(toolbarBorder);

            BottomStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = ThemeManager.HeaderBackground
            };

            LeftSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = ThemeManager.PanelBackground
            };
            // Right line for sidebar
            Panel sidebarBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = ThemeManager.BorderColor };
            LeftSidebar.Controls.Add(sidebarBorder);

            MainWorkspace = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.BackgroundColor,
                Padding = new Padding(10)
            };

            // Kolejność dodawania ma znaczenie w WinForms dla Dockingu
            this.Controls.Add(MainWorkspace);
            this.Controls.Add(LeftSidebar);
            this.Controls.Add(TopToolbar);
            this.Controls.Add(BottomStatusBar);
        }

        public void SetMainContent(Control control)
        {
            if (!MainWorkspace.Controls.Contains(control))
            {
                control.Dock = DockStyle.Fill;
                MainWorkspace.Controls.Add(control);
            }
            
            // Ukrywamy wszystkie z wyjątkiem nakładek
            foreach (Control c in MainWorkspace.Controls)
            {
                if (c.Dock == DockStyle.Fill)
                {
                    c.Visible = (c == control);
                }
            }
        }

        public void ShowOverlay(Control overlay)
        {
            if (!MainWorkspace.Controls.Contains(overlay))
            {
                overlay.Dock = DockStyle.None; // Nakładki mają konkretny rozmiar
                MainWorkspace.Controls.Add(overlay);
                overlay.BringToFront();
            }
            
            overlay.Visible = true;
            overlay.BringToFront();
            CenterOverlay(overlay);
        }

        public void HideOverlay(Control overlay)
        {
            if (MainWorkspace.Controls.Contains(overlay))
            {
                overlay.Visible = false;
            }
        }

        private void CenterOverlay(Control overlay)
        {
            if (overlay == null || !overlay.Visible) return;
            overlay.Location = new Point(
                (MainWorkspace.Width - overlay.Width) / 2,
                (MainWorkspace.Height - overlay.Height) / 2
            );
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (MainWorkspace != null)
            {
                foreach (Control c in MainWorkspace.Controls)
                {
                    if (c.Dock == DockStyle.None && c.Visible)
                    {
                        CenterOverlay(c);
                    }
                }
            }
        }
    }
}
