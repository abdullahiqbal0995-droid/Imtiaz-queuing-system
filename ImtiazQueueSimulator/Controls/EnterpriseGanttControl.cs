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
    /// Enterprise Gantt Monitoring Dashboard — Exact UI replica of the target design.
    /// Features:
    ///   1. Top Card: Header with Chart Icon, Title, Subtitle, and 6 Toolbar Action Buttons
    ///   2. Filter Card: Server Dropdown, 7-Item Legend, Search Box with Search Icon, Time Range Badge
    ///   3. Gantt Timeline Card: Pinned left server column (Cashier Name, Green dot, Utilization %),
    ///      sticky header time axis, multi-colored customer activity blocks (Busy, Setup, Break, etc.)
    ///   4. Selected Customer Details Panel: 3 sub-cards:
    ///        - Left: Customer profile (Name, ID, Server, Status pill, Color box)
    ///        - Middle: 8 Timing Metrics cards + Visual Progress Stepper line
    ///        - Right: Event Timeline feed list with colored dots and timestamps
    ///   5. Bottom 6 Executive KPI Metric Cards with round icon badges
    ///   6. Bottom Help Section Card
    /// </summary>
    public class EnterpriseGanttControl : UserControl
    {
        // ── State ──────────────────────────────────────────────────────────────
        private SimulationResult? _result;
        private float _zoomLevel = 1.0f;
        private string _search = "";
        private int _serverFilter = 0;   // 0 = All Servers
        private Customer? _selected = null;
        private Customer? _hovered = null;
        private ToolTip _tt = new ToolTip { InitialDelay = 300, ReshowDelay = 100 };

        // ── Main Containers ───────────────────────────────────────────────────
        private Panel _scroll = null!;
        private Panel _headerCard = null!;
        private Panel _filterCard = null!;
        private Panel _ganttWrapper = null!;
        private Panel _serverColPanel = null!;
        private Panel _canvasPanel = null!;
        private Panel _detailsCard = null!;
        private Panel _kpiWrapper = null!;
        private Panel _helpCard = null!;

        // ── Details Sub-Panels ────────────────────────────────────────────────
        private Panel _detLeftPanel = null!;
        private Panel _detCenterPanel = null!;
        private Panel _detRightPanel = null!;

        // Left Detail Controls
        private Label _lblDetTitle = null!;
        private Label _lblDetId = null!;
        private Label _lblDetServerVal = null!;
        private Label _lblDetStatusVal = null!;
        private Panel _pnlDetColorBox = null!;
        private Label _lblDetBadge = null!;

        // Center Detail Metric Labels
        private Label _lblArrVal = null!, _lblQVal = null!, _lblSvcStartVal = null!, _lblDepVal = null!;
        private Label _lblWaitVal = null!, _lblSvcTimeVal = null!, _lblSysTimeVal = null!, _lblAssignedServerVal = null!;
        private Panel _stepperPanel = null!;

        // Right Detail Event Timeline Panel
        private Panel _eventTimelineList = null!;

        // Controls
        private ComboBox _cmbServer = null!;
        private TextBox _txtSearch = null!;
        private Label _lblTimeRange = null!;

        // KPI Circular Metric Card Handles
        private Panel[] _kpiCards = new Panel[6];
        private Label[] _kpiValLabels = new Label[6];

        // ── Design Tokens & Palette ───────────────────────────────────────────
        private static readonly Color BgColor = Color.FromArgb(246, 248, 252);
        private static readonly Color WhiteBg = Color.White;
        private static readonly Color TextHeader = Color.FromArgb(30, 41, 59);      // Slate 800
        private static readonly Color TextMid = Color.FromArgb(71, 85, 105);        // Slate 600
        private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);   // Slate 400
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240); // Slate 200
        private static readonly Color TrackBgColor = Color.FromArgb(248, 250, 252);
        private static readonly Color GridPenColor = Color.FromArgb(241, 245, 249);
        private static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);    // #2563EB

        // Activity Block Palette (matches target screenshot exactly)
        private static readonly Color ClrBusy = Color.FromArgb(37, 99, 235);     // Blue #2563EB
        private static readonly Color ClrIdle = Color.FromArgb(203, 213, 225);   // Slate 300 #CBD5E1
        private static readonly Color ClrWaiting = Color.FromArgb(249, 115, 22);   // Orange #F97316
        private static readonly Color ClrCompleted = Color.FromArgb(16, 185, 129);  // Green #10B981
        private static readonly Color ClrSetup = Color.FromArgb(124, 58, 237);    // Purple #7C3AED
        private static readonly Color ClrBreak = Color.FromArgb(236, 72, 153);    // Pink/Magenta #EC4899
        private static readonly Color ClrOverload = Color.FromArgb(220, 38, 38);  // Red #DC2626

        private static readonly Color[] CustomerPalette = new Color[]
        {
            ClrBusy, ClrWaiting, ClrSetup, ClrCompleted, ClrBusy, ClrBreak, ClrBusy, ClrWaiting, ClrCompleted
        };

        // Layout Constants
        private const int RowHeight = 85;
        private const int TaskHeight = 44;
        private const int TaskRadius = 8;
        private const int AxisHeight = 44;
        private const int PinnedColWidth = 180;

        public EnterpriseGanttControl()
        {
            BackColor = BgColor;
            AutoScroll = true;
            DoubleBuffered = true;
            BuildUI();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  UI BUILDER
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            Controls.Clear();

            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BgColor,
                Padding = new Padding(20, 20, 20, 20)
            };
            Controls.Add(_scroll);

            int currentY = 0;

            // ── 1. HEADER CARD ────────────────────────────────────────────────
            _headerCard = CreateCardPanel(currentY, 78);
            BuildHeaderSection(_headerCard);
            _scroll.Controls.Add(_headerCard);
            currentY += 78 + 18;

            // ── 2. FILTER & CONTROL BAR CARD ──────────────────────────────────
            _filterCard = CreateCardPanel(currentY, 56);
            BuildFilterSection(_filterCard);
            _scroll.Controls.Add(_filterCard);
            currentY += 56 + 18;

            // ── 3. GANTT TIMELINE CARD ────────────────────────────────────────
            _ganttWrapper = CreateCardPanel(currentY, 380);
            BuildGanttSection(_ganttWrapper);
            _scroll.Controls.Add(_ganttWrapper);
            currentY += 380 + 18;

            // ── 4. CUSTOMER DETAILS PANEL CARD ────────────────────────────────
            _detailsCard = CreateCardPanel(currentY, 260);
            BuildDetailsSection(_detailsCard);
            _scroll.Controls.Add(_detailsCard);
            currentY += 260 + 18;

            // ── 5. KPI METRIC CARDS ──────────────────────────────────────────
            _kpiWrapper = new Panel
            {
                Location = new Point(0, currentY),
                Height = 135,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            BuildKPISection(_kpiWrapper);
            _scroll.Controls.Add(_kpiWrapper);
            currentY += 135 + 18;

            // ── 6. HELP CARD ──────────────────────────────────────────────────
            _helpCard = CreateCardPanel(currentY, 70);
            BuildHelpSection(_helpCard);
            _scroll.Controls.Add(_helpCard);
            currentY += 70 + 24;

            _scroll.Resize += (s, e) => RelayoutSections();
            RelayoutSections();
        }

        // ── 1. Header Section ─────────────────────────────────────────────────
        private void BuildHeaderSection(Panel card)
        {
            // Chart Icon Box
            var pnlIcon = new Panel { Size = new Size(38, 38), Location = new Point(18, 18), BackColor = Color.Transparent };
            pnlIcon.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, 36, 36);
                using var b = new SolidBrush(Color.FromArgb(239, 246, 255));
                using var p = RoundPath(r, 8);
                g.FillPath(b, p);
                // Draw mini chart bars
                using var barBrush = new SolidBrush(PrimaryBlue);
                g.FillRectangle(barBrush, 8, 20, 5, 10);
                g.FillRectangle(barBrush, 16, 12, 5, 18);
                g.FillRectangle(barBrush, 24, 7, 5, 23);
            };
            card.Controls.Add(pnlIcon);

            // Title & Subtitle
            var lblTitle = new Label
            {
                Text = "SERVER ACTIVITY TIMELINE (GANTT MONITORING DASHBOARD)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextHeader,
                AutoSize = true,
                Location = new Point(64, 16),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Visualize server utilization, customer checkout activity, and waiting metrics in real-time.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMid,
                AutoSize = true,
                Location = new Point(64, 42),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);

            // Right Action Toolbar Buttons
            var flowButtons = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Location = new Point(card.Width - 620, 20)
            };
            card.Controls.Add(flowButtons);
            card.Resize += (s, e) => flowButtons.Location = new Point(card.Width - flowButtons.PreferredSize.Width - 18, 20);

            var actionList = new (string Label, Action Action)[]
            {
                ("🔍 Zoom In",   () => ChangeZoom(1.25f)),
                ("🔍 Zoom Out",  () => ChangeZoom(0.8f)),
                ("⤢ Fit View",  ResetZoomLevel),
                ("↺ Reset",     ResetAllFilters),
                ("📥 Export",    ExportToPngImage),
                ("⛶ Full Screen", OpenFullScreenModal)
            };

            foreach (var (lbl, act) in actionList)
            {
                var btn = new Button
                {
                    Text = lbl,
                    Font = new Font("Segoe UI Semibold", 8.5f),
                    FlatStyle = FlatStyle.Flat,
                    Height = 34,
                    AutoSize = true,
                    Padding = new Padding(10, 0, 10, 0),
                    BackColor = WhiteBg,
                    ForeColor = TextHeader,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 6, 0)
                };
                btn.FlatAppearance.BorderColor = BorderColor;
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) => act();
                flowButtons.Controls.Add(btn);
            }
        }

        // ── 2. Filter & Legend Toolbar ─────────────────────────────────────────
        private void BuildFilterSection(Panel card)
        {
            // Server Dropdown
            _cmbServer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI Semibold", 9f),
                Size = new Size(135, 28),
                Location = new Point(16, 14),
                FlatStyle = FlatStyle.Flat
            };
            _cmbServer.Items.Add("All Servers");
            _cmbServer.SelectedIndex = 0;
            _cmbServer.SelectedIndexChanged += (s, e) =>
            {
                _serverFilter = _cmbServer.SelectedIndex;
                _canvasPanel?.Invalidate();
                _serverColPanel?.Invalidate();
            };
            card.Controls.Add(_cmbServer);

            // Legend Badges
            var legends = new (string Name, Color Color)[]
            {
                ("Busy Service",   ClrBusy),
                ("Idle",           ClrIdle),
                ("Waiting",        ClrWaiting),
                ("Completed",      ClrCompleted),
                ("Setup / Active", ClrSetup),
                ("Break",          ClrBreak),
                ("Overload",       ClrOverload)
            };

            int lx = 170;
            foreach (var (name, color) in legends)
            {
                var pnlDot = new Panel { Size = new Size(12, 12), Location = new Point(lx, 22), BackColor = color };
                pnlDot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundPath(new Rectangle(0, 0, 11, 11), 3);
                    using var b = new SolidBrush(color);
                    e.Graphics.FillPath(b, path);
                };
                card.Controls.Add(pnlDot);

                var lbl = new Label
                {
                    Text = name,
                    Font = new Font("Segoe UI Semibold", 8.25f),
                    ForeColor = TextMid,
                    AutoSize = true,
                    Location = new Point(lx + 16, 19),
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lbl);
                lx += lbl.PreferredWidth + 24;
            }

            // Right Time Bounds Badge
            _lblTimeRange = new Label
            {
                Text = "📅 00:00:00  →  02:00:00",
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextHeader,
                BackColor = TrackBgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 5, 8, 5),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            card.Controls.Add(_lblTimeRange);

            // Right Search Input
            _txtSearch = new TextBox
            {
                Text = "Search Customer by ID or Name...",
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(200, 26),
                BackColor = WhiteBg
            };
            _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text.StartsWith("Search Customer")) { _txtSearch.Text = ""; _txtSearch.ForeColor = TextHeader; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = "Search Customer by ID or Name..."; _txtSearch.ForeColor = TextMuted; } };
            _txtSearch.TextChanged += (s, e) =>
            {
                _search = _txtSearch.Text.StartsWith("Search Customer") ? "" : _txtSearch.Text.Trim();
                _canvasPanel?.Invalidate();
            };
            card.Controls.Add(_txtSearch);

            card.Resize += (s, e) =>
            {
                _lblTimeRange.Location = new Point(card.Width - _lblTimeRange.Width - 16, 13);
                _txtSearch.Location = new Point(_lblTimeRange.Location.X - _txtSearch.Width - 14, 15);
            };
        }

        // ── 3. Gantt Timeline Section ──────────────────────────────────────────
        private void BuildGanttSection(Panel card)
        {
            var ganttArea = new Panel { Dock = DockStyle.Fill, BackColor = WhiteBg };
            card.Controls.Add(ganttArea);

            // Pinned Left Server Column
            _serverColPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = PinnedColWidth,
                BackColor = WhiteBg
            };
            _serverColPanel.Paint += PaintServerColumn;
            ganttArea.Controls.Add(_serverColPanel);

            // Scrollable Timeline Canvas
            _canvasPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WhiteBg,
                AutoScroll = true
            };
            _canvasPanel.Paint += PaintTimelineCanvas;
            _canvasPanel.MouseMove += Canvas_MouseMove;
            _canvasPanel.MouseClick += Canvas_Click;
            ganttArea.Controls.Add(_canvasPanel);
            _canvasPanel.BringToFront();
        }

        // ── 4. Customer Details Panel (3 Sub-Cards Layout) ────────────────────
        private void BuildDetailsSection(Panel card)
        {
            // Table layout with 3 equal columns
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(12)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));  // Left profile
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));  // Center metrics + stepper
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));  // Right event feed
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(grid);

            // ── SUB-CARD 1: LEFT CUSTOMER PROFILE ────────────────────────────
            _detLeftPanel = CreateSubCardPanel();
            grid.Controls.Add(_detLeftPanel, 0, 0);

            var lblLHeader = new Label { Text = "👤 CUSTOMER DETAILS", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(14, 12), AutoSize = true };
            _detLeftPanel.Controls.Add(lblLHeader);

            _lblDetTitle = new Label { Text = "Customer 012", Font = new Font("Segoe UI Bold", 13f), ForeColor = TextHeader, Location = new Point(14, 38), AutoSize = true };
            _detLeftPanel.Controls.Add(_lblDetTitle);

            _lblDetBadge = new Label
            {
                Text = "Busy Service",
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = ClrBusy,
                BackColor = Color.FromArgb(239, 246, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(160, 42),
                AutoSize = true
            };
            _detLeftPanel.Controls.Add(_lblDetBadge);

            _lblDetId = new Label { Text = "ID: C012", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextMid, Location = new Point(14, 68), AutoSize = true };
            _detLeftPanel.Controls.Add(_lblDetId);

            // Form Fields
            int fy = 100;
            _detLeftPanel.Controls.Add(new Label { Text = "Server", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _lblDetServerVal = new Label { Text = "Cashier 01", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextHeader, Location = new Point(130, fy) };
            _detLeftPanel.Controls.Add(_lblDetServerVal);

            fy += 26;
            _detLeftPanel.Controls.Add(new Label { Text = "Status", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _lblDetStatusVal = new Label { Text = "In Service", Font = new Font("Segoe UI Semibold", 8.5f), ForeColor = PrimaryBlue, BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(6, 2, 6, 2), Location = new Point(130, fy - 2), AutoSize = true };
            _detLeftPanel.Controls.Add(_lblDetStatusVal);

            fy += 28;
            _detLeftPanel.Controls.Add(new Label { Text = "Color", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _pnlDetColorBox = new Panel { Size = new Size(16, 16), Location = new Point(134, fy + 2), BackColor = ClrBusy };
            _detLeftPanel.Controls.Add(_pnlDetColorBox);

            // ── SUB-CARD 2: CENTER TIMING METRICS & STEPPER ──────────────────
            _detCenterPanel = CreateSubCardPanel();
            grid.Controls.Add(_detCenterPanel, 1, 0);

            var centerGrid = new TableLayoutPanel
            {
                Location = new Point(10, 10),
                Size = new Size(460, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++) centerGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            centerGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            centerGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            _detCenterPanel.Controls.Add(centerGrid);

            _lblArrVal = AddDetailMetricCell(centerGrid, 0, 0, "📅", "Arrival Time", "00:38:20", PrimaryBlue);
            _lblQVal = AddDetailMetricCell(centerGrid, 1, 0, "📥", "Queue Entry", "00:38:45", PrimaryBlue);
            _lblSvcStartVal = AddDetailMetricCell(centerGrid, 2, 0, "🏁", "Service Start", "00:38:45", ClrCompleted);
            _lblDepVal = AddDetailMetricCell(centerGrid, 3, 0, "🚩", "Departure Time", "00:53:20", ClrOverload);

            _lblWaitVal = AddDetailMetricCell(centerGrid, 0, 1, "⌛", "Waiting Time", "00:00:25", ClrWaiting);
            _lblSvcTimeVal = AddDetailMetricCell(centerGrid, 1, 1, "⏱", "Service Time", "00:14:35", PrimaryBlue);
            _lblSysTimeVal = AddDetailMetricCell(centerGrid, 2, 1, "∑", "System Time", "00:15:00", ClrCompleted);
            _lblAssignedServerVal = AddDetailMetricCell(centerGrid, 3, 1, "👤", "Assigned Server", "Cashier 01", TextHeader);

            // Stepper Visual Line
            _stepperPanel = new Panel
            {
                Location = new Point(16, 140),
                Height = 80,
                Width = 440,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            _stepperPanel.Paint += PaintStepperLine;
            _detCenterPanel.Controls.Add(_stepperPanel);

            // ── SUB-CARD 3: RIGHT EVENT TIMELINE FEED ─────────────────────────
            _detRightPanel = CreateSubCardPanel();
            grid.Controls.Add(_detRightPanel, 2, 0);

            var lblRHeader = new Label { Text = "EVENT TIMELINE", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(14, 12), AutoSize = true };
            _detRightPanel.Controls.Add(lblRHeader);

            _eventTimelineList = new Panel
            {
                Location = new Point(10, 36),
                Size = new Size(220, 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.Transparent
            };
            _eventTimelineList.Paint += PaintEventTimelineList;
            _detRightPanel.Controls.Add(_eventTimelineList);
        }

        private Label AddDetailMetricCell(TableLayoutPanel grid, int col, int row, string icon, string title, string val, Color valColor)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 11f), Location = new Point(2, 4), AutoSize = true };
            cell.Controls.Add(lblIcon);

            var lblT = new Label { Text = title, Font = new Font("Segoe UI Semibold", 7.5f), ForeColor = TextMuted, Location = new Point(24, 2), AutoSize = true };
            cell.Controls.Add(lblT);

            var lblV = new Label { Text = val, Font = new Font("Segoe UI Bold", 9.5f), ForeColor = valColor, Location = new Point(24, 18), AutoSize = true };
            cell.Controls.Add(lblV);

            grid.Controls.Add(cell, col, row);
            return lblV;
        }

        // ── 5. KPI Section ────────────────────────────────────────────────────
        private void BuildKPISection(Panel wrapper)
        {
            wrapper.Controls.Clear();
            int count = 6;

            var kpiData = new (string Title, string Val, string Sub, Color Color, string Icon)[]
            {
                ("Simulation Time",     "01:59:41", "Total Duration",       PrimaryBlue, "⏱"),
                ("Total Servers",       "2",        "Active Servers",       ClrSetup,    "🖥"),
                ("Average Utilization", "51.0%",    "Across All Servers",   ClrCompleted,"📊"),
                ("Customers Served",    "27",       "Total Customers",      PrimaryBlue, "👥"),
                ("Total Idle Time",     "01:08:57", "Idle Capacity",        ClrWaiting,  "⏳"),
                ("Average Wait Time",   "00:00:42", "Across All Customers", ClrBreak,    "⏱")
            };

            for (int i = 0; i < count; i++)
            {
                var (title, val, sub, color, icon) = kpiData[i];

                var card = new Panel
                {
                    Size = new Size(160, 130),
                    BackColor = WhiteBg
                };

                int idx = i;
                card.Paint += (s, e) =>
                {
                    var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                    var r = new Rectangle(1, 1, card.Width - 3, card.Height - 3);
                    using var bg = new SolidBrush(WhiteBg);
                    using var path = RoundPath(r, 14);
                    g.FillPath(bg, path);
                    using var pen = new Pen(BorderColor, 1.2f);
                    g.DrawPath(pen, path);

                    // Circle Icon Badge
                    var circleR = new Rectangle(14, 16, 42, 42);
                    using var cBg = new SolidBrush(color);
                    g.FillEllipse(cBg, circleR);

                    using var iconFont = new Font("Segoe UI", 13f);
                    using var whiteB = new SolidBrush(Color.White);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(icon, iconFont, whiteB, circleR, sf);
                };

                // Title
                card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 8f), ForeColor = TextMid, Location = new Point(64, 14), AutoSize = true });

                // Value
                var lblVal = new Label { Text = val, Font = new Font("Segoe UI Bold", 13f), ForeColor = TextHeader, Location = new Point(64, 30), AutoSize = true };
                card.Controls.Add(lblVal);
                _kpiValLabels[i] = lblVal;

                // Subtitle
                card.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 7.5f), ForeColor = TextMuted, Location = new Point(64, 54), AutoSize = true });

                _kpiCards[i] = card;
                wrapper.Controls.Add(card);
            }
        }

        // ── 6. Help Section ───────────────────────────────────────────────────
        private void BuildHelpSection(Panel card)
        {
            var pnlIcon = new Panel { Size = new Size(20, 20), Location = new Point(16, 16), BackColor = Color.Transparent };
            pnlIcon.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(PrimaryBlue);
                e.Graphics.FillEllipse(b, 2, 2, 16, 16);
                using var f = new Font("Segoe UI Bold", 9f);
                using var wb = new SolidBrush(Color.White);
                e.Graphics.DrawString("i", f, wb, new PointF(7, 1));
            };
            card.Controls.Add(pnlIcon);

            var lblTitle = new Label { Text = "How to read this chart", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(42, 14), AutoSize = true };
            card.Controls.Add(lblTitle);

            var lblDesc = new Label
            {
                Text = "Each row represents a server. Colored blocks represent customer service activities. The width of each block indicates the duration of the activity. Click on any block to view detailed customer information.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMid,
                Location = new Point(42, 34),
                AutoSize = true
            };
            card.Controls.Add(lblDesc);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LAYOUT ENGINE
        // ═══════════════════════════════════════════════════════════════════════

        private void RelayoutSections()
        {
            if (_scroll == null) return;
            int availW = Math.Max(700, _scroll.ClientSize.Width - 40);

            _headerCard.Width = availW;
            _filterCard.Width = availW;
            _ganttWrapper.Width = availW;
            _detailsCard.Width = availW;
            _kpiWrapper.Width = availW;
            _helpCard.Width = availW;

            // Relayout KPI Cards horizontally across full width
            int kpiCount = 6;
            int gap = 12;
            int cardW = (availW - (kpiCount - 1) * gap) / kpiCount;
            cardW = Math.Max(130, cardW);

            for (int i = 0; i < kpiCount; i++)
            {
                if (_kpiCards[i] != null)
                {
                    _kpiCards[i].Location = new Point(i * (cardW + gap), 0);
                    _kpiCards[i].Width = cardW;
                }
            }

            int numServers = _result?.NumServers ?? 2;
            _ganttWrapper.Height = Math.Max(320, AxisHeight + numServers * RowHeight + 30);

            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SERVER COLUMN PAINTER (Pinned Left)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintServerColumn(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int panW = _serverColPanel.Width;

            // Axis Top Corner Title "Time"
            using (var fontTime = new Font("Segoe UI Bold", 9f))
            using (var brushTime = new SolidBrush(TextMid))
                g.DrawString("Time", fontTime, brushTime, new PointF(16, 14));

            using (var penLine = new Pen(BorderColor, 1.2f))
                g.DrawLine(penLine, 0, AxisHeight, panW, AxisHeight);

            if (_result == null) return;
            int ns = Math.Max(1, _result.NumServers);

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;

                int rowY = AxisHeight + (s - 1) * RowHeight;

                // Row Separator Line
                using (var penSep = new Pen(BorderColor, 1f))
                    g.DrawLine(penSep, 0, rowY + RowHeight, panW, rowY + RowHeight);

                // Cashier Name
                using var fontName = new Font("Segoe UI Bold", 10f);
                using var brushName = new SolidBrush(TextHeader);
                g.DrawString($"Cashier {s:D2}", fontName, brushName, new PointF(16, rowY + 16));

                // Green Dot Status Badge
                using var dotBrush = new SolidBrush(ClrCompleted);
                g.FillEllipse(dotBrush, 102, rowY + 22, 8, 8);

                // Utilization Label
                using var fontUtilLbl = new Font("Segoe UI", 7.5f);
                using var brushUtilLbl = new SolidBrush(TextMuted);
                g.DrawString("Utilization", fontUtilLbl, brushUtilLbl, new PointF(16, rowY + 38));

                // Utilization % Badge Pill
                double util = (_result.ServerUtilizations != null && _result.ServerUtilizations.Length >= s)
                    ? _result.ServerUtilizations[s - 1] * 100 : 0;

                var badgeR = new Rectangle(16, rowY + 54, 52, 20);
                using (var path = RoundPath(badgeR, 5))
                {
                    using var bBg = new SolidBrush(Color.FromArgb(236, 253, 245));
                    g.FillPath(bBg, path);
                    using var bPen = new Pen(Color.FromArgb(167, 243, 208), 1f);
                    g.DrawPath(bPen, path);
                }

                using var fontBadge = new Font("Segoe UI Bold", 8f);
                using var brushBadge = new SolidBrush(ClrCompleted);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{util:F0}%", fontBadge, brushBadge, badgeR, sf);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MAIN TIMELINE CANVAS PAINTER
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintTimelineCanvas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int cW = _canvasPanel.Width;
            int cH = _canvasPanel.Height;

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                using var ef = new Font("Segoe UI Semibold", 9.5f);
                using var eb = new SolidBrush(TextMuted);
                var esf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("ℹ  Run a simulation to populate the Gantt Timeline.", ef, eb, new Rectangle(0, 0, cW, cH), esf);
                return;
            }

            int ns = Math.Max(1, _result.NumServers);
            int tW = (int)((cW - 40) * _zoomLevel);
            tW = Math.Max(500, tW);

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = Math.Max(1.0, _result.SimulationTime);

            // ── A. Time Axis Header ───────────────────────────────────────────
            using (var axPen = new Pen(BorderColor, 1.2f))
                g.DrawLine(axPen, 0, AxisHeight, tW + 40, AxisHeight);

            int tickCount = 8;
            using var tickFont = new Font("Segoe UI Semibold", 8.5f);
            using var tickBrush = new SolidBrush(TextMid);
            using var gridPen = new Pen(GridPenColor, 1f);

            for (int i = 0; i <= tickCount; i++)
            {
                float tx = 20 + (float)i / tickCount * tW;
                double t = (double)i / tickCount * maxT;
                string timeStr = Customer.FormatTime(t);

                // Grid line
                g.DrawLine(gridPen, tx, AxisHeight, tx, AxisHeight + ns * RowHeight);

                // Tick label
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(timeStr, tickFont, tickBrush, new RectangleF(tx - 45, 14, 90, 20), sf);
            }

            // ── B. Server Rows & Customer Activity Blocks ─────────────────────
            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;

                int rowY = AxisHeight + (s - 1) * RowHeight;
                int taskY = rowY + (RowHeight - TaskHeight) / 2;

                // Row Separator Line
                using (var penSep = new Pen(BorderColor, 1f))
                    g.DrawLine(penSep, 0, rowY + RowHeight, tW + 40, rowY + RowHeight);

                var custs = _result.AllCustomers
                    .Where(c => c.AssignedServer == s && (c.ServiceStartTime > 0 || c.DepartureTime > 0))
                    .OrderBy(c => c.ServiceStartTime)
                    .ToList();

                foreach (var c in custs)
                {
                    double st = c.ServiceStartTime;
                    double et = c.DepartureTime > st ? c.DepartureTime : Math.Min(maxT, st + c.ServiceTime);
                    if (et <= st) continue;

                    float bx = 20 + (float)(st / maxT * tW);
                    float bw = Math.Max(14f, (float)((et - st) / maxT * tW));

                    // Block Color Palette Cycling
                    Color blockColor = CustomerPalette[(c.Id - 1) % CustomerPalette.Length];

                    var blockR = new RectangleF(bx, taskY, bw, TaskHeight);

                    bool isSelected = _selected == c;
                    bool isHovered = _hovered == c;

                    // Fill Block
                    using (var path = RoundPath(Rectangle.Round(blockR), TaskRadius))
                    {
                        using var bBrush = new SolidBrush(blockColor);
                        g.FillPath(bBrush, path);

                        // Highlight border if selected or hovered
                        if (isSelected)
                        {
                            using var selPen = new Pen(Color.FromArgb(250, 176, 5), 2.5f);
                            g.DrawPath(selPen, path);
                        }
                        else if (isHovered)
                        {
                            using var hovPen = new Pen(Color.White, 2f);
                            g.DrawPath(hovPen, path);
                        }
                    }

                    // Multi-Line Text inside Customer Block (matches target screenshot!)
                    using var whiteBrush = new SolidBrush(Color.White);
                    if (bw >= 110)
                    {
                        // Full block text (Title, ID, Time range)
                        using var fontTitle = new Font("Segoe UI Bold", 7.5f);
                        using var fontSub = new Font("Segoe UI Semibold", 6.8f);

                        var sfNear = new StringFormat { Alignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                        g.DrawString($"Customer {c.Id:D3}", fontTitle, whiteBrush, new RectangleF(bx + 6, taskY + 3, bw - 10, 14), sfNear);
                        g.DrawString($"ID: C{c.Id:D3}", fontSub, whiteBrush, new RectangleF(bx + 6, taskY + 16, bw - 10, 12), sfNear);
                        g.DrawString($"{Customer.FormatTime(st)} → {Customer.FormatTime(et)}", fontSub, whiteBrush, new RectangleF(bx + 6, taskY + 28, bw - 10, 12), sfNear);
                    }
                    else if (bw >= 60)
                    {
                        using var fontCompact = new Font("Segoe UI Bold", 7.5f);
                        var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString($"C{c.Id:D3}", fontCompact, whiteBrush, blockR, sfCenter);
                    }
                    else if (bw >= 24)
                    {
                        using var fontTiny = new Font("Segoe UI Bold", 7f);
                        var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString($"{c.Id}", fontTiny, whiteBrush, blockR, sfCenter);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  STEPPER LINE PAINTER (Center Sub-Card)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintStepperLine(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = _stepperPanel.Width;

            var nodes = new (string Name, string Time, Color Clr)[]
            {
                ("Arrival",       _lblArrVal.Text,      ClrWaiting),
                ("Queue Entry",   _lblQVal.Text,        ClrWaiting),
                ("Service Start", _lblSvcStartVal.Text, PrimaryBlue),
                ("Departure",     _lblDepVal.Text,      ClrCompleted)
            };

            int startX = 40;
            int endX = w - 40;
            int step = (endX - startX) / (nodes.Length - 1);
            int lineY = 24;

            // Stepper Base Line
            using (var pLine = new Pen(BorderColor, 2f))
                g.DrawLine(pLine, startX, lineY, endX, lineY);

            for (int i = 0; i < nodes.Length; i++)
            {
                int nx = startX + i * step;
                var (name, time, clr) = nodes[i];

                // Node Circle Dot
                using (var bDot = new SolidBrush(clr))
                    g.FillEllipse(bDot, nx - 6, lineY - 6, 12, 12);
                using (var pDot = new Pen(Color.White, 2f))
                    g.DrawEllipse(pDot, nx - 6, lineY - 6, 12, 12);

                // Labels below node
                using var fontName = new Font("Segoe UI Semibold", 7.5f);
                using var brushName = new SolidBrush(TextMid);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(name, fontName, brushName, new RectangleF(nx - 45, lineY + 10, 90, 14), sf);

                using var fontTime = new Font("Segoe UI", 7f);
                using var brushTime = new SolidBrush(TextMuted);
                g.DrawString(time, fontTime, brushTime, new RectangleF(nx - 45, lineY + 24, 90, 14), sf);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  EVENT TIMELINE FEED PAINTER (Right Sub-Card)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintEventTimelineList(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            var events = new (string Name, string Time, Color Color)[]
            {
                ("Customer Arrived",   _lblArrVal.Text,      ClrWaiting),
                ("Joined Queue",       _lblQVal.Text,        ClrWaiting),
                ("Start Service",      _lblSvcStartVal.Text, PrimaryBlue),
                ("Service Completed",  _lblDepVal.Text,      ClrCompleted),
                ("Customer Departed",  _lblDepVal.Text,      ClrCompleted)
            };

            int ey = 8;
            int lineX = 14;

            for (int i = 0; i < events.Length; i++)
            {
                var (name, time, color) = events[i];

                // Vertical Connector Line
                if (i < events.Length - 1)
                {
                    using var pLine = new Pen(BorderColor, 1.5f);
                    g.DrawLine(pLine, lineX, ey + 6, lineX, ey + 30);
                }

                // Event Bullet Dot
                using (var bDot = new SolidBrush(color))
                    g.FillEllipse(bDot, lineX - 4, ey + 2, 8, 8);

                // Event Title
                using var fontName = new Font("Segoe UI Semibold", 8f);
                using var brushName = new SolidBrush(TextHeader);
                g.DrawString(name, fontName, brushName, new PointF(lineX + 14, ey - 1));

                // Timestamp on Far Right
                using var fontTime = new Font("Segoe UI", 7.5f);
                using var brushTime = new SolidBrush(TextMuted);
                var sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(time, fontTime, brushTime, new RectangleF(_eventTimelineList.Width - 75, ey - 1, 70, 16), sfRight);

                ey += 30;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MOUSE INTERACTION & CLICK HANDLERS
        // ═══════════════════════════════════════════════════════════════════════

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_result == null) return;
            var hit = HitTestCustomerBlock(e.X, e.Y);
            if (hit != _hovered)
            {
                _hovered = hit;
                _canvasPanel.Cursor = hit != null ? Cursors.Hand : Cursors.Default;
                _canvasPanel.Invalidate();
                _tt.Hide(_canvasPanel);

                if (hit != null)
                {
                    string tooltip =
                        $"👤 {hit.Name}  (C{hit.Id:D3})\n" +
                        $"🖥  Cashier {hit.AssignedServer:D2}\n" +
                        $"⏱  Arrival:       {Customer.FormatTime(hit.ArrivalTime)}\n" +
                        $"📥  Queue Entry:   {Customer.FormatTime(hit.QueueEntryTime)}\n" +
                        $"⚡  Service Start: {Customer.FormatTime(hit.ServiceStartTime)}\n" +
                        $"🏁  Departure:     {Customer.FormatTime(hit.DepartureTime)}\n" +
                        $"⏳  Wait Time:     {Customer.FormatDuration(hit.WaitingTime)}\n" +
                        $"💳  Service Time:  {Customer.FormatDuration(hit.ServiceTime)}\n" +
                        $"⏱  System Time:   {Customer.FormatDuration(hit.TimeInSystem)}\n\n" +
                        "👉 Click to populate Details Panel below";
                    _tt.Show(tooltip, _canvasPanel, e.X + 16, e.Y + 16, 5000);
                }
            }
        }

        private void Canvas_Click(object? sender, MouseEventArgs e)
        {
            var hit = HitTestCustomerBlock(e.X, e.Y);
            if (hit == null) return;
            _selected = hit;
            _canvasPanel.Invalidate();
            UpdateDetailsPanel(hit);
        }

        private Customer? HitTestCustomerBlock(int mx, int my)
        {
            if (_result == null) return null;

            int ns = Math.Max(1, _result.NumServers);
            int tW = (int)((_canvasPanel.Width - 40) * _zoomLevel);
            tW = Math.Max(500, tW);

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = 1;

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;
                int rowY = AxisHeight + (s - 1) * RowHeight;
                int taskY = rowY + (RowHeight - TaskHeight) / 2;

                if (my < taskY || my > taskY + TaskHeight) continue;

                double tAtMouse = (mx - 20.0) / tW * maxT;

                var hit = _result.AllCustomers.FirstOrDefault(c =>
                    c.AssignedServer == s &&
                    c.ServiceStartTime <= tAtMouse &&
                    (c.DepartureTime > c.ServiceStartTime ? c.DepartureTime : maxT) >= tAtMouse);

                if (hit != null) return hit;
            }
            return null;
        }

        private void UpdateDetailsPanel(Customer c)
        {
            _lblDetTitle.Text = $"Customer {c.Id:D3}";
            _lblDetId.Text = $"ID: C{c.Id:D3}";
            _lblDetServerVal.Text = $"Cashier {c.AssignedServer:D2}";
            _lblDetStatusVal.Text = c.Status;
            _pnlDetColorBox.BackColor = CustomerPalette[(c.Id - 1) % CustomerPalette.Length];

            _lblArrVal.Text = Customer.FormatTime(c.ArrivalTime);
            _lblQVal.Text = Customer.FormatTime(c.QueueEntryTime);
            _lblSvcStartVal.Text = Customer.FormatTime(c.ServiceStartTime);
            _lblDepVal.Text = Customer.FormatTime(c.DepartureTime);
            _lblWaitVal.Text = Customer.FormatTime(c.WaitingTime);
            _lblSvcTimeVal.Text = Customer.FormatTime(c.ServiceTime);
            _lblSysTimeVal.Text = Customer.FormatTime(c.TimeInSystem);
            _lblAssignedServerVal.Text = $"Cashier {c.AssignedServer:D2}";

            _stepperPanel.Invalidate();
            _eventTimelineList.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  TOOLBAR ACTIONS
        // ═══════════════════════════════════════════════════════════════════════

        private void ChangeZoom(float factor) { _zoomLevel = Math.Max(0.4f, Math.Min(4f, _zoomLevel * factor)); _canvasPanel?.Invalidate(); }
        private void ResetZoomLevel() { _zoomLevel = 1.0f; _canvasPanel?.Invalidate(); }
        private void ResetAllFilters()
        {
            _zoomLevel = 1.0f; _search = ""; _serverFilter = 0; _selected = null; _hovered = null;
            if (_cmbServer != null) _cmbServer.SelectedIndex = 0;
            if (_txtSearch != null) { _txtSearch.Text = "Search Customer by ID or Name..."; _txtSearch.ForeColor = TextMuted; }
            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        private void ExportToPngImage()
        {
            try
            {
                using var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"GanttDashboard_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                using var bmp = new Bitmap(_scroll.Width, _scroll.Height);
                _scroll.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(sfd.FileName, ImageFormat.Png);
                MessageBox.Show("Gantt Monitoring Dashboard exported successfully as high-resolution PNG.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export Failed"); }
        }

        private void OpenFullScreenModal()
        {
            var modal = new Form
            {
                Text = "Server Activity Timeline (Gantt Monitoring Dashboard)",
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = BgColor
            };
            var ctrl = new EnterpriseGanttControl { Dock = DockStyle.Fill };
            if (_result != null) ctrl.LoadResults(_result);
            modal.Controls.Add(ctrl);
            modal.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  PUBLIC API DATA BINDING
        // ═══════════════════════════════════════════════════════════════════════

        public void LoadResults(SimulationResult result)
        {
            _result = result;
            _selected = null;
            _hovered = null;

            if (result != null)
            {
                _lblTimeRange.Text = $"📅 00:00:00  →  {Customer.FormatTime(result.SimulationTime)}";

                // Update 6 Executive KPI Metric Values
                if (_kpiValLabels[0] != null) _kpiValLabels[0].Text = Customer.FormatTime(result.SimulationTime);
                if (_kpiValLabels[1] != null) _kpiValLabels[1].Text = $"{result.NumServers}";
                if (_kpiValLabels[2] != null) _kpiValLabels[2].Text = double.IsNaN(result.SimRho) ? "0.0%" : $"{result.SimRho * 100:F1}%";
                if (_kpiValLabels[3] != null) _kpiValLabels[3].Text = $"{result.CustomersServed}";

                double totalIdle = 0;
                if (result.ServerUtilizations != null)
                    foreach (var u in result.ServerUtilizations)
                        totalIdle += Math.Max(0, (1.0 - u) * result.SimulationTime);

                if (_kpiValLabels[4] != null) _kpiValLabels[4].Text = Customer.FormatTime(totalIdle);
                if (_kpiValLabels[5] != null) _kpiValLabels[5].Text = double.IsNaN(result.SimWq) ? "00:00:00" : Customer.FormatTime(result.SimWq);

                // Populate Dropdown
                _cmbServer.Items.Clear();
                _cmbServer.Items.Add("All Servers");
                for (int s = 1; s <= result.NumServers; s++)
                    _cmbServer.Items.Add($"Cashier {s:D2}");
                _cmbServer.SelectedIndex = 0;
                _serverFilter = 0;

                // Select first customer if available for details panel preview
                if (result.AllCustomers.Count > 0)
                {
                    _selected = result.AllCustomers[0];
                    UpdateDetailsPanel(_selected);
                }
            }

            RelayoutSections();
            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  UI HELPER FACTORIES
        // ═══════════════════════════════════════════════════════════════════════

        private Panel CreateCardPanel(int y, int height)
        {
            var p = new Panel
            {
                Location = new Point(0, y),
                Height = height,
                Width = Math.Max(500, ClientSize.Width - 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = WhiteBg
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(1, 1, p.Width - 3, p.Height - 3);

                // Soft Card Shadow & Surface
                using var bg = new SolidBrush(WhiteBg);
                using var path = RoundPath(r, 14);
                g.FillPath(bg, path);
                using var borderPen = new Pen(BorderColor, 1.2f);
                g.DrawPath(borderPen, path);
            };
            return p;
        }

        private Panel CreateSubCardPanel()
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                BackColor = TrackBgColor
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(1, 1, p.Width - 3, p.Height - 3);
                using var bg = new SolidBrush(TrackBgColor);
                using var path = RoundPath(r, 10);
                g.FillPath(bg, path);
                using var borderPen = new Pen(BorderColor, 1f);
                g.DrawPath(borderPen, path);
            };
            return p;
        }

        private static GraphicsPath RoundPath(Rectangle r, int rad)
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
