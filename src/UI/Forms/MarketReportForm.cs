using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.Marketing;
using Conglomerate.UI;

namespace Conglomerate
{
    /// <summary>
    /// Panel Raportu Rynkowego — ranking firm, trendy cen, analiza rynku.
    /// Wzorowany na Market Report z Capitalism Lab.
    /// </summary>
    public class MarketReportForm : Panel
    {
        private GameManager? _gm;
        private Company?     _player;

        private DataGridView dgvRanking = null!;
        private Panel pnlMarketTrends  = null!;
        private Label lblPlayerRank    = null!;
        private Panel pnlAdCampaign    = null!;

        // Kampania reklamowa
        private ComboBox cmbProduct    = null!;
        private ComboBox cmbCampaignType = null!;
        private NumericUpDown numBudget  = null!;
        private NumericUpDown numDuration = null!;
        private Button btnLaunchCampaign = null!;
        private DataGridView dgvCampaigns = null!;

        public MarketReportForm()
        {
            InitControls();
            ThemeManager.ApplyTheme(this);
        }

        private void InitControls()
        {
            lblPlayerRank = new Label
            {
                Text = "Twoja pozycja: obliczam...",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 200, 255),
                Location = new Point(10, 10), AutoSize = true
            };
            this.Controls.Add(lblPlayerRank);

            // Ranking firm
            var lblRankTitle = new Label { Text = "Ranking Firm (Market Cap):", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(150, 180, 255), Location = new Point(10, 35), AutoSize = true };
            this.Controls.Add(lblRankTitle);

            dgvRanking = new DataGridView
            {
                Location = new Point(10, 56),
                Size     = new Size(560, 180),
                BackgroundColor = Color.FromArgb(22, 22, 32),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(40, 40, 55),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvRanking.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 50);
            dgvRanking.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(150, 180, 255);
            dgvRanking.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            dgvRanking.DefaultCellStyle.ForeColor = Color.White;
            dgvRanking.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 70, 120);
            dgvRanking.Columns.Add("Rank", "#");
            dgvRanking.Columns.Add("Firma", "Firma");
            dgvRanking.Columns.Add("MarketCap", "Market Cap");
            dgvRanking.Columns.Add("Cena", "Cena akcji");
            dgvRanking.Columns.Add("UdzialGracza", "Twój udział");
            dgvRanking.Columns["Rank"].Width = 35;
            dgvRanking.Columns["Firma"].Width = 160;
            dgvRanking.Columns["MarketCap"].Width = 120;
            dgvRanking.Columns["Cena"].Width = 100;
            dgvRanking.Columns["UdzialGracza"].Width = 100;
            this.Controls.Add(dgvRanking);

            // ── Sekcja Marketingu ─────────────────────────────────
            var lblAdTitle = new Label { Text = "📢 Kampanie Reklamowe (Brand Awareness):", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(240, 180, 50), Location = new Point(10, 245), AutoSize = true };
            this.Controls.Add(lblAdTitle);

