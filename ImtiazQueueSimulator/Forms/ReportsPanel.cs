using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Reports panel showing saved simulation summaries with View and Save TXT buttons.
    /// Cards are responsive full-width with right-aligned action buttons and empty state.
    /// </summary>
    public class ReportsPanel : UserControl
    {
        public event Action<SimulationResult>? OnSaveReport;
        private FlowLayoutPanel _reportsFlow = null!;
        private Label _emptyLabel = null!;
        private List<SimulationResult> _reports = new();

        public ReportsPanel()
        {
            BackColor = Color.FromArgb(244, 246, 250);
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            int y = 12;

            var title = new Label
            {
                Text      = "📄 REPORTS",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(15, y)
            };
            Controls.Add(title);
            y += 32;

            _emptyLabel = new Label
            {
                Text      = "No reports yet. Run a simulation to generate reports.",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(15, y + 4)
            };
            Controls.Add(_emptyLabel);

            _reportsFlow = new FlowLayoutPanel
            {
                Location      = new Point(15, y + 32),
                Width         = Math.Max(Width - 30, 600),
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0)
            };
            Resize += (s, e) =>
            {
                _reportsFlow.Width = Math.Max(Width - 30, 600);
                foreach (Control card in _reportsFlow.Controls)
                    card.Width = _reportsFlow.Width - 10;
            };
            Controls.Add(_reportsFlow);
        }

        public void LoadReports(List<SimulationResult> reports)
        {
            _reports = reports;
            _reportsFlow.Controls.Clear();
            _emptyLabel.Visible = reports.Count == 0;

            foreach (var r in reports)
            {
                var card = CreateReportCard(r);
                _reportsFlow.Controls.Add(card);
            }
        }

        private Panel CreateReportCard(SimulationResult r)
        {
            int cardW = Math.Max(_reportsFlow.Width - 10, 600);
            var card = new Panel
            {
                Size      = new Size(cardW, 115),
                BackColor = Color.White,
                Margin    = new Padding(0, 0, 0, 12),
                Cursor    = Cursors.Default
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                var path = new GraphicsPath();
                int d = 16;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            };

            var modelLabel = new Label
            {
                Text      = $"{r.ModelName} Simulation Report",
                Font      = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(16, 14)
            };
            card.Controls.Add(modelLabel);

            var dateLabel = new Label
            {
                Text         = $"{r.CreatedAt:dd MMM yyyy HH:mm} • {r.TotalCustomers} Customers • {r.SimulationTime:F1} hrs sim time",
                Font         = new Font("Segoe UI", 8.5f),
                ForeColor    = Color.FromArgb(71, 85, 105),
                AutoSize     = false,
                Location     = new Point(16, 38),
                Size         = new Size(Math.Max(100, card.Width - 230), 20),
                AutoEllipsis = true,
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(dateLabel);

            var metricsLabel = new Label
            {
                Text         = $"Lq: {r.SimLq:F2}   L: {r.SimL:F2}   Wq: {r.SimWq * 60:F1} min   W: {r.SimW * 60:F1} min   ρ: {r.SimRho * 100:F1}%",
                Font         = new Font("Segoe UI Semibold", 9.5f),
                ForeColor    = Color.FromArgb(30, 41, 59),
                AutoSize     = false,
                Location     = new Point(16, 64),
                Size         = new Size(Math.Max(100, card.Width - 230), 22),
                AutoEllipsis = true,
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(metricsLabel);

            // Right-aligned action buttons
            var btnSave = new Button
            {
                Text      = "SAVE TXT",
                Size      = new Size(95, 34),
                Location  = new Point(card.Width - 115, 62),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                Cursor    = Cursors.Hand,
                Tag       = r
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => OnSaveReport?.Invoke(r);
            card.Controls.Add(btnSave);

            var btnView = new Button
            {
                Text      = "VIEW",
                Size      = new Size(85, 34),
                Location  = new Point(card.Width - 215, 62),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                Cursor    = Cursors.Hand,
                Tag       = r
            };
            btnView.FlatAppearance.BorderSize = 0;
            btnView.Click += (s, e) =>
            {
                string report = Reports.ReportGenerator.GenerateReport(r);
                var viewForm = new Form
                {
                    Text          = $"Report Summary — {r.ModelName}",
                    Size          = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };
                var txt = new TextBox
                {
                    Multiline  = true,
                    ReadOnly   = true,
                    ScrollBars = ScrollBars.Both,
                    Dock       = DockStyle.Fill,
                    Font       = new Font("Consolas", 9.5f),
                    Text       = report,
                    BackColor  = Color.White,
                    WordWrap   = false
                };
                viewForm.Controls.Add(txt);
                viewForm.ShowDialog();
            };
            card.Resize += (s, e) =>
            {
                int w = card.Width;
                int btnZoneW = 220;
                int textW = Math.Max(150, w - btnZoneW - 24);
                modelLabel.MaximumSize = new Size(textW, 0);
                dateLabel.Size = new Size(textW, 20);
                metricsLabel.Size = new Size(textW, 22);
                btnSave.Location = new Point(w - 110, 40);
                btnView.Location = new Point(w - 205, 40);
            };

            // Trigger initial resize
            card.Size = new Size(cardW, 115);

            return card;
        }
    }
}
