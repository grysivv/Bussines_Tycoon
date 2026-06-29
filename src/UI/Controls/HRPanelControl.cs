using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.HR;
using Conglomerate.UI;

namespace Conglomerate.UI.Controls
{
    /// <summary>
    /// Panel zarządzania kadrami (HR) — Modern UI.
    /// Zatrudnianie/zwalnianie pracowników, regulacja płac i odświeżanie puli kandydatów.
    /// Oparty o HRManager z GameManager.
    /// </summary>
    public class HRPanelControl : UserControl
    {
        private GameManager? _gm;

        private Label _lblStats = null!;
        private Panel _pnlEmployees = null!;
        private Panel _pnlCandidates = null!;

        public HRPanelControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size           = new Size(880, 600);
            this.BackColor      = ThemeManager.BackgroundColor;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeManager.BorderColor, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Nagłówek ────────────────────────────────────────────────────────
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = ThemeManager.HeaderBackground };
            pnlHeader.Paint += (s, e) =>
            {
                var p = (Panel)s;
                using var brush = new LinearGradientBrush(p.ClientRectangle,
                    ThemeManager.HeaderBackground, Color.FromArgb(10, 22, 40), LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);
                using var goldPen = new Pen(ThemeManager.GoldColor, 2);
                e.Graphics.DrawLine(goldPen, 0, 0, p.Width, 0);
            };

            Label lblTitle = new Label
            {
                Text = "👥 Zarządzanie Kadrami (HR)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ThemeManager.TextColor,
                Location = new Point(16, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Button btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 28),
                Location = new Point(this.Width - 44, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ThemeManager.ApplySecondaryButtonTheme(btnClose);
            btnClose.ForeColor = ThemeManager.NegativeColor;
            btnClose.ToolTipText("Zamknij");
            btnClose.Click += (s, e) => this.Visible = false;
            pnlHeader.Controls.Add(btnClose);

            this.Controls.Add(pnlHeader);

            // ── Pasek statystyk ─────────────────────────────────────────────────
            _lblStats = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "Brak danych kadrowych.",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeManager.GoldColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                BackColor = ThemeManager.PanelBackground
            };
            this.Controls.Add(_lblStats);

            // ── Kontener kolumn ─────────────────────────────────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.BackgroundColor, Padding = new Padding(12) };
            this.Controls.Add(pnlBody);

            // Prawa kolumna: kandydaci
            Panel pnlRight = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = ThemeManager.BackgroundColor, Padding = new Padding(8, 0, 0, 0) };
            pnlBody.Controls.Add(pnlRight);

