using System;
using System.Drawing;
using System.Windows.Forms;


namespace Conglomerate.UI.Controls
{
    public class BuildingInspectorControl : UserControl
    {
        private Building _building;
        
        private Label lblTitle;
        private Label lblType;
        private Label lblStatus;
        private Label lblWorkerExp;
        private TrackBar tbTrainingBudget;
        private Label lblTrainingBudget;

        public BuildingInspectorControl(Building building)
        {
            _building = building;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            RefreshData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(300, 400); // Małe okienko boczne
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = ThemeManager.BackgroundColor;

            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 60;
            pnlHeader.BackColor = ThemeManager.HeaderBackground;
            this.Controls.Add(pnlHeader);

            lblTitle = new Label();
            lblTitle.Font = ThemeManager.HeaderFont;
            lblTitle.ForeColor = ThemeManager.TextColor;
            lblTitle.Location = new Point(10, 15);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Size = new Size(30, 30);
            btnClose.Location = new Point(this.Width - 40, 15);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = Color.Red;
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            Panel pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(10);
            this.Controls.Add(pnlContent);

            lblType = new Label { Location = new Point(20, 20), AutoSize = true };
            lblStatus = new Label { Location = new Point(20, 50), AutoSize = true };
            
            pnlContent.Controls.Add(lblType);
            pnlContent.Controls.Add(lblStatus);
            
            lblWorkerExp = new Label { Location = new Point(20, 80), AutoSize = true };
            pnlContent.Controls.Add(lblWorkerExp);

            lblTrainingBudget = new Label { Location = new Point(20, 110), AutoSize = true, Text = "Budżet Szkoleniowy (msc): $0" };
            pnlContent.Controls.Add(lblTrainingBudget);

            tbTrainingBudget = new TrackBar { Location = new Point(20, 130), Width = 240, Minimum = 0, Maximum = 1000000, TickFrequency = 100000, SmallChange = 10000, LargeChange = 100000 };
            tbTrainingBudget.Scroll += (s, e) => 
            {
                if (_building != null)
                {
                    _building.TrainingBudget = tbTrainingBudget.Value;
                    lblTrainingBudget.Text = $"Budżet Szkoleniowy (msc): {_building.TrainingBudget:C0}";
                }
            };
            pnlContent.Controls.Add(tbTrainingBudget);
            
            ThemeManager.MakeDraggable(pnlHeader, this);
            ThemeManager.MakeDraggable(lblTitle, this);
        }

        public void SetBuilding(Building building)
        {
            _building = building;
            RefreshData();
        }

        public void RefreshData()
        {
            if (_building == null) return;
            lblTitle.Text = _building.Name ?? "Nieznany Budynek";
            lblType.Text = $"Typ: {_building.GetType().Name}";
            lblStatus.Text = $"Lokacja: X:{_building.X} Y:{_building.Y}";
            lblWorkerExp.Text = $"Doświadczenie Załogi: {(_building.WorkerExperience*100f):F0}%";
            
            if (tbTrainingBudget.Value != (int)_building.TrainingBudget)
            {
                tbTrainingBudget.Value = (int)_building.TrainingBudget;
                lblTrainingBudget.Text = $"Budżet Szkoleniowy (msc): {_building.TrainingBudget:C0}";
            }
        }
    }
}
