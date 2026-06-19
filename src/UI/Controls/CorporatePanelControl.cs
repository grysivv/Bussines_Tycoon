using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using System.Collections.Generic;
using Conglomerate.Financials;

namespace Conglomerate.UI.Controls
{
    public class CorporatePanelControl : UserControl
    {
        private Company _company;
        private DataGridView _dgvBuildings;
        private Label _lblTotalBuildings;

        public CorporatePanelControl(Company company)
        {
            _company = company;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            RefreshData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600); // Okno pływające
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = ThemeManager.BackgroundColor;

            // Header
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 80;
            pnlHeader.BackColor = ThemeManager.HeaderBackground;
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label();
            lblTitle.Text = "Corporate Overview";
            lblTitle.Font = ThemeManager.TitleFont;
            lblTitle.ForeColor = ThemeManager.TextColor;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            _lblTotalBuildings = new Label();
            _lblTotalBuildings.Font = ThemeManager.HeaderFont;
            _lblTotalBuildings.ForeColor = ThemeManager.AccentColor;
            _lblTotalBuildings.Location = new Point(20, 50);
            _lblTotalBuildings.AutoSize = true;
            pnlHeader.Controls.Add(_lblTotalBuildings);

            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Size = new Size(40, 40);
            btnClose.Location = new Point(this.Width - 60, 20);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = Color.Red;
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            // Data Grid View
            _dgvBuildings = new DataGridView();
            _dgvBuildings.Dock = DockStyle.Fill;
            ThemeManager.ApplyDataGridViewTheme(_dgvBuildings);
            
            // Kolumny
            _dgvBuildings.AutoGenerateColumns = false;
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Facility Name", DataPropertyName = "Name", Width = 200 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", DataPropertyName = "Type", Width = 150 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Location", DataPropertyName = "Location", Width = 100 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 100 });

            Panel pnlGridContainer = new Panel();
            pnlGridContainer.Dock = DockStyle.Fill;
            pnlGridContainer.Padding = new Padding(20);
            pnlGridContainer.Controls.Add(_dgvBuildings);
            this.Controls.Add(pnlGridContainer);
            
            ThemeManager.MakeDraggable(pnlHeader, this);
        }

        public void RefreshData()
        {
            if (_company == null) return;
            
            _lblTotalBuildings.Text = $"Total Facilities: {_company.Buildings.Count}";
            
            var dataSource = _company.Buildings.Select(b => new 
            {
                Name = b.Name,
                Type = b.GetType().Name,
                Location = $"({b.X}, {b.Y})",
                Status = "Operational" // Zastępcze, można podpiąć pod logikę wyłączania budynków
            }).ToList();

            _dgvBuildings.DataSource = dataSource;
        }
    }
}
