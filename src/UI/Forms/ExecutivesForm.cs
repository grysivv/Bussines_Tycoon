using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.HR;
using Conglomerate.UI;

namespace Conglomerate
{
    /// <summary>
    /// Panel Zarządzania Dyrektorami (C-Suite Executives).
    /// Wzorowany na Capitalism Lab: CEO/COO/CMO/CTO/CFO/CSO z bonusami.
    /// </summary>
    public class ExecutivesForm : Panel
    {
        private GameManager? _gm;
        private Company?     _player;
        private Random _rng = new Random();

        private DataGridView dgvHired      = null!;
        private DataGridView dgvCandidates = null!;
        private Label lblPayroll          = null!;
        private Button btnHire             = null!;
        private Button btnFire             = null!;
        private Button btnRefresh          = null!;

        private System.Collections.Generic.List<Executive> _candidatePool = new();

        public ExecutivesForm()
        {
            InitControls();
            GenerateCandidates();
            ThemeManager.ApplyTheme(this);
        }

        private void InitControls()
        {
            lblPayroll = new Label
            {
                Text = "Łączna miesięczna pensja dyrektorów: $0",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(230, 100, 80),
                Location = new Point(10, 10), AutoSize = true
            };
            this.Controls.Add(lblPayroll);

            // Zatrudnieni dyrektorzy
            var lblHired = new Label { Text = "Zatrudnieni dyrektorzy:", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(200, 150, 255), Location = new Point(10, 35), AutoSize = true };
            this.Controls.Add(lblHired);

            dgvHired = MakeGrid(10, 56, 640, 150);
            dgvHired.Columns.Add("Imię", "Imię i nazwisko");
            dgvHired.Columns.Add("Stanowisko", "Stanowisko");
            dgvHired.Columns.Add("Pensja", "Pensja/mies.");
            dgvHired.Columns.Add("Skill", "Skill (1-10)");
            dgvHired.Columns.Add("Bonus", "Bonus");
            dgvHired.Columns["Imię"].Width = 140;
            dgvHired.Columns["Stanowisko"].Width = 60;
            dgvHired.Columns["Pensja"].Width = 100;
            dgvHired.Columns["Skill"].Width = 70;
            dgvHired.Columns["Bonus"].Width = 250;
            this.Controls.Add(dgvHired);

            btnFire = MakeButton("ZWOLNIJ DYREKTORA", Color.FromArgb(150, 40, 30), 10, 212);
            btnFire.Click += OnFire;
            this.Controls.Add(btnFire);

            // Kandydaci
            var lblCand = new Label { Text = "Dostępni kandydaci:", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(200, 150, 255), Location = new Point(10, 255), AutoSize = true };
            this.Controls.Add(lblCand);

            dgvCandidates = MakeGrid(10, 275, 640, 200);
            dgvCandidates.Columns.Add("Imię", "Imię i nazwisko");
            dgvCandidates.Columns.Add("Stanowisko", "Pozycja");
            dgvCandidates.Columns.Add("Pensja", "Żądana pensja/mies.");
            dgvCandidates.Columns.Add("Skill", "Skill (1-10)");
            dgvCandidates.Columns.Add("Bonus", "Oferowany bonus");
            dgvCandidates.Columns["Imię"].Width = 140;
            dgvCandidates.Columns["Stanowisko"].Width = 60;
            dgvCandidates.Columns["Pensja"].Width = 120;
            dgvCandidates.Columns["Skill"].Width = 70;
            dgvCandidates.Columns["Bonus"].Width = 230;
            this.Controls.Add(dgvCandidates);

            btnHire = MakeButton("ZATRUDNIJ KANDYDATA", Color.FromArgb(30, 110, 60), 10, 481);
            btnHire.Click += OnHire;
            this.Controls.Add(btnHire);

            btnRefresh = MakeButton("ODŚWIEŻ KANDYDATÓW", Color.FromArgb(30, 60, 110), 220, 481);
            btnRefresh.Click += (s, e) => { GenerateCandidates(); RefreshData(); };
            this.Controls.Add(btnRefresh);

            // Legenda bonusów
            var pnlLegend = new Panel { Location = new Point(10, 525), Size = new Size(640, 80), BackColor = Color.FromArgb(25, 25, 40) };
            var lblLegend = new Label
            {
                Text = "💡 Legenda: CEO = +Net Income | COO = +Wydajność fabryk | CMO = +Brand Awareness & Popyt\n" +
                       "           CTO = +R&D & Jakość | CFO = -Podatek CIT | CSO = +Przychody sklepy",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(8, 8), Size = new Size(620, 60)
            };
            pnlLegend.Controls.Add(lblLegend);
            this.Controls.Add(pnlLegend);
        }

