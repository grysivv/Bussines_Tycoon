using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private Label _lblNetWorth;

        public CorporatePanelControl(Company company)
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
                Text      = "Przegląd Firmy",
                Font      = ThemeManager.TitleFont,
                ForeColor = ThemeManager.TextColor,
                Location  = new Point(16, 10),
                AutoSize  = true
            };
            pnlHeader.Controls.Add(lblTitle);

            _lblTotalBuildings = new Label
            {
                Font      = ThemeManager.HeaderFont,
                ForeColor = ThemeManager.AccentColor,
                Location  = new Point(16, 44),
                AutoSize  = true,
                Text      = "Zakłady: 0"
            };
            pnlHeader.Controls.Add(_lblTotalBuildings);

            _lblNetWorth = new Label
            {
                Font      = ThemeManager.HeaderFont,
                ForeColor = ThemeManager.GoldColor,
                Location  = new Point(160, 44),
                AutoSize  = true,
                Text      = "Wartość netto: $0"
            };
            pnlHeader.Controls.Add(_lblNetWorth);

            Button btnClose = new Button
            {
                Text     = "✕",
                Size     = new Size(32, 32),
                Location = new Point(this.Width - 44, 12),
                Anchor   = AnchorStyles.Top | AnchorStyles.Right,
                AccessibleName = "Zamknij"
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.ToolTipText("Zamknij");
            btnClose.Click    += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // ── Tabela budynków ──────────────────────────────────────────────────
            _dgvBuildings = new DataGridView { Dock = DockStyle.Fill };
            ThemeManager.ApplyDataGridViewTheme(_dgvBuildings);

            _dgvBuildings.AutoGenerateColumns = false;
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",     HeaderText = "Nazwa Zakładu",  DataPropertyName = "Name",     Width = 210 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",     HeaderText = "Typ",            DataPropertyName = "Type",     Width = 160 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Location", HeaderText = "Lokalizacja",    DataPropertyName = "Location", Width = 90  });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Workers",  HeaderText = "Doświadcz.",     DataPropertyName = "Workers",  Width = 100 });
            _dgvBuildings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "Status",         DataPropertyName = "Status",   Width = 100 });

            _dgvBuildings.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 4 && e.Value is string status)
                    e.CellStyle.ForeColor = status == "Operational" ? ThemeManager.PositiveColor : ThemeManager.NegativeColor;
            };

            Panel pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 12) };
            pnlGrid.Controls.Add(_dgvBuildings);
            this.Controls.Add(pnlGrid);

            ThemeManager.MakeDraggable(pnlHeader, this);
        }

        public void RefreshData()
        {
            if (_company == null) return;

            _lblTotalBuildings.Text = $"Zakłady: {_company.Buildings.Count}";
            _lblNetWorth.Text       = $"Wartość netto: ${_company.Balance:N0}";

            var dataSource = _company.Buildings.Select(b => new
            {
                Name     = b.Name,
                Type     = b.GetType().Name,
                Location = $"({b.X}, {b.Y})",
                Workers  = $"{(b.WorkerExperience * 100f):F0}%",
                Status   = "Operational"
            }).ToList();

            _dgvBuildings.DataSource = dataSource;
        }
    }
}
