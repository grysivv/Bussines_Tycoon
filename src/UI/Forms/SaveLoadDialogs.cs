using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Conglomerate.UI;

namespace Conglomerate.UI.Forms
{
    /// <summary>Proste modalne okno do podania nazwy zapisu gry (Modern UI).</summary>
    public class SaveNameDialog : Form
    {
        private readonly TextBox _txtName;

        public string SaveName => _txtName.Text.Trim();

        public SaveNameDialog(string defaultName)
        {
            Text = "Zapisz grę";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(380, 140);
            BackColor = ThemeManager.PanelBackground;
            ForeColor = ThemeManager.TextColor;

            var lbl = new Label
            {
                Text = "Nazwa zapisu:",
                ForeColor = ThemeManager.MutedTextColor,
                Font = ThemeManager.DefaultFont,
                Location = new Point(16, 16),
                AutoSize = true
            };
            Controls.Add(lbl);

            _txtName = new TextBox
            {
                Text = defaultName,
                Location = new Point(16, 40),
                Width = 348,
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeManager.DefaultFont
            };
            Controls.Add(_txtName);

            var btnOk = new Button
            {
                Text = "Zapisz",
                DialogResult = DialogResult.OK,
                Location = new Point(184, 90),
                Size = new Size(86, 32)
            };
            ThemeManager.ApplyButtonTheme(btnOk);
            btnOk.ForeColor = ThemeManager.PositiveColor;

            var btnCancel = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new Point(278, 90),
                Size = new Size(86, 32)
            };
            ThemeManager.ApplySecondaryButtonTheme(btnCancel);

            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // Walidacja pustej nazwy
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                {
                    MessageBox.Show("Nazwa zapisu nie może być pusta!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            _txtName.SelectAll();
        }
    }

    /// <summary>Modalne okno wyboru pliku zapisu do wczytania (Modern UI).</summary>
    public class LoadGameDialog : Form
    {
        private readonly ListBox _list;
        private readonly List<FileInfo> _files;

        public string? SelectedPath { get; private set; }

        public LoadGameDialog()
        {
            Text = "Wczytaj grę";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 420);
            BackColor = ThemeManager.PanelBackground;
            ForeColor = ThemeManager.TextColor;

            var lbl = new Label
            {
                Text = "Dostępne zapisy:",
                ForeColor = ThemeManager.GoldColor,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            Controls.Add(lbl);

            _list = new ListBox
            {
                Location = new Point(16, 40),
                Size = new Size(528, 312),
                BackColor = ThemeManager.BackgroundColor,
                ForeColor = ThemeManager.TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeManager.DataFont,
                IntegralHeight = false
            };
            _list.DoubleClick += (s, e) => Confirm();
            Controls.Add(_list);

            _files = SaveGameManager.GetSaveFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            foreach (var f in _files)
            {
                string label;
                try
                {
                    var meta = SaveGameManager.GetSaveMetadata(f.FullName);
                    label = $"{Path.GetFileNameWithoutExtension(f.Name)}  —  {meta.CorporationName}  •  Dzień {meta.CurrentDay}  •  ${meta.NetWorth:N0}  •  {meta.RealWorldSaveTime:yyyy-MM-dd HH:mm}";
                }
                catch
                {
                    label = $"{Path.GetFileNameWithoutExtension(f.Name)}  (uszkodzony plik)";
                }
                _list.Items.Add(label);
            }

            if (_list.Items.Count > 0) _list.SelectedIndex = 0;

            var btnLoad = new Button
            {
                Text = "Wczytaj",
                Location = new Point(360, 372),
                Size = new Size(86, 32)
            };
            ThemeManager.ApplyButtonTheme(btnLoad);
            btnLoad.ForeColor = ThemeManager.PositiveColor;
            btnLoad.Click += (s, e) => Confirm();

            var btnCancel = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new Point(458, 372),
                Size = new Size(86, 32)
            };
            ThemeManager.ApplySecondaryButtonTheme(btnCancel);

            Controls.Add(btnLoad);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;

            if (_list.Items.Count == 0)
            {
                btnLoad.Enabled = false;
                _list.Items.Add("Brak zapisanych gier.");
                _list.Enabled = false;
            }
        }

        private void Confirm()
        {
            if (!_list.Enabled || _list.SelectedIndex < 0 || _list.SelectedIndex >= _files.Count) return;
            SelectedPath = _files[_list.SelectedIndex].FullName;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