        private DataGridView MakeGrid(int x, int y, int w, int h)
        {
            var dgv = new DataGridView
            {
                Location = new Point(x, y), Size = new Size(w, h),
                BackgroundColor = Color.FromArgb(22, 22, 32), ForeColor = Color.White,
                GridColor = Color.FromArgb(40, 40, 55), BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(200, 150, 255);
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 40, 100);
            return dgv;
        }

        private Button MakeButton(string text, Color bg, int x, int y)
        {
            var btn = new Button
            {
                Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(x, y), Size = new Size(200, 32),
                BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void GenerateCandidates()
        {
            _candidatePool.Clear();
            var types = Enum.GetValues<ExecutiveType>();
            foreach (var type in types)
            {
                _candidatePool.Add(Executive.GenerateCandidate(type, _rng));
            }
            // Dodaj 2 losowych
            _candidatePool.Add(Executive.GenerateCandidate((ExecutiveType)_rng.Next(types.Length), _rng));
            _candidatePool.Add(Executive.GenerateCandidate((ExecutiveType)_rng.Next(types.Length), _rng));
        }

        public void SetGameManager(GameManager gm, Company player)
        {
            _gm    = gm;
            _player = player;
            RefreshData();
        }

        public void RefreshData()
        {
            if (_player == null) return;

            // Zatrudnieni
            dgvHired.Rows.Clear();
            foreach (var exec in _player.HiredExecutives)
            {
                dgvHired.Rows.Add(exec.Name, exec.Type.ToString(), exec.MonthlySalary.ToString("C0"), $"{exec.SkillLevel:F1}", exec.BonusDescription);
            }

            // Payroll
            decimal payroll = _player.HiredExecutives.Sum(e => e.MonthlySalary);
            lblPayroll.Text = $"Łączna miesięczna pensja dyrektorów: {payroll:C0}";

            // Kandydaci
            dgvCandidates.Rows.Clear();
            foreach (var cand in _candidatePool)
            {
                dgvCandidates.Rows.Add(cand.Name, cand.Type.ToString(), cand.MonthlySalary.ToString("C0"), $"{cand.SkillLevel:F1}", cand.BonusDescription);
            }
        }

        private void OnHire(object? sender, EventArgs e)
        {
            if (_player == null || dgvCandidates.SelectedRows.Count == 0) return;
            int idx = dgvCandidates.SelectedRows[0].Index;
            if (idx < 0 || idx >= _candidatePool.Count) return;

            var candidate = _candidatePool[idx];

            // Sprawdź czy już mamy kogoś na tym stanowisku
            bool alreadyHired = _player.HiredExecutives.Any(e => e.Type == candidate.Type);
            if (alreadyHired)
            {
                MessageBox.Show($"Już masz dyrektora na pozycji {candidate.Type}!\nZwolnij go najpierw.", "HR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _player.HiredExecutives.Add(candidate);
            _candidatePool.Remove(candidate);

            MessageBox.Show($"Zatrudniono {candidate.Name} jako {candidate.Type}!\nPensja: {candidate.MonthlySalary:C0}/miesiąc\nBonus: {candidate.BonusDescription}",
                "HR - Gratulacje", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshData();
        }

        private void OnFire(object? sender, EventArgs e)
        {
            if (_player == null || dgvHired.SelectedRows.Count == 0) return;
            int idx = dgvHired.SelectedRows[0].Index;
            if (idx < 0 || idx >= _player.HiredExecutives.Count) return;

            var exec = _player.HiredExecutives[idx];
            var result = MessageBox.Show($"Zwolnić {exec.Name} ({exec.Type})?\nOdprawa: {exec.MonthlySalary * 3:C0}",
                "HR", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Odprawa = 3 miesiące pensji
                decimal severance = exec.MonthlySalary * 3;
                if (_player.Balance >= severance) _player.Balance -= severance;

                _player.HiredExecutives.Remove(exec);
                _candidatePool.Add(exec); // Wraca na rynek pracy
                RefreshData();
            }
        }
    }
}
