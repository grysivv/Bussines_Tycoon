using System;
using System.Drawing;
using System.Windows.Forms;
using Conglomerate.Financials;
using System.Linq;

namespace Conglomerate.UI.Controls
{
    public class FinancePanelControl : UserControl
    {
        private Company _company;
        private Label _lblBalance;
        private DataGridView _dgvTransactions;

        public FinancePanelControl(Company company)
        {
            _company = company;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            RefreshData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600); // Stały rozmiar jako okno nakładki
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = ThemeManager.BackgroundColor;

            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 100;
            pnlHeader.BackColor = ThemeManager.HeaderBackground;
            this.Controls.Add(pnlHeader);

            Label lblTitle = new Label();
            lblTitle.Text = "Corporate Finance Report";
            lblTitle.Font = ThemeManager.TitleFont;
            lblTitle.ForeColor = ThemeManager.TextColor;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            _lblBalance = new Label();
            _lblBalance.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            _lblBalance.ForeColor = ThemeManager.PositiveColor;
            _lblBalance.Location = new Point(20, 55);
            _lblBalance.AutoSize = true;
            pnlHeader.Controls.Add(_lblBalance);

            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Size = new Size(40, 40);
            btnClose.Location = new Point(this.Width - 60, 20);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = Color.Red;
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            // Transakcje
            _dgvTransactions = new DataGridView();
            _dgvTransactions.Dock = DockStyle.Fill;
            ThemeManager.ApplyDataGridViewTheme(_dgvTransactions);
            
            _dgvTransactions.AutoGenerateColumns = false;
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dzień", DataPropertyName = "Day", Width = 60 });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Godzina", DataPropertyName = "Hour", Width = 60 });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Opis", DataPropertyName = "Description", Width = 300 });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kategoria", DataPropertyName = "Category", Width = 150 });
            
            DataGridViewTextBoxColumn amountCol = new DataGridViewTextBoxColumn { HeaderText = "Kwota", DataPropertyName = "Amount", Width = 120 };
            amountCol.DefaultCellStyle.Format = "C2";
            _dgvTransactions.Columns.Add(amountCol);

            Panel pnlGridContainer = new Panel();
            pnlGridContainer.Dock = DockStyle.Fill;
            pnlGridContainer.Padding = new Padding(20);
            pnlGridContainer.Controls.Add(_dgvTransactions);
            this.Controls.Add(pnlGridContainer);
            
            ThemeManager.MakeDraggable(pnlHeader, this);
        }

        public void RefreshData()
        {
            if (_company == null) return;
            
            _lblBalance.Text = $"Stan konta: {_company.Balance:C2}";
            _lblBalance.ForeColor = _company.Balance >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;

            var transactions = _company.Ledger.GetAllTransactions().OrderByDescending(t => t.Day).ThenByDescending(t => t.Hour).ToList();
            _dgvTransactions.DataSource = transactions;
        }
    }
}