            pnlAdCampaign = new Panel
            {
                Location = new Point(10, 270),
                Size     = new Size(580, 215),
                BackColor = Color.FromArgb(25, 25, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlAdCampaign);

            // Formularz kampanii
            int fx = 8, fy = 8;
            Label MakeLabel(string t) => new Label { Text = t, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, Location = new Point(fx, fy), AutoSize = true };

            pnlAdCampaign.Controls.Add(MakeLabel("Produkt:"));
            fy += 16;
            cmbProduct = new ComboBox { Location = new Point(fx, fy), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            foreach (var r in new[] { "Mleko", "Ser", "Masło", "Odzież", "Odzież Premium", "Smartfon", "Laptop", "Meble", "Meble Luksusowe", "Żywność Pakowana", "Chleb" })
                cmbProduct.Items.Add(r);
            cmbProduct.SelectedIndex = 0;
            pnlAdCampaign.Controls.Add(cmbProduct);
            fy += 30;

            pnlAdCampaign.Controls.Add(new Label { Text = "Typ kampanii:", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, Location = new Point(fx, fy), AutoSize = true });
            fy += 16;
            cmbCampaignType = new ComboBox { Location = new Point(fx, fy), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            cmbCampaignType.Items.Add("📺 Telewizja (szeroki zasięg)");
            cmbCampaignType.Items.Add("📱 Social Media (szybki wzrost)");
            cmbCampaignType.Items.Add("🏙️ Outdoor / Bilboardy");
            cmbCampaignType.Items.Add("⭐ Influencerzy (silny spike)");
            cmbCampaignType.SelectedIndex = 1;
            pnlAdCampaign.Controls.Add(cmbCampaignType);
            fy += 30;

            // Budżet i czas
            pnlAdCampaign.Controls.Add(new Label { Text = "Budżet dzienny (zł):", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, Location = new Point(fx, fy), AutoSize = true });
            numBudget = new NumericUpDown { Location = new Point(fx + 130, fy - 3), Width = 100, Minimum = 1000, Maximum = 100000, Value = 5000, Increment = 1000, BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            pnlAdCampaign.Controls.Add(numBudget);
            fy += 30;

            pnlAdCampaign.Controls.Add(new Label { Text = "Czas trwania (dni):", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, Location = new Point(fx, fy), AutoSize = true });
            numDuration = new NumericUpDown { Location = new Point(fx + 130, fy - 3), Width = 80, Minimum = 7, Maximum = 90, Value = 14, BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 9) };
            pnlAdCampaign.Controls.Add(numDuration);
            fy += 30;

            btnLaunchCampaign = new Button
            {
                Text = "🚀 URUCHOM KAMPANIĘ",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(fx, fy), Size = new Size(190, 32),
                BackColor = Color.FromArgb(130, 80, 20), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnLaunchCampaign.FlatAppearance.BorderSize = 0;
            btnLaunchCampaign.Click += OnLaunchCampaign;
            pnlAdCampaign.Controls.Add(btnLaunchCampaign);

            // Aktywne kampanie
            var lblActiveCamp = new Label { Text = "Aktywne kampanie:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(150, 180, 255), Location = new Point(10, 495), AutoSize = true };
            this.Controls.Add(lblActiveCamp);

            dgvCampaigns = new DataGridView
            {
                Location = new Point(10, 517),
                Size     = new Size(560, 110),
                BackgroundColor = Color.FromArgb(22, 22, 32),
                ForeColor = Color.White, GridColor = Color.FromArgb(40, 40, 55),
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                ReadOnly = true, AllowUserToAddRows = false
            };
            dgvCampaigns.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 50);
            dgvCampaigns.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(150, 180, 255);
            dgvCampaigns.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);
            dgvCampaigns.DefaultCellStyle.ForeColor = Color.White;
            dgvCampaigns.Columns.Add("Produkt", "Produkt");
            dgvCampaigns.Columns.Add("Typ",  "Typ");
            dgvCampaigns.Columns.Add("Budzet", "Budżet/dzień");
            dgvCampaigns.Columns.Add("Pozostalo", "Dni pozostało");
            dgvCampaigns.Columns.Add("BrandAwareness", "Brand Awareness");
            this.Controls.Add(dgvCampaigns);
        }

        public void SetGameManager(GameManager gm, Company player)
        {
            _gm    = gm;
            _player = player;
            RefreshData();
        }

        public void RefreshData()
        {
            if (_gm == null || _player == null) return;

            // Ranking firm
            dgvRanking.Rows.Clear();
            var rankings = _gm.StockMarket.GetRanking();
            int rank = 1;
            int playerRank = -1;
            foreach (var listing in rankings)
            {
                decimal ownPct = listing.PlayerOwnershipPercent;
                bool isPlayer = listing.CompanyName == _player.Name;
                if (isPlayer) playerRank = rank;

                int rowIdx = dgvRanking.Rows.Add(
                    rank,
                    (isPlayer ? "★ " : "") + listing.CompanyName,
                    FormatMoney(listing.MarketCap),
                    listing.SharePrice.ToString("C0"),
                    ownPct > 0 ? $"{ownPct:F1}%" : "-"
                );

                if (isPlayer)
                    dgvRanking.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(100, 230, 100);

                rank++;
            }

            lblPlayerRank.Text = playerRank > 0
                ? $"Twoja pozycja: #{playerRank} z {rankings.Count} firm"
                : "Twoja firma nie jest notowana na giełdzie";

            // Kampanie reklamowe
            dgvCampaigns.Rows.Clear();
            foreach (var campaign in _player.ActiveCampaigns)
            {
                float ba = _player.GetBrandAwareness(campaign.ProductName);
                dgvCampaigns.Rows.Add(
                    campaign.ProductName,
                    campaign.Type.ToString(),
                    campaign.DailyBudget.ToString("C0"),
                    campaign.DaysRemaining,
                    $"{ba:F1}/100"
                );
            }

            // Pokaż Brand Awareness dla produktów bez kampanii
            if (_player.ActiveCampaigns.Count == 0)
            {
                dgvCampaigns.Rows.Add("(brak aktywnych kampanii)", "", "", "", "");
            }
        }

        private void OnLaunchCampaign(object? sender, EventArgs e)
        {
            if (_gm == null || _player == null) return;
            if (cmbProduct.SelectedItem == null) return;

            string product = cmbProduct.SelectedItem.ToString() ?? "";
            var campaignType = (CampaignType)cmbCampaignType.SelectedIndex;
            decimal budget = numBudget.Value;
            int days = (int)numDuration.Value;

            decimal totalCost = budget * days;
            if (_player.Balance < budget)
            {
                MessageBox.Show($"Potrzebujesz co najmniej {budget:C0} na dzień kampanii!\nSaldo: {_player.Balance:C0}", "Marketing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var campaign = new AdvertisingCampaign(product, campaignType, budget, days, _gm.CurrentDay);
            _player.ActiveCampaigns.Add(campaign);

            MessageBox.Show($"Kampania dla {product} uruchomiona!\nCałkowity koszt: {totalCost:C0}\nPrzewidywany wzrost Brand Awareness: +{campaign.DailyAwarenessGain * days:F1} pkt",
                "Marketing", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshData();
        }

        private static string FormatMoney(decimal value)
        {
            if (value >= 1_000_000m) return $"${value / 1_000_000m:F1}M";
            if (value >= 1_000m) return $"${value / 1_000m:F0}K";
            return $"{value:C0}";
        }
    }
}
