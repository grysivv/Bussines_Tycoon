using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Finance;
using Conglomerate.UI;

namespace Conglomerate
{
    /// <summary>
    /// Panel Bankowy — zaciąganie i spłata kredytów. Capitalism Lab style.
    /// Wyświetla aktywne kredyty, zdolność kredytową i umożliwia nowe pożyczki.
    /// </summary>
    public class BankingForm : Panel
    {
        private GameManager? _gm;
        private Company?     _player;

        private Label lblCreditLimit = null!;
        private Label lblTotalDebt   = null!;
        private Label lblMonthlyLoad = null!;
        private DataGridView dgvLoans = null!;
        private ComboBox cmbLoanType = null!;
        private NumericUpDown numLoanAmount = null!;
        private Button btnTakeLoan = null!;
        private Button btnRepay = null!;

        public BankingForm()
        {
            InitControls();
            ThemeManager.ApplyTheme(this);
        }

        private void InitControls()
        {
            int y = 10;

            // Panel stanu kredytowego
            var pnlStatus = new Panel
            {
                Location = new Point(10, y),
                Size     = new Size(500, 90),
                BackColor = Color.FromArgb(25, 30, 45),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlStatus);

            lblCreditLimit = MakeLabel(pnlStatus, "Zdolność kredytowa: obliczam...", 12, 8, FontStyle.Bold, Color.FromArgb(100, 230, 100));
            lblTotalDebt   = MakeLabel(pnlStatus, "Łączne zadłużenie: $0",          38, 8, FontStyle.Regular, Color.FromArgb(230, 100, 80));
            lblMonthlyLoad = MakeLabel(pnlStatus, "Miesięczne raty: $0",            62, 8, FontStyle.Regular, Color.FromArgb(240, 180, 50));
            y += 100;

            // Tabela aktywnych kredytów
            var lblLoansTitle = new Label { Text = "Aktywne kredyty:", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(150, 180, 255), Location = new Point(10, y), AutoSize = true };
            this.Controls.Add(lblLoansTitle);
            y += 25;

            dgvLoans = new DataGridView
            {
                Location = new Point(10, y),
                Size     = new Size(580, 160),
                BackgroundColor = Color.FromArgb(22, 22, 32),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(45, 45, 60),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvLoans.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 50);
            dgvLoans.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(150, 180, 255);
            dgvLoans.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            dgvLoans.DefaultCellStyle.ForeColor = Color.White;
            dgvLoans.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 70, 120);
            dgvLoans.Columns.Add("Typ",  "Typ kredytu");
            dgvLoans.Columns.Add("Kwota", "Pozostało");
            dgvLoans.Columns.Add("Rata",  "Miesięczna rata");
            dgvLoans.Columns.Add("Oprocentowanie", "Oprocentowanie");
            dgvLoans.Columns.Add("Miesięce", "Mies. pozostało");
            this.Controls.Add(dgvLoans);
            y += 175;

            // Formularz nowego kredytu
            var lblNewLoan = new Label { Text = "Nowy kredyt:", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(150, 180, 255), Location = new Point(10, y), AutoSize = true };
            this.Controls.Add(lblNewLoan);
            y += 28;

