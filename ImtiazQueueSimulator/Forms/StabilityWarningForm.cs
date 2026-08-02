using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Custom enterprise-grade warning modal displayed when simulating an unstable queueing system.
    /// Engineered with 100% dynamic sizing and zero-clipping labels to handle High DPI scaling seamlessly.
    /// </summary>
    public class StabilityWarningForm : Form
    {
        public StabilityWarningForm(double lambda, double mu, int n, double rho)
        {
            // Form setup
            Text = "Stability Warning";
            FormBorderStyle = FormBorderStyle.None; // borderless for 14px rounded corners
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            // ── 1. Header (64px height) ──
            var headerPanel = new Panel
            {
                BackColor = Color.White,
                Size = new Size(640, 64),
                Location = new Point(0, 0)
            };
            
            var warningIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(249, 115, 22), // Orange Warning
                Location = new Point(24, 14),
                Size = new Size(30, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(warningIcon);

            var titleLbl = new Label
            {
                Text = "System Instability Warning",
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59), // Dark Gray slate-800
                Location = new Point(56, 16),
                Size = new Size(350, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(titleLbl);

            var btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184), // slate-400
                Location = new Point(590, 18),
                Size = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.No; Close(); };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68); // Red hover
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(148, 163, 184);
            headerPanel.Controls.Add(btnClose);

            // Light divider below header
            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(241, 245, 249), 1f); // slate-100
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };
            Controls.Add(headerPanel);

            // ── 2. Content Layout (Vertical stacking FlowLayoutPanel) ──
            var mainFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 640,
                Location = new Point(0, 64),
                BackColor = Color.Transparent,
                Padding = new Padding(24, 16, 24, 0)
            };
            Controls.Add(mainFlow);

            // Warning Banner (Dynamic auto-height)
            var banner = new RoundedPanel
            {
                BackColor = Color.FromArgb(255, 244, 229), // #FFF4E5 light orange
                BorderColor = Color.FromArgb(254, 202, 202), // light orange border
                Width = 592,
                AutoSize = true,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, 16)
            };

            var bannerContent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Width = 560,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            banner.Controls.Add(bannerContent);

            var bannerIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(249, 115, 22),
                AutoSize = true,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Color.Transparent
            };
            bannerContent.Controls.Add(bannerIcon);

            var bannerTextFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 500,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            bannerContent.Controls.Add(bannerTextFlow);

            var bannerTitle = new Label
            {
                Text = "System May Become Unstable",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(194, 65, 12), // Dark orange
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.Transparent
            };
            bannerTextFlow.Controls.Add(bannerTitle);

            var bannerDesc = new Label
            {
                Text = "The configured parameters create an unstable queueing system because the offered load exceeds available service capacity.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(124, 45, 18),
                AutoSize = true,
                MaximumSize = new Size(490, 0), // Enable wrap
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            bannerTextFlow.Controls.Add(bannerDesc);
            mainFlow.Controls.Add(banner);

            // Parameter Summary Card (Dynamic auto-height)
            var paramCard = new RoundedPanel
            {
                BackColor = Color.White,
                BorderColor = Color.FromArgb(226, 232, 240), // slate-200
                Width = 592,
                AutoSize = true,
                Padding = new Padding(16, 12, 16, 16),
                Margin = new Padding(0, 0, 0, 16)
            };

            var cardFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 560,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            paramCard.Controls.Add(cardFlow);

            var tbl = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 5,
                AutoSize = true,
                Width = 560,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            AddParameterRow(tbl, "Arrival Rate", $"{lambda:F2} cust/hr", 0);
            AddParameterRow(tbl, "Service Rate", $"{mu:F2} cust/hr", 1);
            AddParameterRow(tbl, "Servers", $"{n}", 2);
            AddParameterRow(tbl, "Capacity", $"{n * mu:F2} cust/hr", 3);
            AddParameterRow(tbl, "Utilization", $"{rho * 100:F2}%", 4, true);
            cardFlow.Controls.Add(tbl);

            var progressBar = new CustomProgressBar(rho)
            {
                Size = new Size(560, 8),
                Margin = new Padding(0)
            };
            cardFlow.Controls.Add(progressBar);
            mainFlow.Controls.Add(paramCard);

            // Explanation Card (Dynamic auto-height)
            var infoCard = new RoundedPanel
            {
                BackColor = Color.FromArgb(239, 246, 255), // light blue
                BorderColor = Color.FromArgb(219, 234, 254),
                Width = 592,
                AutoSize = true,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, 16)
            };

            var infoContent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Width = 560,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            infoCard.Controls.Add(infoContent);

            var infoIcon = new Label
            {
                Text = "ℹ",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                AutoSize = true,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Color.Transparent
            };
            infoContent.Controls.Add(infoIcon);

            var infoText = new Label
            {
                Text = "• This system is theoretically unstable.\n• Customers may continue accumulating because demand exceeds service capacity.\n• The simulation can still be executed to observe transient queue growth.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                MaximumSize = new Size(510, 0), // Enable wrap
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            infoContent.Controls.Add(infoText);
            mainFlow.Controls.Add(infoCard);

            // ── 3. Bottom Button Panel (80px height) ──
            var buttonsPanel = new Panel
            {
                Size = new Size(640, 80),
                BackColor = Color.Transparent
            };
            
            // Cancel button left-aligned
            var btnCancel = new RoundedButton
            {
                Text = "Cancel",
                DialogResult = DialogResult.No,
                Size = new Size(160, 48),
                Location = new Point(24, 16),
                BackColor = Color.White,
                BorderColor = Color.FromArgb(203, 213, 225), // slate-300
                ForeColor = Color.FromArgb(71, 85, 105), // slate-600
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
            };
            buttonsPanel.Controls.Add(btnCancel);

            // Run Anyway button right-aligned
            var btnRun = new RoundedButton
            {
                Text = "Run Anyway",
                DialogResult = DialogResult.Yes,
                Size = new Size(160, 48),
                Location = new Point(456, 16),
                BackColor = Color.FromArgb(249, 115, 22), // Orange Accent
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
            };
            buttonsPanel.Controls.Add(btnRun);
            Controls.Add(buttonsPanel);

            // Perform manual height layout adjustment to support dynamic size
            mainFlow.PerformLayout();
            mainFlow.Height = mainFlow.PreferredSize.Height;
            buttonsPanel.Location = new Point(0, headerPanel.Height + mainFlow.Height);

            // Set Form size to perfectly fit standard panels
            this.Size = new Size(640, headerPanel.Height + mainFlow.Height + buttonsPanel.Height + 8);

            // Drag form support on header
            headerPanel.MouseDown += FormDrag_MouseDown;
        }

        private void AddParameterRow(TableLayoutPanel tbl, string label, string val, int row, bool isUtil = false)
        {
            var lblName = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9.5f, isUtil ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isUtil ? Color.FromArgb(220, 38, 38) : Color.FromArgb(100, 116, 139), // slate-400 or warning red
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 6)
            };

            var lblVal = new Label
            {
                Text = val,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = isUtil ? Color.FromArgb(220, 38, 38) : Color.FromArgb(15, 23, 42),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 6),
                Anchor = AnchorStyles.Right // align values right
            };

            tbl.Controls.Add(lblName, 0, row);
            tbl.Controls.Add(lblVal, 1, row);
        }

        // Support rounded form region & shadow border native API
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateFormRegion();
        }

        private void UpdateFormRegion()
        {
            var path = new GraphicsPath();
            int r = 28; // diameter for 14px radius
            path.AddArc(0, 0, r, r, 180, 90);
            path.AddArc(Width - r, 0, r, r, 270, 90);
            path.AddArc(Width - r, Height - r, r, r, 0, 90);
            path.AddArc(0, Height - r, r, r, 90, 90);
            path.CloseFigure();
            this.Region = new Region(path);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // Make window draggable
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void FormDrag_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }

    /// <summary>
    /// Double-buffered panel with rounded border capability.
    /// </summary>
    public class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.Transparent;
        public float BorderWidth { get; set; } = 1f;

        public RoundedPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundPath(r, Radius);

            // Fill bg
            using var bg = new SolidBrush(BackColor);
            g.FillPath(bg, path);

            // Draw border
            if (BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor, BorderWidth);
                g.DrawPath(pen, path);
            }
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

    /// <summary>
    /// Custom double-buffered button with rounded borders.
    /// </summary>
    public class RoundedButton : Button
    {
        public int Radius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.Transparent;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundPath(r, Radius);

            // Fill bg
            using var bg = new SolidBrush(BackColor);
            g.FillPath(bg, path);

            // Draw border
            if (BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor, 1f);
                g.DrawPath(pen, path);
            }

            // Text
            TextRenderer.DrawText(g, Text, Font, r, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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

    /// <summary>
    /// Custom painted utilization progress bar based on traffic intensity scale rules.
    /// </summary>
    public class CustomProgressBar : Panel
    {
        public double Value { get; set; }

        public CustomProgressBar(double val)
        {
            Value = val;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(241, 245, 249); // slate-100
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background trace
            var rBg = new Rectangle(0, 0, Width - 1, Height - 1);
            using var pathBg = RoundPath(rBg, Height / 2);
            using var brushBg = new SolidBrush(BackColor);
            g.FillPath(brushBg, pathBg);

            // Fill color rules: Green (<70%), Yellow (70-90%), Orange (90-100%), Red (>100%)
            Color fillClr;
            if (Value < 0.70)
                fillClr = Color.FromArgb(34, 197, 94); // Green
            else if (Value < 0.90)
                fillClr = Color.FromArgb(234, 179, 8); // Yellow
            else if (Value <= 1.00)
                fillClr = Color.FromArgb(249, 115, 22); // Orange
            else
                fillClr = Color.FromArgb(239, 68, 68); // Red

            double pct = Math.Min(1.0, Value);
            int fillW = (int)(Width * pct);

            if (fillW > 0)
            {
                var rFill = new Rectangle(0, 0, fillW, Height);
                using var pathFill = RoundPath(rFill, Height / 2);
                using var brushFill = new SolidBrush(fillClr);
                g.FillPath(brushFill, pathFill);
            }
        }

        private static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) d = 1;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
