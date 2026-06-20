using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Conglomerate.Financials;
using System.Linq;

namespace Conglomerate.UI.Controls
{
    public class FinancePanelControl : UserControl
    {
        private Company _company;
        private Label _lblBalance;
        private Label _lblBalanceCaption;
        private DataGridView _dgvTransactions;

        public FinancePanelControl(Company company)
        {
            _company = company;
            InitializeComponent();
            RefreshData();
        }

        private void InitializeComponent()
        {
            this.Size           = new Size(820, 600);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = ThemeManager.HeaderBackground
            };
            pnlHeader.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var brush = new LinearGradientBrush(
                    p.ClientRectangle,
                    ThemeManager.HeaderBackground,
                    Color.FromArgb(10, 22, 40),
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);

                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, p.Width, 0);

                using var sepPen = new Pen(ThemeManager.SeparatorColor, 1);
                e.Graphics.DrawLine(sepPen, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text      = "Raport Finansowy",
                Font      = ThemeManager.TitleFont,
                ForeColor = ThemeManager.TextColor,
                Location  = new Point(16, 10),
                AutoSize  = true
            };
            pnlHeader.Controls.Add(lblTitle);

            _lblBalanceCaption = new Label
            {
                Text      = "Stan konta:",
                Font      = ThemeManager.SmallFont,
                ForeColor = ThemeManager.MutedTextColor,
                Location  = new Point(16, 42),
                AutoSize  = true
            };
            pnlHeader.Controls.Add(_lblBalanceCaption);

            _lblBalance = new Label
            {
                Font      = new Font("Consolas", 18, FontStyle.Bold),
                ForeColor = ThemeManager.PositiveColor,
                Location  = new Point(16, 55),
                AutoSize  = true,
                Text      = "$0"
            };
            pnlHeader.Controls.Add(_lblBalance);

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(32, 32),
                Location = new Point(this.Width - 44, 12),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.Click    += (s, e) => this.Visible = false;
            ThemeManager.SetToolTip(btnClose, "Zamknij");
            btnClose.AccessibleName = "Zamknij";
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // ── Tabela transakcji ────────────────────────────────────────────────
            _dgvTransactions = new DataGridView { Dock = DockStyle.Fill };
            ThemeManager.ApplyDataGridViewTheme(_dgvTransactions);

            _dgvTransactions.AutoGenerateColumns = false;
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dzień",      DataPropertyName = "Day",         Width = 58  });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Godz.",      DataPropertyName = "Hour",        Width = 48  });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Opis",       DataPropertyName = "Description", Width = 310 });
            _dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kategoria",  DataPropertyName = "Category",    Width = 140 });

            var amountCol = new DataGridViewTextBoxColumn
            {
                HeaderText       = "Kwota",
                DataPropertyName = "Amount",
                Width            = 120
            };
            amountCol.DefaultCellStyle.Format    = "C2";
            amountCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvTransactions.Columns.Add(amountCol);

            _dgvTransactions.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 4 && e.Value is decimal val)
                    e.CellStyle.ForeColor = val >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;
            };

            Panel pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 12) };
            pnlGrid.Controls.Add(_dgvTransactions);
            this.Controls.Add(pnlGrid);

            ThemeManager.MakeDraggable(pnlHeader, this);
        }

        public void RefreshData()
        {
            if (_company == null) return;

            _lblBalance.Text      = $"${_company.Balance:N2}";
            _lblBalance.ForeColor = _company.Balance >= 0 ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;

            var transactions = _company.Ledger.GetAllTransactions()
                                       .OrderByDescending(t => t.Day)
                                       .ThenByDescending(t => t.Hour)
                                       .ToList();
            _dgvTransactions.DataSource = transactions;
        }
    }
}