            cmbLoanType = new ComboBox
            {
                Location = new Point(10, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            cmbLoanType.Items.Add("Krótkoterminowy (10%, 12 mies.)");
            cmbLoanType.Items.Add("Średnioterminowy (8%, 36 mies.)");
            cmbLoanType.Items.Add("Długoterminowy (6%, 60 mies.)");
            cmbLoanType.SelectedIndex = 0;
            this.Controls.Add(cmbLoanType);

            numLoanAmount = new NumericUpDown
            {
                Location = new Point(200, y), Width = 160, Minimum = 10000, Maximum = 5000000,
                Value = 100000, Increment = 10000, DecimalPlaces = 0,
                BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            this.Controls.Add(numLoanAmount);
            y += 35;

            btnTakeLoan = new Button
            {
                Text = "ZACIĄGNIJ KREDYT", Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(10, y), Size = new Size(170, 34),
                BackColor = Color.FromArgb(30, 110, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnTakeLoan.FlatAppearance.BorderSize = 0;
            btnTakeLoan.Click += OnTakeLoan;
            this.Controls.Add(btnTakeLoan);

            btnRepay = new Button
            {
                Text = "SPŁAĆ WYBRANY", Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(190, y), Size = new Size(150, 34),
                BackColor = Color.FromArgb(120, 60, 20), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnRepay.FlatAppearance.BorderSize = 0;
            btnRepay.Click += OnRepayLoan;
            this.Controls.Add(btnRepay);
        }

        private static Label MakeLabel(Panel parent, string text, int y, int x, FontStyle style, Color color)
        {
            var lbl = new Label { Text = text, Font = new Font("Segoe UI", 9, style), ForeColor = color, Location = new Point(x, y), AutoSize = true };
            parent.Controls.Add(lbl);
            return lbl;
        }

        public void SetGameManager(GameManager gm, Company player)
        {
            _gm = gm;
            _player = player;
            RefreshData();
        }

        public void RefreshData()
        {
            if (_gm == null || _player == null) return;

            decimal netWorth    = _player.GetNetWorth();
            decimal creditLimit = _gm.Banking.CalculateCreditLimit(netWorth, _gm.Banking.TotalDebt);
            decimal totalDebt   = _gm.Banking.TotalDebt;
            decimal monthlyLoad = _gm.Banking.TotalMonthlyPayments;

            lblCreditLimit.Text = $"Zdolność kredytowa: {creditLimit:C0}";
            lblTotalDebt.Text   = $"Łączne zadłużenie: {totalDebt:C0}";
            lblMonthlyLoad.Text = $"Miesięczne raty: {monthlyLoad:C0}";

            dgvLoans.Rows.Clear();
            foreach (var loan in _gm.Banking.Loans)
            {
                dgvLoans.Rows.Add(
                    loan.Type.ToString(),
                    loan.OutstandingBalance.ToString("C0"),
                    loan.MonthlyPayment.ToString("C0"),
                    $"{loan.AnnualInterestRate * 100:F1}%",
                    loan.MonthsRemaining
                );
            }
        }

        private void OnTakeLoan(object? sender, EventArgs e)
        {
            if (_gm == null || _player == null) return;
            var loanType = (LoanType)cmbLoanType.SelectedIndex;
            decimal amount = numLoanAmount.Value;

            decimal netWorth = _player.GetNetWorth();
            decimal creditAvail = _gm.Banking.CalculateCreditLimit(netWorth, _gm.Banking.TotalDebt);

            if (amount > creditAvail)
            {
                MessageBox.Show($"Przekraczasz zdolność kredytową!\nDostępne: {creditAvail:C0}", "Bank", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = _gm.Banking.TakeLoan(loanType, amount, _player, _gm.CurrentDay, _gm.CurrentHour);
            MessageBox.Show(ok ? $"Kredyt {amount:C0} przyznany!" : "Błąd kredytu.", "Bank", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshData();
        }

        private void OnRepayLoan(object? sender, EventArgs e)
        {
            if (_gm == null || _player == null || dgvLoans.SelectedRows.Count == 0) return;
            int idx = dgvLoans.SelectedRows[0].Index;
            var loan = _gm.Banking.Loans.ElementAtOrDefault(idx);
            if (loan == null) return;

            decimal repayAmount = Math.Min(loan.OutstandingBalance, _player.Balance);
            if (repayAmount <= 0) { MessageBox.Show("Brak środków do spłaty.", "Bank", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            bool ok = _gm.Banking.RepayLoan(loan.LoanId, repayAmount, _player, _gm.CurrentDay, _gm.CurrentHour);
            MessageBox.Show(ok ? $"Spłacono {repayAmount:C0}!" : "Błąd spłaty.", "Bank", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            RefreshData();
        }
    }
}
