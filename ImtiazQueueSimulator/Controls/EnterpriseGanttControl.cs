using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using ImtiazQueueSimulator.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Enterprise-grade Gantt Timeline Dashboard for Queueing Simulation.
    /// Modeled after Azure Monitor, Grafana, Jira Timeline & Ant Design Pro.
    /// Features:
    ///   - Interactive Top Toolbar with Zoom, Fit, PNG Export & Fullscreen mode
    ///   - Wrapping Status Legend Bar
    ///   - Pinned Left Server Column with Live Utilization Badges
    ///   - Sticky Top Time Axis with subtle grid lines
    ///   - Custom-drawn 34px rounded Gantt blocks (Row H: 70px, Gap: 24px)
    ///   - Formatted block labels (C001, 00:03 → 00:08) with zero text overlap
    ///   - Interactive Hover Tooltips & Click-to-Open Customer Detail Modal
    ///   - Bottom KPI Summary Cards
    /// </summary>
    public class EnterpriseGanttControl : UserControl
    {
        private SimulationResult? _result;
        private float _zoomLevel = 1.0f;
        private Customer? _hoveredCustomer = null;
        private ToolTip _tooltip = new ToolTip();

        // Control Layout Structure
        private Panel _toolbarPanel = null!;
        private Panel _legendPanel = null!;
        private Panel _ganttCanvas = null!;
        private Panel _summaryCardsPanel = null!;

        // Toolbar Buttons
        private Button _btnZoomIn = null!;
        private Button _btnZoomOut = null!;
        private Button _btnFit = null!;
        private Button _btnExport = null!;
        private Button _btnFullScreen = null!;

        // Summary Card Labels
        private MetricCard _cardSimTime = null!;
        private MetricCard _cardServers = null!;
        private MetricCard _cardAvgUtil = null!;
        private MetricCard _cardServed = null!;
        private MetricCard _cardIdleTime = null!;
        private MetricCard _cardAvgWait = null!;
        private MetricCard _cardAvgSvc = null!;

        // Design Tokens
        private static readonly Color PageBg       = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg       = Color.White;
        private static readonly Color TextDark     = Color.FromArgb(30, 41, 59);    // Slate 800
        private static readonly Color TextMid      = Color.FromArgb(71, 85, 105);   // Slate 600
        private static readonly Color TextMuted    = Color.FromArgb(100, 116, 139);  // Slate 500
        private static readonly Color BorderColor  = Color.FromArgb(226, 232, 240); // Slate 200
        private static readonly Color TrackBg      = Color.FromArgb(248, 250, 252); // Slate 50
        private static readonly Color GridLinePen  = Color.FromArgb(241, 245, 249); // Slate 100

        // Status Palette
        private static readonly Color ColorBusy      = Color.FromArgb(37, 99, 235);   // Blue (#2563EB)
        private static readonly Color ColorIdle      = Color.FromArgb(226, 232, 240); // Gray (#E2E8F0)
        private static readonly Color ColorWaiting   = Color.FromArgb(217, 119, 6);   // Orange/Amber (#D97706)
        private static readonly Color ColorCompleted = Color.FromArgb(16, 185, 129);  // Green (#10B981)
        private static readonly Color ColorSetup     = Color.FromArgb(124, 58, 237);  // Purple (#7C3AED)
        private static readonly Color ColorBreak     = Color.FromArgb(236, 72, 153);  // Pink (#EC4899)
        private static readonly Color ColorOverload  = Color.FromArgb(220, 38, 38);   // Red (#DC2626)

        public event Action<Customer>? OnCustomerSelected;

        public EnterpriseGanttControl()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            DoubleBuffered = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            int y = 0;

            // ── 1. TOP TOOLBAR ────────────────────────────────────────────────
            _toolbarPanel = new Panel
            {
                Location  = new Point(0, y),
                Height    = 60,
                Dock      = DockStyle.Top,
                BackColor = CardBg
            };
            _toolbarPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, _toolbarPanel.Height - 1, _toolbarPanel.Width, _toolbarPanel.Height - 1);
            };
            Controls.Add(_toolbarPanel);

            var titleFlow = new FlowLayoutPanel
            {
                Location      = new Point(20, 10),
                Size          = new Size(500, 42),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            _toolbarPanel.Controls.Add(titleFlow);

            var lblTitle = new Label
            {
                Text      = "📊 SERVER ACTIVITY TIMELINE (GANTT CHART)",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 2)
            };
            titleFlow.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text      = "Visualize server utilization and customer service activity.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Margin    = new Padding(0)
            };
            titleFlow.Controls.Add(lblSub);

            // Action Buttons Flow (Right Aligned)
            var actionFlow = new FlowLayoutPanel
            {
                Anchor        = AnchorStyles.Top | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent
            };
            _toolbarPanel.Controls.Add(actionFlow);

            _btnZoomIn     = CreateToolbarButton("🔍 + Zoom In",   (s, e) => ChangeZoom(1.25f));
            _btnZoomOut    = CreateToolbarButton("🔍 - Zoom Out",  (s, e) => ChangeZoom(0.8f));
            _btnFit        = CreateToolbarButton("⤢ Fit View",     (s, e) => ResetZoom());
            _btnExport     = CreateToolbarButton("📷 Export PNG",  (s, e) => ExportPNG());
            _btnFullScreen = CreateToolbarButton("⛶ Full Screen", (s, e) => ToggleFullScreen());

            actionFlow.Controls.AddRange(new Control[] { _btnZoomIn, _btnZoomOut, _btnFit, _btnExport, _btnFullScreen });
            _toolbarPanel.Resize += (s, e) =>
            {
                actionFlow.Location = new Point(_toolbarPanel.Width - actionFlow.PreferredSize.Width - 20, 11);
            };

            // ── 2. LEGEND BAR ─────────────────────────────────────────────────
            _legendPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 42,
                BackColor = TrackBg
            };
            _legendPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, _legendPanel.Height - 1, _legendPanel.Width, _legendPanel.Height - 1);
            };
            Controls.Add(_legendPanel);

            var legendFlow = new FlowLayoutPanel
            {
                Location      = new Point(20, 8),
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(12, 4, 12, 4)
            };
            _legendPanel.Controls.Add(legendFlow);

            AddLegendBadge(legendFlow, "Busy Service", ColorBusy);
            AddLegendBadge(legendFlow, "Idle", ColorIdle);
            AddLegendBadge(legendFlow, "Waiting", ColorWaiting);
            AddLegendBadge(legendFlow, "Completed", ColorCompleted);
            AddLegendBadge(legendFlow, "Setup / Active", ColorSetup);
            AddLegendBadge(legendFlow, "Break", ColorBreak);
            AddLegendBadge(legendFlow, "Overload", ColorOverload);

            // ── 3. BOTTOM SUMMARY CARDS ───────────────────────────────────────
            _summaryCardsPanel = new FlowLayoutPanel
            {
                Dock         = DockStyle.Bottom,
                Height       = 175,
                BackColor    = PageBg,
                Padding      = new Padding(20, 14, 20, 14),
                WrapContents = true
            };
            Controls.Add(_summaryCardsPanel);

            _cardSimTime  = CreateMetricCard("SIMULATION TIME", "--:--:--", "total duration", ColorBusy);
            _cardServers  = CreateMetricCard("SERVERS",         "--",       "active cashiers", ColorSetup);
            _cardAvgUtil  = CreateMetricCard("AVG UTILIZATION", "--",       "server utilization", ColorCompleted);
            _cardServed   = CreateMetricCard("SERVED",          "0",        "customers completed", ColorCompleted);
            _cardIdleTime = CreateMetricCard("TOTAL IDLE TIME", "--",       "idle capacity", ColorWaiting);
            _cardAvgWait  = CreateMetricCard("AVG WAIT TIME",   "--",       "minutes wait", ColorWaiting);
            _cardAvgSvc   = CreateMetricCard("AVG SERVICE TIME", "--",      "minutes service", ColorBusy);

            _summaryCardsPanel.Controls.AddRange(new Control[]
            {
                _cardSimTime, _cardServers, _cardAvgUtil, _cardServed, _cardIdleTime, _cardAvgWait, _cardAvgSvc
            });

            // ── 4. MAIN GANTT CANVAS (DOCK FILL) ──────────────────────────────
            _ganttCanvas = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = CardBg,
                AutoScroll = true
            };
            _ganttCanvas.Paint += PaintGanttCanvas;
            _ganttCanvas.MouseMove += GanttCanvas_MouseMove;
            _ganttCanvas.MouseClick += GanttCanvas_MouseClick;
            Controls.Add(_ganttCanvas);

            // Correct winforms dock ordering: Top & Bottom added first, Fill last!
            _ganttCanvas.BringToFront();
            _toolbarPanel.SendToBack();

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private void LayoutControls()
        {
            if (_summaryCardsPanel == null) return;
            int availW = Math.Max(400, ClientSize.Width - 40);
            _summaryCardsPanel.Width = availW;

            int cardCount = _summaryCardsPanel.Controls.Count;
            if (cardCount > 0)
            {
                int cardsPerRow = Math.Max(1, Math.Min(7, availW / 170));
                int cardW = Math.Max(150, (availW - (cardsPerRow - 1) * 12) / cardsPerRow);
                foreach (Control c in _summaryCardsPanel.Controls)
                {
                    if (c is MetricCard mc) mc.Width = cardW;
                }
            }
        }

        private Button CreateToolbarButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextDark,
                BackColor = CardBg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(110, 36),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.BorderSize  = 1;
            btn.Click += onClick;
            return btn;
        }

        private void AddLegendBadge(Panel parent, string text, Color color)
        {
            var p = new Panel
            {
                AutoSize  = true,
                Margin    = new Padding(0, 0, 18, 0),
                BackColor = Color.Transparent
            };

            var dot = new Panel
            {
                Size      = new Size(12, 12),
                Location  = new Point(0, 4),
                BackColor = color
            };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundPath(new Rectangle(0, 0, 11, 11), 3);
                using var b = new SolidBrush(color);
                e.Graphics.FillPath(b, path);
            };
            p.Controls.Add(dot);

            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextMid,
                AutoSize  = true,
                Location  = new Point(16, 1)
            };
            p.Controls.Add(lbl);
            parent.Controls.Add(p);
        }

        private MetricCard CreateMetricCard(string title, string val, string sub, Color accent)
        {
            return new MetricCard
            {
                Title       = title,
                Value       = val,
                Subtitle    = sub,
                AccentColor = accent,
                Size        = new Size(175, 145),
                Margin      = new Padding(0, 0, 12, 0)
            };
        }

        // ── Zoom & Actions ────────────────────────────────────────────────────
        private void ChangeZoom(float factor)
        {
            _zoomLevel = Math.Max(0.5f, Math.Min(4.0f, _zoomLevel * factor));
            _ganttCanvas.Invalidate();
        }

        private void ResetZoom()
        {
            _zoomLevel = 1.0f;
            _ganttCanvas.Invalidate();
        }

        private void ExportPNG()
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter   = "PNG Image (*.png)|*.png",
                    FileName = $"GanttTimeline_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using var bmp = new Bitmap(_ganttCanvas.Width, _ganttCanvas.Height);
                    _ganttCanvas.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(sfd.FileName, ImageFormat.Png);
                    MessageBox.Show("Gantt timeline exported successfully as PNG!", "Export Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleFullScreen()
        {
            var form = new Form
            {
                Text            = "Server Activity Timeline — Enterprise Full Screen View",
                WindowState     = FormWindowState.Maximized,
                StartPosition   = FormStartPosition.CenterScreen,
                BackColor       = PageBg,
                FormBorderStyle = FormBorderStyle.Sizable
            };

            var fullCtrl = new EnterpriseGanttControl { Dock = DockStyle.Fill };
            if (_result != null) fullCtrl.LoadResults(_result);
            form.Controls.Add(fullCtrl);
            form.ShowDialog();
        }

        // ── Data API ──────────────────────────────────────────────────────────
        public void LoadResults(SimulationResult result)
        {
            _result = result;

            if (result != null)
            {
                _cardSimTime.Value  = Customer.FormatTime(result.SimulationTime);
                _cardServers.Value  = $"{result.NumServers}";
                _cardAvgUtil.Value  = double.IsNaN(result.SimRho) ? "--" : $"{result.SimRho * 100:F1}%";
                _cardServed.Value   = $"{result.CustomersServed}";
                _cardAvgWait.Value  = double.IsNaN(result.SimWq) ? "--" : $"{result.SimWq * 60:F1} m";
                _cardAvgSvc.Value   = double.IsNaN(result.SimW) ? "--" : $"{result.SimW * 60:F1} m";

                double totalIdle = 0;
                if (result.ServerUtilizations != null && result.ServerUtilizations.Length > 0)
                {
                    foreach (var util in result.ServerUtilizations)
                    {
                        totalIdle += Math.Max(0, (1.0 - util) * result.SimulationTime);
                    }
                }
                _cardIdleTime.Value = Customer.FormatDuration(totalIdle);
            }

            _ganttCanvas.Invalidate();
            LayoutControls();
        }

        // ── Main GDI+ Gantt Renderer ──────────────────────────────────────────
        private void PaintGanttCanvas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int totalW = _ganttCanvas.Width;
            int totalH = _ganttCanvas.Height;

            int leftColW  = 180; // Fixed Left Column for Server Names & Util Badges
            int topAxisH  = 42;  // Sticky Top Time Axis
            int rowH      = 70;  // Specification: Row height 70px
            int rowGap    = 24;  // Specification: Gap between rows 24px
            int blockH    = 34;  // Specification: Block height 34px

            int timelineX = leftColW + 15;
            int timelineW = (int)((totalW - timelineX - 30) * _zoomLevel);
            timelineW = Math.Max(300, timelineW);

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                DrawEmptyState(g, new Rectangle(leftColW, topAxisH, totalW - leftColW, totalH - topAxisH),
                    "No simulation results loaded. Run a simulation to populate the Gantt Chart.");
                return;
            }

            int numServers = Math.Max(1, _result.NumServers);
            double maxTime = _result.AllCustomers.Where(c => c.Status == "Completed" || c.DepartureTime > 0)
                                                 .Select(c => c.DepartureTime)
                                                 .DefaultIfEmpty(_result.SimulationTime)
                                                 .Max();
            if (maxTime <= 0) maxTime = Math.Max(1.0, _result.SimulationTime);

            int gridBottomY = topAxisH + numServers * (rowH + rowGap);

            // ── A. Sticky Top Time Axis ───────────────────────────────────────
            using (var axisBg = new SolidBrush(TrackBg))
                g.FillRectangle(axisBg, 0, 0, totalW, topAxisH);

            using (var axisPen = new Pen(BorderColor, 1.2f))
                g.DrawLine(axisPen, 0, topAxisH - 1, totalW, topAxisH - 1);

            int numTicks = Math.Max(4, (int)(8 * _zoomLevel));
            using (var font = new Font("Segoe UI Semibold", 8.5f))
            using (var brush = new SolidBrush(TextMid))
            using (var gridPen = new Pen(GridLinePen, 1f) { DashStyle = DashStyle.Dash })
            {
                for (int i = 0; i <= numTicks; i++)
                {
                    float tx = timelineX + (float)i / numTicks * timelineW;

                    // Vertical grid line through rows
                    g.DrawLine(gridPen, tx, topAxisH, tx, gridBottomY);

                    // Time tick label
                    double t = (double)i / numTicks * maxTime;
                    string timeStr = Customer.FormatTime(t);
                    g.DrawString(timeStr, font, brush, new RectangleF(tx - 35, 12, 70, 20),
                        new StringFormat { Alignment = StringAlignment.Center });
                }
            }

            // ── B. Server Rows & Customer Blocks ──────────────────────────────
            Color[] barPalette = new Color[]
            {
                ColorBusy, ColorCompleted, ColorSetup, ColorWaiting, ColorBreak, Color.FromArgb(14, 165, 233)
            };

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var sfLeft   = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            var sfRight  = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            for (int s = 1; s <= numServers; s++)
            {
                int rowY  = topAxisH + (s - 1) * (rowH + rowGap) + 12;
                int barY  = rowY + (rowH - blockH) / 2;

                double serverUtil = (_result.ServerUtilizations != null && _result.ServerUtilizations.Length >= s)
                                  ? _result.ServerUtilizations[s - 1] * 100.0 : 0;

                // 1. Pinned Left Server Card Panel
                var leftCardRect = new Rectangle(12, rowY, leftColW - 20, rowH);
                using (var path = RoundPath(leftCardRect, 10))
                {
                    using var cardBgBrush = new SolidBrush(CardBg);
                    g.FillPath(cardBgBrush, path);
                    using var borderPen = new Pen(BorderColor, 1.2f);
                    g.DrawPath(borderPen, path);
                }

                // Server Name text
                using (var f = new Font("Segoe UI Bold", 9.5f))
                using (var b = new SolidBrush(TextDark))
                    g.DrawString($"Cashier {s:D2}", f, b, leftCardRect.X + 12, leftCardRect.Y + 12);

                // Server Utilization Badge
                Color utilBg = serverUtil > 85 ? Color.FromArgb(254, 242, 242)
                             : serverUtil > 65 ? Color.FromArgb(254, 252, 232)
                             : Color.FromArgb(240, 253, 244);

                Color utilFg = serverUtil > 85 ? ColorOverload
                             : serverUtil > 65 ? ColorWaiting
                             : ColorCompleted;

                var badgeRect = new Rectangle(leftCardRect.X + 12, leftCardRect.Y + 36, 115, 22);
                using (var path = RoundPath(badgeRect, 6))
                {
                    using var bgB = new SolidBrush(utilBg);
                    g.FillPath(bgB, path);
                    using var borderP = new Pen(utilFg, 1f);
                    g.DrawPath(borderP, path);
                }

                using (var f = new Font("Segoe UI Semibold", 8f))
                using (var b = new SolidBrush(utilFg))
                    g.DrawString($"Utilization {serverUtil:F0}%", f, b, badgeRect, sfCenter);

                // 2. Row Idle Background Track
                var trackRect = new Rectangle(timelineX, barY, timelineW, blockH);
                using (var path = RoundPath(trackRect, 8))
                {
                    using var idleBrush = new SolidBrush(TrackBg);
                    g.FillPath(idleBrush, path);
                    using var borderPen = new Pen(BorderColor, 1f);
                    g.DrawPath(borderPen, path);
                }

                // 3. Customer Service Blocks
                int serverId = s;
                var serverCustomers = _result.AllCustomers
                    .Where(c => c.AssignedServer == serverId && (c.Status == "Completed" || c.DepartureTime > 0 || c.ServiceStartTime > 0))
                    .OrderBy(c => c.ServiceStartTime)
                    .ToList();

                foreach (var c in serverCustomers)
                {
                    double startT = c.ServiceStartTime;
                    double endT   = c.DepartureTime > startT ? c.DepartureTime : Math.Min(maxTime, startT + c.ServiceTime);
                    if (endT <= startT) continue;

                    float bx1 = timelineX + (float)(startT / maxTime * timelineW);
                    float bx2 = timelineX + (float)(endT / maxTime * timelineW);
                    float bw  = Math.Max(4f, bx2 - bx1);

                    var blockRect = new RectangleF(bx1, barY, bw, blockH);

                    Color color = barPalette[(c.Id - 1) % barPalette.Length];

                    // Draw rounded block
                    using (var blockBrush = new SolidBrush(Color.FromArgb(225, color)))
                    {
                        if (bw > 8)
                        {
                            using var path = RoundPath(Rectangle.Round(blockRect), 8);
                            g.FillPath(blockBrush, path);
                        }
                        else
                        {
                            g.FillRectangle(blockBrush, blockRect);
                        }
                    }

                    using (var borderPen = new Pen(color, 1.2f))
                    {
                        if (bw > 8)
                        {
                            using var path = RoundPath(Rectangle.Round(blockRect), 8);
                            g.DrawPath(borderPen, path);
                        }
                        else
                        {
                            g.DrawRectangle(borderPen, blockRect.X, blockRect.Y, blockRect.Width, blockRect.Height);
                        }
                    }

                    // Render block labels: C001 and Time Range (00:03 → 00:08)
                    if (bw >= 150)
                    {
                        using var fBold = new Font("Segoe UI Bold", 8.5f);
                        using var fSub  = new Font("Segoe UI Semibold", 7.5f);
                        using var bText = new SolidBrush(Color.White);

                        string custTitle = $"C{c.Id:D3}";
                        string timeRange = $"{Customer.FormatTime(startT)} → {Customer.FormatTime(endT)}";

                        g.DrawString(custTitle, fBold, bText, new RectangleF(bx1 + 8, barY + 2, bw - 16, 16), sfLeft);
                        g.DrawString(timeRange, fSub,  bText, new RectangleF(bx1 + 8, barY + 16, bw - 16, 15), sfLeft);
                    }
                    else if (bw >= 65)
                    {
                        using var fBold = new Font("Segoe UI Bold", 8.5f);
                        using var bText = new SolidBrush(Color.White);
                        g.DrawString($"C{c.Id:D3}", fBold, bText, blockRect, sfCenter);
                    }
                    else if (bw >= 30)
                    {
                        using var fBold = new Font("Segoe UI Bold", 7.5f);
                        using var bText = new SolidBrush(Color.White);
                        g.DrawString($"{c.Id}", fBold, bText, blockRect, sfCenter);
                    }
                }
            }
        }

        // ── Mouse Hit Testing & Interaction ──────────────────────────────────
        private void GanttCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_result == null || _result.AllCustomers.Count == 0) return;

            int leftColW = 180;
            int topAxisH = 42;
            int rowH     = 70;
            int rowGap   = 24;
            int blockH   = 34;

            int timelineX = leftColW + 15;
            int timelineW = (int)((_ganttCanvas.Width - timelineX - 30) * _zoomLevel);
            timelineW = Math.Max(300, timelineW);

            int numServers = Math.Max(1, _result.NumServers);
            double maxTime = _result.AllCustomers.Where(c => c.Status == "Completed" || c.DepartureTime > 0)
                                                 .Select(c => c.DepartureTime)
                                                 .DefaultIfEmpty(_result.SimulationTime)
                                                 .Max();
            if (maxTime <= 0) maxTime = Math.Max(1.0, _result.SimulationTime);

            Customer? hovered = null;

            for (int s = 1; s <= numServers; s++)
            {
                int rowY = topAxisH + (s - 1) * (rowH + rowGap) + 12;
                int barY = rowY + (rowH - blockH) / 2;

                if (e.Y >= barY && e.Y <= barY + blockH && e.X >= timelineX && e.X <= timelineX + timelineW)
                {
                    double timeAtMouse = (e.X - timelineX) / (double)timelineW * maxTime;
                    int serverId = s;

                    hovered = _result.AllCustomers.FirstOrDefault(c =>
                        c.AssignedServer == serverId &&
                        c.ServiceStartTime <= timeAtMouse &&
                        (c.DepartureTime > c.ServiceStartTime ? c.DepartureTime : maxTime) >= timeAtMouse);

                    if (hovered != null) break;
                }
            }

            if (hovered != _hoveredCustomer)
            {
                _hoveredCustomer = hovered;
                if (hovered != null)
                {
                    _ganttCanvas.Cursor = Cursors.Hand;
                    string tipText =
                        $"👤 Customer: {hovered.Name} (C{hovered.Id:D3})\n" +
                        $"🖥 Server: Cashier {hovered.AssignedServer:D2}\n" +
                        $"⏱ Store Arrival: {Customer.FormatTime(hovered.ArrivalTime)}\n" +
                        $"⚡ Service Start: {Customer.FormatTime(hovered.ServiceStartTime)}\n" +
                        $"🏁 End Service:   {Customer.FormatTime(hovered.DepartureTime)}\n" +
                        $"⏳ Waiting Time:   {Customer.FormatDuration(hovered.WaitingTime)}\n" +
                        $"💳 Service Time:   {Customer.FormatDuration(hovered.ServiceTime)}\n" +
                        $"⏱ System Time:    {Customer.FormatDuration(hovered.TimeInSystem)}\n\n" +
                        "👉 Click block to view complete customer profile dialog";

                    _tooltip.Show(tipText, _ganttCanvas, e.X + 15, e.Y + 15, 4000);
                }
                else
                {
                    _ganttCanvas.Cursor = Cursors.Default;
                    _tooltip.Hide(_ganttCanvas);
                }
            }
        }

        private void GanttCanvas_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_hoveredCustomer != null)
            {
                OnCustomerSelected?.Invoke(_hoveredCustomer);
                var modal = new CustomerDetailForm(_hoveredCustomer);
                modal.ShowDialog();
            }
        }

        private void DrawEmptyState(Graphics g, Rectangle area, string message)
        {
            using var font = new Font("Segoe UI Semibold", 9.5f);
            using var brush = new SolidBrush(TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString($"ℹ  {message}", font, brush, area, sf);
        }

        private GraphicsPath RoundPath(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
