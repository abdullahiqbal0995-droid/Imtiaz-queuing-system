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
    /// Senior Architect-grade Enterprise Gantt Timeline Control.
    /// Features:
    ///   - Modular Card Sections (Header, Toolbar, Legend, Timeline Canvas, Summary Cards, Help Card)
    ///   - Pinned Server Column on Left (Scrolling timeline, sticky labels with Utilization %)
    ///   - Dynamic Sticky Time Axis (5 / 10 / 15 min ticks)
    ///   - 70px Row Height, 34px Activity Bar Height, 8px Radius, Min 12px Visible Width
    ///   - Customer Search Filter & Server Filter
    ///   - Zoom (+, -, Fit, Reset), Export PNG, Fullscreen Mode
    ///   - Rich Hover Tooltips & Click-to-Open Customer Detail Modal
    ///   - 8 Executive Metric Summary Cards
    /// </summary>
    public class EnterpriseGanttControl : UserControl
    {
        private SimulationResult? _result;
        private float _zoomLevel = 1.0f;
        private string _searchQuery = "";
        private int _selectedServerFilter = 0; // 0 = All Servers
        private Customer? _hoveredCustomer = null;
        private ToolTip _tooltip = new ToolTip();

        // ── Card Containers ────────────────────────────────────────────────────
        private Panel _mainScrollContainer = null!;
        private Panel _headerCard = null!;
        private Panel _toolbarCard = null!;
        private Panel _legendCard = null!;
        private Panel _timelineCard = null!;
        private Panel _ganttCanvas = null!;
        private FlowLayoutPanel _summaryCardsPanel = null!;
        private Panel _helpCard = null!;

        // ── Toolbar Controls ───────────────────────────────────────────────────
        private Button _btnZoomIn = null!;
        private Button _btnZoomOut = null!;
        private Button _btnFit = null!;
        private Button _btnReset = null!;
        private Button _btnExport = null!;
        private Button _btnFullScreen = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cmbServerFilter = null!;

        // ── Summary KPI Metric Cards ───────────────────────────────────────────
        private MetricCard _cardSimTime = null!;
        private MetricCard _cardServed = null!;
        private MetricCard _cardAvgWait = null!;
        private MetricCard _cardAvgSvc = null!;
        private MetricCard _cardIdleTime = null!;
        private MetricCard _cardPeakQueue = null!;
        private MetricCard _cardAvgUtil = null!;
        private MetricCard _cardThroughput = null!;

        // ── Design System Tokens ───────────────────────────────────────────────
        private static readonly Color PageBg       = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg       = Color.White;
        private static readonly Color TextDark     = Color.FromArgb(30, 41, 59);    // Slate 800
        private static readonly Color TextMid      = Color.FromArgb(71, 85, 105);   // Slate 600
        private static readonly Color TextMuted    = Color.FromArgb(100, 116, 139);  // Slate 500
        private static readonly Color BorderColor  = Color.FromArgb(226, 232, 240); // Slate 200
        private static readonly Color TrackBg      = Color.FromArgb(248, 250, 252); // Slate 50
        private static readonly Color GridLinePen  = Color.FromArgb(241, 245, 249); // Slate 100
        private static readonly Color HighlightGold = Color.FromArgb(234, 179, 8);   // Amber 500

        // Palette
        private static readonly Color ColorBusy      = Color.FromArgb(37, 99, 235);   // Blue
        private static readonly Color ColorIdle      = Color.FromArgb(226, 232, 240); // Gray
        private static readonly Color ColorWaiting   = Color.FromArgb(217, 119, 6);   // Orange
        private static readonly Color ColorCompleted = Color.FromArgb(16, 185, 129);  // Green
        private static readonly Color ColorSetup     = Color.FromArgb(124, 58, 237);  // Purple
        private static readonly Color ColorBreak     = Color.FromArgb(236, 72, 153);  // Pink
        private static readonly Color ColorOverload  = Color.FromArgb(220, 38, 38);   // Red

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

            _mainScrollContainer = new Panel
            {
                Location   = new Point(0, 0),
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = PageBg,
                Padding    = new Padding(24)
            };
            Controls.Add(_mainScrollContainer);

            int y = 20;

            // ── 1. HEADER CARD ────────────────────────────────────────────────
            _headerCard = CreateSectionCard(y, 70);
            var lblTitle = new Label
            {
                Text      = "📊 SERVER ACTIVITY TIMELINE (GANTT MONITORING DASHBOARD)",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(20, 12)
            };
            _headerCard.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text      = "Enterprise visualization of server utilization, customer checkout activity, and waiting metrics.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Location  = new Point(20, 38)
            };
            _headerCard.Controls.Add(lblSub);
            _mainScrollContainer.Controls.Add(_headerCard);
            y += 70 + 24;

            // ── 2. TOOLBAR & FILTERS CARD ─────────────────────────────────────
            _toolbarCard = CreateSectionCard(y, 64);

            var toolFlow = new FlowLayoutPanel
            {
                Location      = new Point(16, 13),
                Height        = 40,
                Width         = _toolbarCard.Width - 32,
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            _toolbarCard.Controls.Add(toolFlow);

            _btnZoomIn     = CreateButton("🔍 + Zoom In",   (s, e) => ChangeZoom(1.25f));
            _btnZoomOut    = CreateButton("🔍 - Zoom Out",  (s, e) => ChangeZoom(0.8f));
            _btnFit        = CreateButton("⤢ Fit View",     (s, e) => ResetZoom());
            _btnReset      = CreateButton("↺ Reset",        (s, e) => ResetFilters());
            _btnExport     = CreateButton("📷 Export PNG",  (s, e) => ExportPNG());
            _btnFullScreen = CreateButton("⛶ Full Screen", (s, e) => ToggleFullScreen());

            // Search Customer Input
            var pnlSearch = new Panel { Size = new Size(180, 34), Margin = new Padding(0, 0, 10, 0), BackColor = Color.FromArgb(248, 250, 252) };
            pnlSearch.Paint += (s, e) => {
                using var pen = new Pen(BorderColor, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };
            _txtSearch = new TextBox
            {
                Text          = "Search Customer...",
                ForeColor     = TextMuted,
                Font          = new Font("Segoe UI", 8.5f),
                BorderStyle   = BorderStyle.None,
                Location      = new Point(8, 8),
                Width         = 164,
                BackColor     = Color.FromArgb(248, 250, 252)
            };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search Customer...") { _txtSearch.Text = ""; _txtSearch.ForeColor = TextDark; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = "Search Customer..."; _txtSearch.ForeColor = TextMuted; } };
            _txtSearch.TextChanged += (s, e) => {
                _searchQuery = _txtSearch.Text == "Search Customer..." ? "" : _txtSearch.Text.Trim();
                _ganttCanvas.Invalidate();
            };
            pnlSearch.Controls.Add(_txtSearch);

            // Server Filter Dropdown
            _cmbServerFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI Semibold", 8.5f),
                Size          = new Size(130, 34),
                Margin        = new Padding(0, 0, 10, 0)
            };
            _cmbServerFilter.Items.Add("All Servers");
            _cmbServerFilter.SelectedIndex = 0;
            _cmbServerFilter.SelectedIndexChanged += (s, e) => {
                _selectedServerFilter = _cmbServerFilter.SelectedIndex;
                _ganttCanvas.Invalidate();
            };

            toolFlow.Controls.AddRange(new Control[]
            {
                _btnZoomIn, _btnZoomOut, _btnFit, _btnReset, _btnExport, _btnFullScreen, pnlSearch, _cmbServerFilter
            });

            _mainScrollContainer.Controls.Add(_toolbarCard);
            y += 64 + 24;

            // ── 3. LEGEND CARD ────────────────────────────────────────────────
            _legendCard = CreateSectionCard(y, 52);
            var legendFlow = new FlowLayoutPanel
            {
                Location      = new Point(16, 12),
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(12, 4, 12, 4)
            };
            _legendCard.Controls.Add(legendFlow);

            AddLegendBadge(legendFlow, "Busy Service", ColorBusy);
            AddLegendBadge(legendFlow, "Idle", ColorIdle);
            AddLegendBadge(legendFlow, "Waiting", ColorWaiting);
            AddLegendBadge(legendFlow, "Customer Completed", ColorCompleted);
            AddLegendBadge(legendFlow, "Setup / Active", ColorSetup);
            AddLegendBadge(legendFlow, "Break", ColorBreak);
            AddLegendBadge(legendFlow, "Offline / Overload", ColorOverload);

            _mainScrollContainer.Controls.Add(_legendCard);
            y += 52 + 24;

            // ── 4. TIMELINE CANVAS CARD ───────────────────────────────────────
            _timelineCard = CreateSectionCard(y, 420);
            _ganttCanvas = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = CardBg,
                AutoScroll = true
            };
            _ganttCanvas.Paint += PaintGanttCanvas;
            _ganttCanvas.MouseMove += GanttCanvas_MouseMove;
            _ganttCanvas.MouseClick += GanttCanvas_MouseClick;
            _timelineCard.Controls.Add(_ganttCanvas);

            _mainScrollContainer.Controls.Add(_timelineCard);
            y += 420 + 24;

            // ── 5. SUMMARY KPI CARDS ──────────────────────────────────────────
            _summaryCardsPanel = new FlowLayoutPanel
            {
                Location     = new Point(0, y),
                Width        = _mainScrollContainer.Width - 48,
                Height       = 160,
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor    = Color.Transparent,
                WrapContents = true
            };

            _cardSimTime   = CreateMetricCard("SIMULATION TIME", "--:--:--", "total duration", ColorBusy);
            _cardServed    = CreateMetricCard("SERVED",          "0",        "customers completed", ColorCompleted);
            _cardAvgWait   = CreateMetricCard("AVG WAIT (Wq)",   "--",       "minutes wait", ColorWaiting);
            _cardAvgSvc    = CreateMetricCard("AVG SERVICE (W)", "--",       "minutes service", ColorBusy);
            _cardIdleTime  = CreateMetricCard("TOTAL IDLE TIME", "--",       "idle capacity", ColorWaiting);
            _cardPeakQueue = CreateMetricCard("PEAK QUEUE (Lq)", "0",        "max queue length", ColorOverload);
            _cardAvgUtil   = CreateMetricCard("UTILIZATION",     "--",       "server workload", ColorSetup);
            _cardThroughput= CreateMetricCard("THROUGHPUT (λ)",  "--",       "cust / hour", ColorCompleted);

            _summaryCardsPanel.Controls.AddRange(new Control[]
            {
                _cardSimTime, _cardServed, _cardAvgWait, _cardAvgSvc, _cardIdleTime, _cardPeakQueue, _cardAvgUtil, _cardThroughput
            });
            _mainScrollContainer.Controls.Add(_summaryCardsPanel);
            y += 160 + 24;

            // ── 6. HELP SECTION CARD ──────────────────────────────────────────
            _helpCard = CreateSectionCard(y, 100);
            var lblHelpTitle = new Label
            {
                Text      = "💡 Interactive Timeline Guidance & Keyboard Shortcuts",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(20, 14)
            };
            _helpCard.Controls.Add(lblHelpTitle);

            var lblHelpBody = new Label
            {
                Text      = "• Hover over any customer activity block to inspect full journey metrics (Arrival, Queue Entry, Service Start, Departure).\n" +
                            "• Click any block to launch the Customer Detail Dialog. Use Search Box to highlight specific customers.\n" +
                            "• Server panel remains pinned on the left during horizontal scrolling. Use Zoom buttons to adjust time scale.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextMid,
                AutoSize  = true,
                Location  = new Point(20, 38)
            };
            _helpCard.Controls.Add(lblHelpBody);
            _mainScrollContainer.Controls.Add(_helpCard);
            y += 100 + 40;

            _mainScrollContainer.Resize += (s, e) => LayoutCustomCards();
            LayoutCustomCards();
        }

        private Panel CreateSectionCard(int y, int height)
        {
            var p = new Panel
            {
                Location  = new Point(0, y),
                Height    = height,
                Width     = Math.Max(400, ClientSize.Width - 48),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = CardBg
            };

            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);

                using (var bgBrush = new SolidBrush(CardBg))
                {
                    using var path = RoundPath(r, 16);
                    g.FillPath(bgBrush, path);
                }
                using (var pen = new Pen(BorderColor, 1.2f))
                {
                    using var path = RoundPath(r, 16);
                    g.DrawPath(pen, path);
                }
            };
            return p;
        }

        private void LayoutCustomCards()
        {
            if (_mainScrollContainer == null) return;
            int availW = Math.Max(400, _mainScrollContainer.ClientSize.Width - 48);

            _headerCard.Width   = availW;
            _toolbarCard.Width  = availW;
            _legendCard.Width   = availW;
            _timelineCard.Width  = availW;
            _helpCard.Width      = availW;
            _summaryCardsPanel.Width = availW;

            int cardCount = _summaryCardsPanel.Controls.Count;
            if (cardCount > 0)
            {
                int cardsPerRow = Math.Max(1, Math.Min(8, availW / 160));
                int cardW = Math.Max(140, (availW - (cardsPerRow - 1) * 12) / cardsPerRow);
                foreach (Control c in _summaryCardsPanel.Controls)
                {
                    if (c is MetricCard mc) mc.Width = cardW;
                }
            }

            int numServers = _result != null && _result.NumServers > 0 ? _result.NumServers : 1;
            int timelineH  = Math.Max(320, 80 + numServers * 94);
            _timelineCard.Height = timelineH;
        }

        private Button CreateButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextDark,
                BackColor = CardBg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(105, 34),
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
                Margin    = new Padding(0, 0, 16, 0),
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
                Size        = new Size(155, 145),
                Margin      = new Padding(0, 0, 10, 0)
            };
        }

        // ── Controls Actions ──────────────────────────────────────────────────
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

        private void ResetFilters()
        {
            _zoomLevel = 1.0f;
            _txtSearch.Text = "Search Customer...";
            _txtSearch.ForeColor = TextMuted;
            _searchQuery = "";
            _cmbServerFilter.SelectedIndex = 0;
            _selectedServerFilter = 0;
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
                Text            = "Server Activity Timeline — Enterprise Monitoring View",
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
                _cardSimTime.Value    = Customer.FormatTime(result.SimulationTime);
                _cardServed.Value     = $"{result.CustomersServed}";
                _cardAvgWait.Value    = double.IsNaN(result.SimWq) ? "--" : $"{result.SimWq * 60:F1} m";
                _cardAvgSvc.Value     = double.IsNaN(result.SimW) ? "--" : $"{result.SimW * 60:F1} m";
                _cardAvgUtil.Value    = double.IsNaN(result.SimRho) ? "--" : $"{result.SimRho * 100:F1}%";

                int peakLq = result.QueueLengthOverTime != null && result.QueueLengthOverTime.Count > 0
                           ? result.QueueLengthOverTime.Max(q => q.QueueLength) : 0;
                _cardPeakQueue.Value  = $"{peakLq}";

                double throughput = result.SimulationTime > 0 ? (result.CustomersServed / result.SimulationTime) : 0;
                _cardThroughput.Value = $"{throughput:F1}/hr";

                double totalIdle = 0;
                if (result.ServerUtilizations != null)
                {
                    foreach (var util in result.ServerUtilizations)
                        totalIdle += Math.Max(0, (1.0 - util) * result.SimulationTime);
                }
                _cardIdleTime.Value = Customer.FormatDuration(totalIdle);

                // Update Server Filter Dropdown Items
                _cmbServerFilter.Items.Clear();
                _cmbServerFilter.Items.Add("All Servers");
                for (int s = 1; s <= result.NumServers; s++)
                    _cmbServerFilter.Items.Add($"Cashier {s:D2}");
                _cmbServerFilter.SelectedIndex = 0;
            }

            LayoutCustomCards();
            _ganttCanvas.Invalidate();
        }

        // ── Main GDI+ Gantt Renderer ──────────────────────────────────────────
        private void PaintGanttCanvas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int totalW = _ganttCanvas.Width;
            int totalH = _ganttCanvas.Height;

            int leftColW  = 175; // Pinned Left Column width
            int topAxisH  = 44;  // Sticky Top Time Axis height
            int rowH      = 70;  // Specification: Row height 70px
            int rowGap    = 24;  // Specification: Gap between rows 24px
            int blockH    = 34;  // Specification: Block height 34px

            int timelineX = leftColW + 15;
            int timelineW = (int)((totalW - timelineX - 30) * _zoomLevel);
            timelineW = Math.Max(300, timelineW);

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                DrawEmptyState(g, new Rectangle(leftColW, topAxisH, totalW - leftColW, totalH - topAxisH),
                    "No simulation results loaded. Run a simulation to view the Gantt Chart.");
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

                    // Dynamic tick label (5 / 10 / 15 min intervals)
                    double t = (double)i / numTicks * maxTime;
                    string timeStr = Customer.FormatTime(t);
                    g.DrawString(timeStr, font, brush, new RectangleF(tx - 35, 12, 70, 20),
                        new StringFormat { Alignment = StringAlignment.Center });
                }
            }

            // ── B. Server Rows & Activity Bars ────────────────────────────────
            Color[] barPalette = new Color[]
            {
                ColorBusy, ColorCompleted, ColorSetup, ColorWaiting, ColorBreak, Color.FromArgb(14, 165, 233)
            };

            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var sfLeft   = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            var sfRight  = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            for (int s = 1; s <= numServers; s++)
            {
                // Server Filter check
                if (_selectedServerFilter > 0 && _selectedServerFilter != s) continue;

                int rowY  = topAxisH + (s - 1) * (rowH + rowGap) + 12;
                int barY  = rowY + (rowH - blockH) / 2;

                double serverUtil = (_result.ServerUtilizations != null && _result.ServerUtilizations.Length >= s)
                                  ? _result.ServerUtilizations[s - 1] * 100.0 : 0;

                // 1. Pinned Left Server Header Card
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

                // 2. Idle Background Track Strip
                var trackRect = new Rectangle(timelineX, barY, timelineW, blockH);
                using (var path = RoundPath(trackRect, 8))
                {
                    using var idleBrush = new SolidBrush(TrackBg);
                    g.FillPath(idleBrush, path);
                    using var borderPen = new Pen(BorderColor, 1f);
                    g.DrawPath(borderPen, path);
                }

                // 3. Customer Activity Bars
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
                    float bw  = Math.Max(12f, bx2 - bx1); // Specification: minimum visible width 12px

                    var blockRect = new RectangleF(bx1, barY, bw, blockH);

                    Color color = barPalette[(c.Id - 1) % barPalette.Length];

                    // Check if matched by search filter
                    bool isSearchMatch = !string.IsNullOrEmpty(_searchQuery) &&
                        ($"C{c.Id:D3}".IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         c.Id.ToString() == _searchQuery ||
                         c.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);

                    // Draw rounded bar (34px height, 8px radius)
                    using (var blockBrush = new SolidBrush(Color.FromArgb(225, color)))
                    {
                        using var path = RoundPath(Rectangle.Round(blockRect), 8);
                        g.FillPath(blockBrush, path);
                    }

                    using (var borderPen = new Pen(isSearchMatch ? HighlightGold : color, isSearchMatch ? 2.5f : 1.2f))
                    {
                        using var path = RoundPath(Rectangle.Round(blockRect), 8);
                        g.DrawPath(borderPen, path);
                    }

                    // Render Activity Content: Customer ID, Start Time, End Time
                    if (bw >= 140)
                    {
                        using var fBold = new Font("Segoe UI Bold", 8.5f);
                        using var fSub  = new Font("Segoe UI Semibold", 7.5f);
                        using var bText = new SolidBrush(Color.White);

                        string custTitle = $"C{c.Id:D3}";
                        string timeRange = $"{Customer.FormatTime(startT)} → {Customer.FormatTime(endT)}";

                        g.DrawString(custTitle, fBold, bText, new RectangleF(bx1 + 8, barY + 2, bw - 16, 16), sfLeft);
                        g.DrawString(timeRange, fSub,  bText, new RectangleF(bx1 + 8, barY + 16, bw - 16, 15), sfLeft);
                    }
                    else if (bw >= 50)
                    {
                        using var fBold = new Font("Segoe UI Bold", 8.5f);
                        using var bText = new SolidBrush(Color.White);
                        g.DrawString($"C{c.Id:D3}", fBold, bText, blockRect, sfCenter);
                    }
                    else if (bw >= 20)
                    {
                        using var fBold = new Font("Segoe UI Bold", 7.5f);
                        using var bText = new SolidBrush(Color.White);
                        g.DrawString($"{c.Id}", fBold, bText, blockRect, sfCenter);
                    }
                    else
                    {
                        // Specification: If duration is too short (< 20px), show clean icon/dot
                        using var fBold = new Font("Segoe UI", 7.5f);
                        using var bText = new SolidBrush(Color.White);
                        g.DrawString("👤", fBold, bText, blockRect, sfCenter);
                    }
                }
            }
        }

        // ── Mouse Hit Testing & Interaction ──────────────────────────────────
        private void GanttCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_result == null || _result.AllCustomers.Count == 0) return;

            int leftColW = 175;
            int topAxisH = 44;
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
                if (_selectedServerFilter > 0 && _selectedServerFilter != s) continue;

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
                        $"👤 Customer:     {hovered.Name} (C{hovered.Id:D3})\n" +
                        $"🖥 Server:       Cashier {hovered.AssignedServer:D2}\n" +
                        $"⏱ Store Arrival: {Customer.FormatTime(hovered.ArrivalTime)}\n" +
                        $"📥 Queue Entry:  {Customer.FormatTime(hovered.QueueEntryTime)}\n" +
                        $"⚡ Service Start:{Customer.FormatTime(hovered.ServiceStartTime)}\n" +
                        $"🏁 Departure:    {Customer.FormatTime(hovered.DepartureTime)}\n" +
                        $"⏳ Wait Time (Wq):{Customer.FormatDuration(hovered.WaitingTime)}\n" +
                        $"💳 Service Time: {Customer.FormatDuration(hovered.ServiceTime)}\n" +
                        $"⏱ System Time(W):{Customer.FormatDuration(hovered.TimeInSystem)}\n\n" +
                        "👉 Click block to open complete customer details modal";

                    _tooltip.Show(tipText, _ganttCanvas, e.X + 15, e.Y + 15, 5000);
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
