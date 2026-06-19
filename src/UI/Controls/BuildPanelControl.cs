using System;
using System.Drawing;
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
        CopperFoundry 
    }

    public class BuildPanelControl : UserControl
    {
        private GameManager _gameManager;
        private IsometricMapControl _mapControl;
        
        public event EventHandler<SelectedBlueprint>? OnBlueprintSelected;
        
        public BuildPanelControl(GameManager gameManager, IsometricMapControl mapControl)
        {
            _gameManager = gameManager;
            _mapControl = mapControl;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(200, 500); // Wąski pasek z boku lub pływające okno
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = ThemeManager.BackgroundColor;

            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 40;
            pnlHeader.BackColor = ThemeManager.HeaderBackground;
            
            Label lblTitle = new Label();
            lblTitle.Text = "Budynki";
            lblTitle.Font = ThemeManager.HeaderFont;
            lblTitle.ForeColor = ThemeManager.TextColor;
            lblTitle.Location = new Point(10, 10);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Size = new Size(30, 30);
            btnClose.Location = new Point(this.Width - 40, 5);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = Color.Red;
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            FlowLayoutPanel flowList = new FlowLayoutPanel();
            flowList.Dock = DockStyle.Fill;
            flowList.FlowDirection = FlowDirection.TopDown;
            flowList.WrapContents = false;
            flowList.Padding = new Padding(10);
            flowList.AutoScroll = true;
            this.Controls.Add(flowList);

            flowList.Controls.Add(CreateBuildButton("Farma Krów ($10k)", SelectedBlueprint.Farm));
            flowList.Controls.Add(CreateBuildButton("Kopalnia Węgla ($15k)", SelectedBlueprint.CoalMine));
            flowList.Controls.Add(CreateBuildButton("Kopalnia Miedzi ($20k)", SelectedBlueprint.CopperMine));
            flowList.Controls.Add(CreateBuildButton("Magazyn Żywności ($8k)", SelectedBlueprint.FoodWarehouse));
            flowList.Controls.Add(CreateBuildButton("Magazyn Kopalniany ($12k)", SelectedBlueprint.MiningWarehouse));
            flowList.Controls.Add(CreateBuildButton("Fabryka Sera ($25k)", SelectedBlueprint.CheeseFactory));
            flowList.Controls.Add(CreateBuildButton("Huta Miedzi ($30k)", SelectedBlueprint.CopperFoundry));
            flowList.Controls.Add(CreateBuildButton("Sklep Wielobranżowy ($50k)", SelectedBlueprint.GeneralStore));

            ThemeManager.MakeDraggable(pnlHeader, this);
            ThemeManager.MakeDraggable(lblTitle, this);
        }

        private Button CreateBuildButton(string text, SelectedBlueprint blueprint)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(160, 40);
            btn.Margin = new Padding(0, 0, 0, 5);
            ThemeManager.ApplySecondaryButtonTheme(btn);
            btn.Click += (s, e) => 
            {
                OnBlueprintSelected?.Invoke(this, blueprint);
                _mapControl.SetBuildMode(true);
            };
            return btn;
        }
    }
}