            Label lblCandHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "DOSTĘPNI KANDYDACI",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ThemeManager.MutedTextColor
            };
            _pnlCandidates = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(10, 18, 30), BorderStyle = BorderStyle.FixedSingle };

            Button btnRefreshPool = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Text = "🔄 Odśwież kandydatów ($500)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            ThemeManager.ApplyButtonTheme(btnRefreshPool);
            btnRefreshPool.ForeColor = ThemeManager.AccentColor;
            btnRefreshPool.Click += (s, e) => RefreshCandidatePool();

            pnlRight.Controls.Add(_pnlCandidates);
            pnlRight.Controls.Add(btnRefreshPool);
            pnlRight.Controls.Add(lblCandHeader);

            // Lewa kolumna: zatrudnieni
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = ThemeManager.BackgroundColor, Padding = new Padding(0, 0, 8, 0) };
            pnlBody.Controls.Add(pnlLeft);
            pnlLeft.BringToFront();

            Label lblEmpHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "ZATRUDNIENI PRACOWNICY",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ThemeManager.MutedTextColor
            };
            _pnlEmployees = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(10, 18, 30), BorderStyle = BorderStyle.FixedSingle };

            pnlLeft.Controls.Add(_pnlEmployees);
            pnlLeft.Controls.Add(lblEmpHeader);

            ThemeManager.MakeDraggable(pnlHeader, this);
        }

        // ─────────────────────────────────────────────────────────
        //  API publiczne
        // ─────────────────────────────────────────────────────────

        public void SetData(GameManager gm)
        {
            _gm = gm;
            Rebuild();
        }

        public void RefreshData() => Rebuild();

        // ─────────────────────────────────────────────────────────
        //  Akcje
        // ─────────────────────────────────────────────────────────

        private void RefreshCandidatePool()
        {
            if (_gm == null) return;
            var company = _gm.ActiveCompany;
            if (company.Balance >= 500m)
            {
                company.Balance -= 500m;
                company.AddTransaction(_gm.CurrentDay, _gm.CurrentHour, "Opłata za rekrutację (odświeżenie)", -500m, "Utrzymanie");
                _gm.HR.RefreshCandidatePool();
                Rebuild();
            }
            else
            {
                MessageBox.Show("Brak wystarczających środków na rekrutację (wymagane $500).",
                    "Błąd rekrutacji", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Budowa list
        // ─────────────────────────────────────────────────────────

        private void Rebuild()
        {
            if (_gm == null) return;

            var hr = _gm.HR;
            var employees = hr.Employees;

            decimal payroll = hr.CalculateTotalMonthlyPayroll();
            float avgSat = employees.Count > 0 ? employees.Average(e => e.Satisfaction) : 0f;
            float avgFat = employees.Count > 0 ? employees.Average(e => e.Fatigue) : 0f;
            float avgEff = employees.Count > 0 ? employees.Average(e => e.Efficiency) * 100f : 0f;
            _lblStats.Text = $"Pracownicy: {employees.Count}   •   Fundusz płac: ${payroll:N0}/m   •   Morale: {avgSat:F0}%   •   Zmęczenie: {avgFat:F0}%   •   Wydajność: {avgEff:F0}%";

            BuildEmployees();
            BuildCandidates();
        }

        private void BuildEmployees()
        {
            _pnlEmployees.SuspendLayout();
            _pnlEmployees.Controls.Clear();

            var employees = _gm!.HR.Employees;
            int width = _pnlEmployees.ClientSize.Width - 24;
            if (width < 200) width = 200;

            if (employees.Count == 0)
            {
                _pnlEmployees.Controls.Add(new Label
                {
                    Text = "Brak zatrudnionych pracowników.\nZrekrutuj kandydatów z panelu po prawej.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = ThemeManager.MutedTextColor,
                    Location = new Point(12, 16),
                    AutoSize = true
                });
                _pnlEmployees.ResumeLayout();
                return;
            }

            int y = 6;
            foreach (var emp in employees)
            {
                var row = new Panel
                {
                    Location = new Point(8, y),
                    Size = new Size(width, 78),
                    BackColor = Color.FromArgb(20, 34, 54)
                };
                row.Paint += (s, e) =>
                {
                    using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                    e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                };

                row.Controls.Add(new Label
                {
                    Text = $"{emp.Name}  ({emp.Role.Title})",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = ThemeManager.TextColor,
                    Location = new Point(10, 6),
                    Size = new Size(width - 80, 18),
                    AutoEllipsis = true
                });

                row.Controls.Add(new Label
                {
                    Text = $"Dział: {emp.Role.Type}   Morale: {emp.Satisfaction:F0}%   Zmęcz.: {emp.Fatigue:F0}%   Wyd.: {emp.Efficiency * 100f:F0}%",
                    Font = ThemeManager.SmallFont,
                    ForeColor = ThemeManager.MutedTextColor,
                    Location = new Point(10, 28),
                    Size = new Size(width - 20, 16),
                    AutoEllipsis = true
                });

                row.Controls.Add(new Label
                {
                    Text = $"Płaca: ${emp.MonthlySalary:N0}/m   (rynkowa: ${emp.Role.BaseMarketSalary:N0})" + (emp.IsPlanningToQuit ? "   ⚠️ planuje odejść!" : ""),
                    Font = ThemeManager.SmallFont,
                    ForeColor = emp.IsPlanningToQuit ? ThemeManager.NegativeColor
                                : emp.MonthlySalary < emp.Role.BaseMarketSalary ? ThemeManager.GoldColor
                                : ThemeManager.PositiveColor,
                    Location = new Point(10, 48),
                    Size = new Size(width - 130, 16),
                    AutoEllipsis = true
                });

                var btnMinus = new Button { Text = "-$500", Size = new Size(50, 22), Location = new Point(width - 168, 46), Font = ThemeManager.SmallFont };
                ThemeManager.ApplySecondaryButtonTheme(btnMinus);
                btnMinus.ForeColor = ThemeManager.NegativeColor;
                btnMinus.Click += (s, e) => { emp.AdjustSalary(Math.Max(0, emp.MonthlySalary - 500)); Rebuild(); };
                row.Controls.Add(btnMinus);

                var btnPlus = new Button { Text = "+$500", Size = new Size(50, 22), Location = new Point(width - 114, 46), Font = ThemeManager.SmallFont };
                ThemeManager.ApplySecondaryButtonTheme(btnPlus);
                btnPlus.ForeColor = ThemeManager.PositiveColor;
                btnPlus.Click += (s, e) => { emp.AdjustSalary(emp.MonthlySalary + 500); Rebuild(); };
                row.Controls.Add(btnPlus);

                var btnFire = new Button { Text = "Zwolnij", Size = new Size(58, 24), Location = new Point(width - 68, 8), Font = new Font("Segoe UI", 8, FontStyle.Bold) };
                ThemeManager.ApplySecondaryButtonTheme(btnFire);
                btnFire.ForeColor = ThemeManager.NegativeColor;
                btnFire.Click += (s, e) =>
                {
                    if (MessageBox.Show($"Zwolnić pracownika {emp.Name}?", "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _gm!.HR.FireEmployee(emp.Id);
                        Rebuild();
                    }
                };
                row.Controls.Add(btnFire);

                // Przyciski na wierzch, aby etykiety (dodane wcześniej) nie blokowały kliknięć.
                btnMinus.BringToFront();
                btnPlus.BringToFront();
                btnFire.BringToFront();

                _pnlEmployees.Controls.Add(row);
                y += 84;
            }

            _pnlEmployees.ResumeLayout();
        }

        private void BuildCandidates()
        {
            _pnlCandidates.SuspendLayout();
            _pnlCandidates.Controls.Clear();

            var candidates = _gm!.HR.CandidatePool;
            int width = _pnlCandidates.ClientSize.Width - 24;
            if (width < 180) width = 180;

            int y = 6;
            foreach (var cand in candidates)
            {
                var row = new Panel
                {
                    Location = new Point(8, y),
                    Size = new Size(width, 70),
                    BackColor = Color.FromArgb(20, 34, 54)
                };
                row.Paint += (s, e) =>
                {
                    using var pen = new Pen(ThemeManager.SeparatorColor, 1);
                    e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                };

                row.Controls.Add(new Label
                {
                    Text = cand.Name,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = ThemeManager.TextColor,
                    Location = new Point(10, 6),
                    Size = new Size(width - 100, 18),
                    AutoEllipsis = true
                });
                row.Controls.Add(new Label
                {
                    Text = $"{cand.Role.Title} ({cand.Role.Type})",
                    Font = ThemeManager.SmallFont,
                    ForeColor = ThemeManager.MutedTextColor,
                    Location = new Point(10, 26),
                    Size = new Size(width - 100, 16),
                    AutoEllipsis = true
                });
                row.Controls.Add(new Label
                {
                    Text = $"Żądanie: ${cand.MonthlySalary:N0}/m",
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = ThemeManager.AccentColor,
                    Location = new Point(10, 44),
                    Size = new Size(width - 100, 16)
                });

                var btnHire = new Button { Text = "Zatrudnij", Size = new Size(78, 28), Location = new Point(width - 86, 20), Font = new Font("Segoe UI", 8, FontStyle.Bold) };
                ThemeManager.ApplySecondaryButtonTheme(btnHire);
                btnHire.ForeColor = ThemeManager.PositiveColor;
                btnHire.Click += (s, e) => { _gm!.HR.HireEmployee(cand); Rebuild(); };
                row.Controls.Add(btnHire);
                btnHire.BringToFront();

                _pnlCandidates.Controls.Add(row);
                y += 76;
            }

            _pnlCandidates.ResumeLayout();
        }
    }
}
