using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Timer2.App.UI;

namespace Timer2.App.Forms
{
    public class InAppLogViewerForm : Form
    {
        private readonly string _filePath;
        private readonly string _themeName;
        private TextBox txtSearch;
        private RichTextBox rtbLog;
        private Label lblStatus;
        private string _fullContent = string.Empty;

        public InAppLogViewerForm(string filePath, string title, string themeName)
        {
            _filePath = filePath;
            _themeName = themeName;

            this.Text = $"Log Viewer — {title}";
            this.Size = new Size(820, 580);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeComponent();
            ThemeManager.ApplyTheme(this, _themeName);
            LoadFileContent();
        }

        private void InitializeComponent()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(12, 10, 12, 10) };

            var lblSearch = new Label { Text = "Search Filter:", Location = new Point(12, 16), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            txtSearch = new TextBox { Location = new Point(115, 14), Width = 340, Font = new Font("Segoe UI", 9.5f) };
            txtSearch.TextChanged += (s, e) => FilterLog();

            var btnClear = new Button { Text = "Clear", Location = new Point(465, 12), Width = 70, Height = 28 };
            btnClear.Click += (s, e) => { txtSearch.Text = ""; FilterLog(); };

            var btnCopy = new Button { Text = "Copy All", Location = new Point(545, 12), Width = 80, Height = 28 };
            btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(rtbLog.Text))
                {
                    Clipboard.SetText(rtbLog.Text);
                    MessageBox.Show("Log contents copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnReload = new Button { Text = "Reload", Location = new Point(635, 12), Width = 80, Height = 28 };
            btnReload.Click += (s, e) => LoadFileContent();

            topPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnClear, btnCopy, btnReload });
            this.Controls.Add(topPanel);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(10, 5, 10, 5) };
            lblStatus = new Label { Dock = DockStyle.Fill, Text = "Ready", Font = new Font("Segoe UI", 8.5f) };
            bottomPanel.Controls.Add(lblStatus);
            this.Controls.Add(bottomPanel);

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10f),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Tag = "LogBox"
            };
            this.Controls.Add(rtbLog);
        }

        private void LoadFileContent()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    _fullContent = File.ReadAllText(_filePath);
                    FilterLog();
                }
                catch (Exception ex)
                {
                    rtbLog.Text = $"Error reading log file: {ex.Message}";
                }
            }
            else
            {
                rtbLog.Text = $"Log file not found at: {_filePath}";
            }
        }

        private void FilterLog()
        {
            string query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                rtbLog.Text = _fullContent;
                int lines = _fullContent.Split('\n').Length;
                lblStatus.Text = $"File: {_filePath} | Total Lines: {lines}";
            }
            else
            {
                var matchingLines = _fullContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                    .Where(l => l.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                rtbLog.Text = string.Join(Environment.NewLine, matchingLines);
                lblStatus.Text = $"File: {_filePath} | Showing {matchingLines.Count} line(s) matching '{query}'";
            }

            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }
    }
}
