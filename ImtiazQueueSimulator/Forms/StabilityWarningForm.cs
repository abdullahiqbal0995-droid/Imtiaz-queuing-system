using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Custom modern warning modal displayed when simulating an unstable queueing system.
    /// Fully styled according to the Imtiaz Queue Analyzer branding guidelines.
    /// </summary>
    public class StabilityWarningForm : Form
    {
        public StabilityWarningForm(double lambda, double mu, int n, double rho)
        {
            Text = "Stability Warning";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(480, 420);
            BackColor = Color.FromArgb(244, 246, 250);

            // Header Panel
            var headerPanel = new Panel
            {
                BackColor = Color.FromArgb(254, 242, 242), // Red Tint
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(16, 0, 16, 0)
            };
            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(254, 202, 202), 1f);
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            var warningTitle = new Label
            {
                Text = "⚠ SYSTEM INSTABILITY WARNING",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38), // Crimson Warning
                AutoSize = true,
                Location = new Point(16, 18),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(warningTitle);
            Controls.Add(headerPanel);

            // Content Panel (Flow Layout for spacing auto-wrap support)
            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Location = new Point(24, 78),
                Width = 420,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            Controls.Add(contentFlow);

            contentFlow.Controls.Add(CreateTextLabel("The configured queuing parameters result in a theoretically unstable system (offered load ρ ≥ 100%):"));

            // Card Panel for Metrics
            var paramTable = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true,
                Width = 412,
                Margin = new Padding(0, 12, 0, 12),
                BackColor = Color.White,
                Padding = new Padding(12),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            paramTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            paramTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));

            paramTable.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, paramTable.Width - 1, paramTable.Height - 1);
                using var path = RoundPath(r, 8);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            AddRow(paramTable, "Arrival Rate (λ):", $"{lambda:F2} cust/hr", 0);
            AddRow(paramTable, "Service Rate per Server (μ):", $"{mu:F2} cust/hr", 1);
            AddRow(paramTable, "Active Servers (N):", $"{n}", 2);
            AddRow(paramTable, "Total Service Capacity (N × μ):", $"{n * mu:F2} cust/hr", 3);
            AddRow(paramTable, "System Utilization (ρ):", $"{rho * 100:F2}%", 4, true);

            contentFlow.Controls.Add(paramTable);

            var explanationLbl = new Label
            {
                Text = "Under steady-state conditions, the queue will grow without bound. Do you wish to continue and run the simulation anyway? (Useful for demonstrating transient queue growth)",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                MaximumSize = new Size(412, 0),
                Margin = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };
            contentFlow.Controls.Add(explanationLbl);

            // Action Buttons Panel
            var buttonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.Transparent
            };
            Controls.Add(buttonsPanel);

            var btnCancel = new Button
            {
                Text = "NO / Cancel",
                DialogResult = DialogResult.No,
                Size = new Size(130, 36),
                Location = new Point(164, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 9.5f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            buttonsPanel.Controls.Add(btnCancel);

            var btnContinue = new Button
            {
                Text = "YES / Continue",
                DialogResult = DialogResult.Yes,
                Size = new Size(130, 36),
                Location = new Point(306, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38), // Warning Red Background
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnContinue.FlatAppearance.BorderSize = 0;
            buttonsPanel.Controls.Add(btnContinue);

            // Adjust height dynamically based on layout contents
            contentFlow.PerformLayout();
            Height = headerPanel.Height + contentFlow.Height + buttonsPanel.Height + 52;
        }

        private Label CreateTextLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize = true,
                MaximumSize = new Size(412, 0),
                Margin = new Padding(0, 0, 0, 6),
                BackColor = Color.Transparent
            };
        }

        private void AddRow(TableLayoutPanel tbl, string label, string val, int row, bool highlight = false)
        {
            var lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9.5f, highlight ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = highlight ? Color.FromArgb(220, 38, 38) : Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent
            };
            var lblVal = new Label
            {
                Text = val,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = highlight ? Color.FromArgb(220, 38, 38) : Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent
            };
            tbl.Controls.Add(lblLabel, 0, row);
            tbl.Controls.Add(lblVal, 1, row);
        }

        private static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
