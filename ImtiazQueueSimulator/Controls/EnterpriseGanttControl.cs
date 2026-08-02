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
    /// Professional Enterprise Gantt Chart — Continuous Timeline Architecture.
    /// Strictly supports TWO block types:
    ///   1. CUSTOMER (Blue #2563EB)
    ///   2. IDLE     (Light Gray #F1F5F9)
    ///
    /// Fixes Applied:
    ///   - Dynamic text measurement & StringFormatFlags.NoWrap to eliminate ALL text overlapping and line-wrap bugs
    ///   - Server label and green dot positioning dynamically calculated with zero overlap
    ///   - Responsive timeline scale (min 400px per hour) ensuring ample block width and readability
    ///   - Strict width thresholds for 3-line, 2-line, 1-line, and tiny block text rendering
    /// </summary>
    public class EnterpriseGanttControl : UserControl
    {
        // ── State ──────────────────────────────────────────────────────────────
        private SimulationResult? _result;
        private float  _zoomLevel    = 1.0f;
        private string _search       = "";
        private int    _serverFilter = 0;   // 0 = All
        private Customer? _selected  = null;
        private Customer? _hovered   = null;
        private (int Server, double Start, double End)? _hoveredIdle = null;
        private ToolTip _tt = new ToolTip { InitialDelay = 300, ReshowDelay = 100 };

        // ── Layout handles ─────────────────────────────────────────────────────
        private Panel _scroll        = null!;
        private Panel _headerCard    = null!;
        private Panel _filterCard    = null!;
        private Panel _ganttWrapper  = null!;
        private Panel _serverColPanel= null!;
        private Panel _canvasPanel   = null!;
        private Panel _detailsCard   = null!;
        private Panel _kpiWrapper    = null!;
        private Panel _helpCard      = null!;

        // Filter controls
        private ComboBox _cmbServer  = null!;
        private TextBox  _txtSearch  = null!;

        // Details card labels
        private Label _lblDetTitle   = null!;
        private Label _lblDetId      = null!;
        private Label _lblDetServerVal = null!;
        private Label _lblDetStatusVal = null!;
        private Panel _pnlDetColorBox  = null!;
        private Label _lblDetBadge   = null!;

        private Label _lblArrVal = null!, _lblQVal = null!, _lblSvcStartVal = null!, _lblDepVal = null!;
        private Label _lblWaitVal = null!, _lblSvcTimeVal = null!, _lblSysTimeVal = null!, _lblAssignedServerVal = null!;
        private Panel _stepperPanel  = null!;
        private Panel _eventTimelineList = null!;

        // KPI Metric Card Handles
        private Panel[] _kpiCards = new Panel[6];
        private Label[] _kpiValLabels = new Label[6];

        // ── Design Tokens & Strict 2-Color Palette ────────────────────────────
        private static readonly Color BgColor       = Color.FromArgb(246, 248, 252);
        private static readonly Color WhiteBg       = Color.White;
        private static readonly Color TextHeader    = Color.FromArgb(30, 41, 59);      // Slate 800
        private static readonly Color TextMid       = Color.FromArgb(71, 85, 105);      // Slate 600
        private static readonly Color TextMuted     = Color.FromArgb(148, 163, 184);   // Slate 400
        private static readonly Color BorderColor   = Color.FromArgb(226, 232, 240);   // Slate 200
        private static readonly Color GridPenColor  = Color.FromArgb(241, 245, 249);   // Slate 100
        private static readonly Color GridMinorPen  = Color.FromArgb(248, 250, 252);

        // Strict 2 Block Colors
        private static readonly Color ClrCustomer   = Color.FromArgb(37, 99, 235);     // Blue #2563EB
        private static readonly Color ClrIdle       = Color.FromArgb(241, 245, 249);   // Light Gray #F1F5F9
        private static readonly Color ClrIdleBorder = Color.FromArgb(203, 213, 225);   // Border #CBD5E1
        private static readonly Color ClrIdleText   = Color.FromArgb(71, 85, 105);     // Text #475569

        // Geometry Constants
        private const int RowHeight    = 85;
        private const int TaskHeight   = 46;
        private const int TaskRadius   = 8;
        private const int AxisHeight   = 50;
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
            var pnlIcon = new Panel { Size = new Size(38, 38), Location = new Point(18, 18), BackColor = Color.Transparent };
            pnlIcon.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, 36, 36);
                using var b = new SolidBrush(Color.FromArgb(239, 246, 255));
                using var p = RoundPath(r, 8);
                g.FillPath(b, p);
                using var barBrush = new SolidBrush(ClrCustomer);
                g.FillRectangle(barBrush, 8, 20, 5, 10);
                g.FillRectangle(barBrush, 16, 12, 5, 18);
                g.FillRectangle(barBrush, 24, 7, 5, 23);
            };
            card.Controls.Add(pnlIcon);

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
                Text = "Continuous timeline visualization of server utilization and customer checkout activity.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMid,
                AutoSize = true,
                Location = new Point(64, 42),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);

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

        // ── 2. Filter & Legend Toolbar (STRICTLY ONLY CUSTOMER AND IDLE) ──────
        private void BuildFilterSection(Panel card)
        {
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

            var legends = new (string Name, Color Color, Color Border)[]
            {
                ("Customer", ClrCustomer, ClrCustomer),
                ("Idle",     ClrIdle,     ClrIdleBorder)
            };

            int lx = 175;
            foreach (var (name, color, border) in legends)
            {
                var pnlDot = new Panel { Size = new Size(14, 14), Location = new Point(lx, 21), BackColor = color };
                pnlDot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundPath(new Rectangle(0, 0, 13, 13), 3);
                    using var b = new SolidBrush(color);
                    e.Graphics.FillPath(b, path);
                    using var p = new Pen(border, 1.2f);
                    e.Graphics.DrawPath(p, path);
                };
                card.Controls.Add(pnlDot);

                var lbl = new Label
                {
                    Text = name,
                    Font = new Font("Segoe UI Semibold", 9f),
                    ForeColor = TextHeader,
                    AutoSize = true,
                    Location = new Point(lx + 20, 18),
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lbl);
                lx += lbl.PreferredWidth + 36;
            }

            _txtSearch = new TextBox
            {
                Text = "Search Customer by ID or Name...",
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(220, 26),
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
                _txtSearch.Location = new Point(card.Width - _txtSearch.Width - 16, 15);
            };
        }

        // ── 3. Gantt Timeline Section ──────────────────────────────────────────
        private void BuildGanttSection(Panel card)
        {
            var ganttArea = new Panel { Dock = DockStyle.Fill, BackColor = WhiteBg };
            card.Controls.Add(ganttArea);

            _serverColPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = PinnedColWidth,
                BackColor = WhiteBg
            };
            _serverColPanel.Paint += PaintServerColumn;
            ganttArea.Controls.Add(_serverColPanel);

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

        // ── 4. Customer Details Panel Section ──────────────────────────────────
        private void BuildDetailsSection(Panel card)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(12)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(grid);

            // Sub-Card 1: Profile
            var detLeftPanel = CreateSubCardPanel();
            grid.Controls.Add(detLeftPanel, 0, 0);

            detLeftPanel.Controls.Add(new Label { Text = "👤 CUSTOMER DETAILS", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(14, 12), AutoSize = true });
            _lblDetTitle = new Label { Text = "Customer 001", Font = new Font("Segoe UI Bold", 13f), ForeColor = TextHeader, Location = new Point(14, 38), AutoSize = true };
            detLeftPanel.Controls.Add(_lblDetTitle);

            _lblDetBadge = new Label
            {
                Text = "Customer",
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = ClrCustomer,
                BackColor = Color.FromArgb(239, 246, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 2, 6, 2),
                Location = new Point(160, 42),
                AutoSize = true
            };
            detLeftPanel.Controls.Add(_lblDetBadge);

            _lblDetId = new Label { Text = "ID: C001", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextMid, Location = new Point(14, 68), AutoSize = true };
            detLeftPanel.Controls.Add(_lblDetId);

            int fy = 100;
            detLeftPanel.Controls.Add(new Label { Text = "Assigned Server", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _lblDetServerVal = new Label { Text = "Cashier 01", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextHeader, Location = new Point(130, fy) };
            detLeftPanel.Controls.Add(_lblDetServerVal);

            fy += 26;
            detLeftPanel.Controls.Add(new Label { Text = "Status", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _lblDetStatusVal = new Label { Text = "Completed", Font = new Font("Segoe UI Semibold", 8.5f), ForeColor = ClrCustomer, BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(6, 2, 6, 2), Location = new Point(130, fy - 2), AutoSize = true };
            detLeftPanel.Controls.Add(_lblDetStatusVal);

            fy += 28;
            detLeftPanel.Controls.Add(new Label { Text = "Block Color", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, Location = new Point(14, fy) });
            _pnlDetColorBox = new Panel { Size = new Size(16, 16), Location = new Point(134, fy + 2), BackColor = ClrCustomer };
            detLeftPanel.Controls.Add(_pnlDetColorBox);

            // Sub-Card 2: Metrics Grid & Stepper
            var detCenterPanel = CreateSubCardPanel();
            grid.Controls.Add(detCenterPanel, 1, 0);

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
            detCenterPanel.Controls.Add(centerGrid);

            _lblArrVal = AddDetailMetricCell(centerGrid, 0, 0, "📅", "Arrival Time", "00:00:27", ClrCustomer);
            _lblQVal = AddDetailMetricCell(centerGrid, 1, 0, "📥", "Queue Entry", "00:00:27", ClrCustomer);
            _lblSvcStartVal = AddDetailMetricCell(centerGrid, 2, 0, "🏁", "Service Start", "00:00:27", ClrCustomer);
            _lblDepVal = AddDetailMetricCell(centerGrid, 3, 0, "🚩", "Departure", "00:20:42", TextHeader);

            _lblWaitVal = AddDetailMetricCell(centerGrid, 0, 1, "⌛", "Waiting Time", "00:00:00", ClrCustomer);
            _lblSvcTimeVal = AddDetailMetricCell(centerGrid, 1, 1, "⏱", "Service Time", "00:20:15", ClrCustomer);
            _lblSysTimeVal = AddDetailMetricCell(centerGrid, 2, 1, "∑", "System Time", "00:20:15", ClrCustomer);
            _lblAssignedServerVal = AddDetailMetricCell(centerGrid, 3, 1, "👤", "Assigned Server", "Cashier 01", TextHeader);

            _stepperPanel = new Panel
            {
                Location = new Point(16, 140),
                Height = 80,
                Width = 440,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            _stepperPanel.Paint += PaintStepperLine;
            detCenterPanel.Controls.Add(_stepperPanel);

            // Sub-Card 3: Event Feed
            var detRightPanel = CreateSubCardPanel();
            grid.Controls.Add(detRightPanel, 2, 0);

            detRightPanel.Controls.Add(new Label { Text = "EVENT TIMELINE", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(14, 12), AutoSize = true });

            _eventTimelineList = new Panel
            {
                Location = new Point(10, 36),
                Size = new Size(220, 180),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.Transparent
            };
            _eventTimelineList.Paint += PaintEventTimelineList;
            detRightPanel.Controls.Add(_eventTimelineList);
        }

        private Label AddDetailMetricCell(TableLayoutPanel grid, int col, int row, string icon, string title, string val, Color valColor)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            cell.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI", 11f), Location = new Point(2, 4), AutoSize = true });
            cell.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 7.5f), ForeColor = TextMuted, Location = new Point(24, 2), AutoSize = true });
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
                ("Simulation Time",     "01:59:41", "Total Duration",       ClrCustomer, "⏱"),
                ("Total Servers",       "2",        "Active Servers",       ClrCustomer, "🖥"),
                ("Average Utilization", "55.0%",    "Across All Servers",   ClrCustomer, "📊"),
                ("Customers Served",    "27",       "Total Customers",      ClrCustomer, "👥"),
                ("Total Idle Time",     "01:08:57", "Idle Capacity",        ClrIdleText, "⏳"),
                ("Average Wait Time",   "00:00:42", "Across All Customers", ClrCustomer, "⏱")
            };

            for (int i = 0; i < count; i++)
            {
                var (title, val, sub, color, icon) = kpiData[i];

                var card = new Panel { Size = new Size(160, 130), BackColor = WhiteBg };

                card.Paint += (s, e) =>
                {
                    var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                    var r = new Rectangle(1, 1, card.Width - 3, card.Height - 3);
                    using var bg = new SolidBrush(WhiteBg);
                    using var path = RoundPath(r, 14);
                    g.FillPath(bg, path);
                    using var pen = new Pen(BorderColor, 1.2f);
                    g.DrawPath(pen, path);

                    var circleR = new Rectangle(14, 16, 42, 42);
                    using var cBg = new SolidBrush(color);
                    g.FillEllipse(cBg, circleR);

                    using var iconFont = new Font("Segoe UI", 13f);
                    using var whiteB = new SolidBrush(Color.White);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(icon, iconFont, whiteB, circleR, sf);
                };

                card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 8f), ForeColor = TextMid, Location = new Point(64, 14), AutoSize = true });
                var lblVal = new Label { Text = val, Font = new Font("Segoe UI Bold", 13f), ForeColor = TextHeader, Location = new Point(64, 30), AutoSize = true };
                card.Controls.Add(lblVal);
                _kpiValLabels[i] = lblVal;
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
                using var b = new SolidBrush(ClrCustomer);
                e.Graphics.FillEllipse(b, 2, 2, 16, 16);
                using var f = new Font("Segoe UI Bold", 9f);
                using var wb = new SolidBrush(Color.White);
                e.Graphics.DrawString("i", f, wb, new PointF(7, 1));
            };
            card.Controls.Add(pnlIcon);

            card.Controls.Add(new Label { Text = "How to read this timeline", Font = new Font("Segoe UI Bold", 9f), ForeColor = TextHeader, Location = new Point(42, 14), AutoSize = true });
            card.Controls.Add(new Label
            {
                Text = "Each row represents a server track. Blue blocks represent active Customer service. Light Gray blocks represent Idle periods. Blocks connect continuously across the timeline.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TextMid,
                Location = new Point(42, 34),
                AutoSize = true
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LAYOUT ENGINE
        // ═══════════════════════════════════════════════════════════════════════

        private void RelayoutSections()
        {
            if (_scroll == null) return;
            int availW = Math.Max(700, _scroll.ClientSize.Width - 40);

            _headerCard.Width  = availW;
            _filterCard.Width  = availW;
            _ganttWrapper.Width= availW;
            _detailsCard.Width = availW;
            _kpiWrapper.Width  = availW;
            _helpCard.Width    = availW;

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
        //  SERVER COLUMN PAINTER (Pinned Left Column - No Overlapping Dots)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintServerColumn(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int panW = _serverColPanel.Width;

            // Axis Top Corner Title "Server"
            using (var fontTime  = new Font("Segoe UI Bold", 9f))
            using (var brushTime = new SolidBrush(TextMid))
                g.DrawString("Server", fontTime, brushTime, new PointF(16, 16));

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
                string cashierText = $"Cashier {s:D2}";
                using var fontName = new Font("Segoe UI Bold", 10f);
                using var brushName = new SolidBrush(TextHeader);
                g.DrawString(cashierText, fontName, brushName, new PointF(16, rowY + 16));

                // Measure name to place dot dynamically with zero overlap
                SizeF nameSize = g.MeasureString(cashierText, fontName);
                int dotX = (int)(16 + nameSize.Width + 6);

                using var dotBrush = new SolidBrush(ClrCustomer);
                g.FillEllipse(dotBrush, dotX, rowY + 22, 8, 8);

                // Utilization Label
                using var fontUtilLbl = new Font("Segoe UI", 7.5f);
                using var brushUtilLbl = new SolidBrush(TextMuted);
                g.DrawString("Utilization", fontUtilLbl, brushUtilLbl, new PointF(16, rowY + 38));

                // Utilization % Badge Pill
                double util = (_result.ServerUtilizations != null && _result.ServerUtilizations.Length >= s)
                    ? _result.ServerUtilizations[s - 1] * 100 : 0;

                var badgeR = new Rectangle(16, rowY + 54, 56, 20);
                using (var path = RoundPath(badgeR, 5))
                {
                    using var bBg  = new SolidBrush(Color.FromArgb(239, 246, 255));
                    g.FillPath(bBg, path);
                    using var bPen = new Pen(Color.FromArgb(191, 219, 254), 1f);
                    g.DrawPath(bPen, path);
                }

                using var fontBadge  = new Font("Segoe UI Bold", 8f);
                using var brushBadge = new SolidBrush(ClrCustomer);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{util:F0}%", fontBadge, brushBadge, badgeR, sf);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  CONTINUOUS TIMELINE CANVAS PAINTER (STRICT NO-OVERFLOW TYPOGRAPHY)
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

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = Math.Max(1.0, _result.SimulationTime);

            // Responsive Timeline Width (minimum 400px per simulation hour ensures zero text crowding)
            int minPixelsPerHour = 400;
            int desiredWidth = (int)(maxT * minPixelsPerHour * _zoomLevel);
            int tW = Math.Max(cW - 40, desiredWidth);

            // ── A. TIME SCALE AXIS (15m Major Ticks, 5m Minor Ticks) ──────────
            using (var axPen = new Pen(BorderColor, 1.2f))
                g.DrawLine(axPen, 0, AxisHeight, tW + 40, AxisHeight);

            double majorInterval = 0.25;      // 15 mins
            double minorInterval = 0.083333;  // 5 mins

            // Minor Ticks (Grid lines)
            for (double t = 0; t <= maxT + 0.001; t += minorInterval)
            {
                float tx = 20 + (float)(t / maxT * tW);
                using var minorPen = new Pen(GridMinorPen, 1f);
                g.DrawLine(minorPen, tx, AxisHeight, tx, AxisHeight + ns * RowHeight);
            }

            // Major Ticks with labels
            for (double t = 0; t <= maxT + 0.001; t += majorInterval)
            {
                float tx = 20 + (float)(t / maxT * tW);
                string timeStr = Customer.FormatTime(t);

                using var gridPen = new Pen(GridPenColor, 1.2f);
                g.DrawLine(gridPen, tx, AxisHeight - 6, tx, AxisHeight + ns * RowHeight);

                using var tickFont  = new Font("Segoe UI Semibold", 8.5f);
                using var tickBrush = new SolidBrush(TextHeader);
                var sf = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(timeStr, tickFont, tickBrush, new RectangleF(tx - 40, 14, 80, 20), sf);
            }

            // ── B. CONTINUOUS TIMELINE ────────────────────────────────────────
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

                double currentTime = 0;

                for (int i = 0; i < custs.Count; i++)
                {
                    var c = custs[i];
                    double st = c.ServiceStartTime;
                    double et = c.DepartureTime > st ? c.DepartureTime : Math.Min(maxT, st + c.ServiceTime);

                    // 1. Render IDLE block if there is a gap before this customer
                    if (st > currentTime + 0.0001)
                    {
                        RenderIdleBlock(g, s, currentTime, st, maxT, tW, taskY);
                    }

                    // 2. Render CUSTOMER block (Blue)
                    if (et > st)
                    {
                        RenderCustomerBlock(g, c, st, et, maxT, tW, taskY);
                        currentTime = et;
                    }
                }

                // 3. Render final IDLE block if timeline extends beyond last customer
                if (currentTime < maxT - 0.0001)
                {
                    RenderIdleBlock(g, s, currentTime, maxT, maxT, tW, taskY);
                }
            }
        }

        // ── Customer Block Renderer (Blue) ────────────────────────────────────
        private void RenderCustomerBlock(Graphics g, Customer c, double st, double et, double maxT, int tW, int taskY)
        {
            float bx = 20 + (float)(st / maxT * tW);
            float bw = Math.Max(6f, (float)((et - st) / maxT * tW));

            var blockR = new RectangleF(bx, taskY, bw, TaskHeight);
            bool isSelected = _selected == c;
            bool isHovered  = _hovered == c;

            using (var path = RoundPath(Rectangle.Round(blockR), TaskRadius))
            {
                using var bBrush = new SolidBrush(ClrCustomer);
                g.FillPath(bBrush, path);

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

            // Adaptive Typography — Strict No-Wrap to prevent vertical text collisions
            using var whiteBrush = new SolidBrush(Color.White);

            if (bw >= 140)
            {
                // Large: Customer Name, ID, Start -> End (3 clean lines)
                using var fontTitle = new Font("Segoe UI Bold", 7.8f);
                using var fontSub   = new Font("Segoe UI Semibold", 6.8f);

                var sf = new StringFormat { Alignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString($"Customer {c.Id:D3}", fontTitle, whiteBrush, new RectangleF(bx + 6, taskY + 3,  bw - 10, 13), sf);
                g.DrawString($"ID : C{c.Id:D3}",     fontSub,   whiteBrush, new RectangleF(bx + 6, taskY + 16, bw - 10, 12), sf);
                g.DrawString($"{Customer.FormatTime(st)} → {Customer.FormatTime(et)}", fontSub, whiteBrush, new RectangleF(bx + 6, taskY + 28, bw - 10, 12), sf);
            }
            else if (bw >= 90)
            {
                // Medium: Customer Name, ID (2 clean lines)
                using var fontTitle = new Font("Segoe UI Bold", 7.5f);
                using var fontSub   = new Font("Segoe UI Semibold", 6.8f);

                var sf = new StringFormat { Alignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString($"Customer {c.Id:D3}", fontTitle, whiteBrush, new RectangleF(bx + 5, taskY + 6,  bw - 8, 14), sf);
                g.DrawString($"C{c.Id:D3}",          fontSub,   whiteBrush, new RectangleF(bx + 5, taskY + 22, bw - 8, 12), sf);
            }
            else if (bw >= 45)
            {
                // Small: C001 (1 centered line, NO-WRAP prevents C03/6 line break!)
                using var fontCompact = new Font("Segoe UI Bold", 7.5f);
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString($"C{c.Id:D3}", fontCompact, whiteBrush, blockR, sfCenter);
            }
            // Tiny (< 45px): Solid blue block only (no text overflow)
        }

        // ── Idle Block Renderer (Light Gray) ──────────────────────────────────
        private void RenderIdleBlock(Graphics g, int server, double st, double et, double maxT, int tW, int taskY)
        {
            float bx = 20 + (float)(st / maxT * tW);
            float bw = Math.Max(6f, (float)((et - st) / maxT * tW));

            var blockR = new RectangleF(bx, taskY, bw, TaskHeight);
            bool isHovered = _hoveredIdle.HasValue && _hoveredIdle.Value.Server == server &&
                             Math.Abs(_hoveredIdle.Value.Start - st) < 0.001;

            using (var path = RoundPath(Rectangle.Round(blockR), TaskRadius))
            {
                using var bBrush = new SolidBrush(ClrIdle);
                g.FillPath(bBrush, path);

                using var bPen = new Pen(isHovered ? ClrCustomer : ClrIdleBorder, 1f);
                g.DrawPath(bPen, path);
            }

            using var textBrush = new SolidBrush(ClrIdleText);
            if (bw >= 120)
            {
                using var fontTitle = new Font("Segoe UI Bold", 7.5f);
                using var fontSub   = new Font("Segoe UI", 6.8f);

                var sf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString("Idle", fontTitle, textBrush, new RectangleF(bx + 4, taskY + 6,  bw - 8, 14), sf);
                g.DrawString($"{Customer.FormatTime(st)} → {Customer.FormatTime(et)}", fontSub, textBrush, new RectangleF(bx + 4, taskY + 22, bw - 8, 12), sf);
            }
            else if (bw >= 45)
            {
                using var fontTitle = new Font("Segoe UI Semibold", 7.5f);
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString("Idle", fontTitle, textBrush, blockR, sfCenter);
            }
            // Tiny (< 45px): Solid light gray block only
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  STEPPER & EVENT FEED PAINTERS
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintStepperLine(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = _stepperPanel.Width;

            var nodes = new (string Name, string Time, Color Clr)[]
            {
                ("Arrival",       _lblArrVal.Text,      ClrCustomer),
                ("Queue Entry",   _lblQVal.Text,        ClrCustomer),
                ("Service Start", _lblSvcStartVal.Text, ClrCustomer),
                ("Departure",     _lblDepVal.Text,      ClrCustomer)
            };

            int startX = 40;
            int endX = w - 40;
            int step = (endX - startX) / (nodes.Length - 1);
            int lineY = 24;

            using (var pLine = new Pen(BorderColor, 2f))
                g.DrawLine(pLine, startX, lineY, endX, lineY);

            for (int i = 0; i < nodes.Length; i++)
            {
                int nx = startX + i * step;
                var (name, time, clr) = nodes[i];

                using (var bDot = new SolidBrush(clr))
                    g.FillEllipse(bDot, nx - 6, lineY - 6, 12, 12);
                using (var pDot = new Pen(Color.White, 2f))
                    g.DrawEllipse(pDot, nx - 6, lineY - 6, 12, 12);

                using var fontName  = new Font("Segoe UI Semibold", 7.5f);
                using var brushName = new SolidBrush(TextMid);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(name, fontName, brushName, new RectangleF(nx - 45, lineY + 10, 90, 14), sf);

                using var fontTime  = new Font("Segoe UI", 7f);
                using var brushTime = new SolidBrush(TextMuted);
                g.DrawString(time, fontTime, brushTime, new RectangleF(nx - 45, lineY + 24, 90, 14), sf);
            }
        }

        private void PaintEventTimelineList(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;

            var events = new (string Name, string Time, Color Color)[]
            {
                ("Customer Arrived",   _lblArrVal.Text,      ClrCustomer),
                ("Joined Queue",       _lblQVal.Text,        ClrCustomer),
                ("Start Service",      _lblSvcStartVal.Text, ClrCustomer),
                ("Service Completed",  _lblDepVal.Text,      ClrCustomer),
                ("Customer Departed",  _lblDepVal.Text,      ClrCustomer)
            };

            int ey = 8;
            int lineX = 14;

            for (int i = 0; i < events.Length; i++)
            {
                var (name, time, color) = events[i];

                if (i < events.Length - 1)
                {
                    using var pLine = new Pen(BorderColor, 1.5f);
                    g.DrawLine(pLine, lineX, ey + 6, lineX, ey + 30);
                }

                using (var bDot = new SolidBrush(color))
                    g.FillEllipse(bDot, lineX - 4, ey + 2, 8, 8);

                using var fontName  = new Font("Segoe UI Semibold", 8f);
                using var brushName = new SolidBrush(TextHeader);
                g.DrawString(name, fontName, brushName, new PointF(lineX + 14, ey - 1));

                using var fontTime  = new Font("Segoe UI", 7.5f);
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

            var (cust, idle) = HitTestCanvas(e.X, e.Y);

            if (cust != _hovered || idle != _hoveredIdle)
            {
                _hovered = cust;
                _hoveredIdle = idle;
                _canvasPanel.Cursor = (cust != null || idle != null) ? Cursors.Hand : Cursors.Default;
                _canvasPanel.Invalidate();
                _tt.Hide(_canvasPanel);

                if (cust != null)
                {
                    string tooltip =
                        $"👤 {cust.Name}  (C{cust.Id:D3})\n" +
                        $"🖥  Assigned:      Cashier {cust.AssignedServer:D2}\n" +
                        $"⏱  Arrival:       {Customer.FormatTime(cust.ArrivalTime)}\n" +
                        $"📥  Queue Entry:   {Customer.FormatTime(cust.QueueEntryTime)}\n" +
                        $"⚡  Service Start: {Customer.FormatTime(cust.ServiceStartTime)}\n" +
                        $"🏁  Departure:     {Customer.FormatTime(cust.DepartureTime)}\n" +
                        $"⏳  Wait Time:     {Customer.FormatDuration(cust.WaitingTime)}\n" +
                        $"💳  Service Time:  {Customer.FormatDuration(cust.ServiceTime)}\n" +
                        $"⏱  System Time:   {Customer.FormatDuration(cust.TimeInSystem)}\n\n" +
                        "👉 Click to pin details panel";
                    _tt.Show(tooltip, _canvasPanel, e.X + 16, e.Y + 16, 5000);
                }
                else if (idle != null)
                {
                    string tooltip =
                        $"⬜ Cashier {idle.Value.Server:D2} (Idle Period)\n" +
                        $"⏱  Start Time: {Customer.FormatTime(idle.Value.Start)}\n" +
                        $"🏁  End Time:   {Customer.FormatTime(idle.Value.End)}\n" +
                        $"⏳  Duration:   {Customer.FormatDuration(idle.Value.End - idle.Value.Start)}";
                    _tt.Show(tooltip, _canvasPanel, e.X + 16, e.Y + 16, 4000);
                }
            }
        }

        private void Canvas_Click(object? sender, MouseEventArgs e)
        {
            var (cust, _) = HitTestCanvas(e.X, e.Y);
            if (cust == null) return;
            _selected = cust;
            _canvasPanel.Invalidate();
            UpdateDetailsPanel(cust);
        }

        private (Customer? Cust, (int Server, double Start, double End)? Idle) HitTestCanvas(int mx, int my)
        {
            if (_result == null) return (null, null);

            int ns = Math.Max(1, _result.NumServers);

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = 1;

            int minPixelsPerHour = 400;
            int desiredWidth = (int)(maxT * minPixelsPerHour * _zoomLevel);
            int tW = Math.Max(_canvasPanel.Width - 40, desiredWidth);

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;
                int rowY = AxisHeight + (s - 1) * RowHeight;
                int taskY = rowY + (RowHeight - TaskHeight) / 2;

                if (my < taskY || my > taskY + TaskHeight) continue;

                double tAtMouse = (mx - 20.0) / tW * maxT;

                var custs = _result.AllCustomers
                    .Where(c => c.AssignedServer == s && (c.ServiceStartTime > 0 || c.DepartureTime > 0))
                    .OrderBy(c => c.ServiceStartTime)
                    .ToList();

                double currentTime = 0;

                for (int i = 0; i < custs.Count; i++)
                {
                    var c = custs[i];
                    double st = c.ServiceStartTime;
                    double et = c.DepartureTime > st ? c.DepartureTime : Math.Min(maxT, st + c.ServiceTime);

                    if (st > currentTime + 0.0001 && tAtMouse >= currentTime && tAtMouse <= st)
                        return (null, (s, currentTime, st));

                    if (tAtMouse >= st && tAtMouse <= et)
                        return (c, null);

                    currentTime = et;
                }

                if (currentTime < maxT - 0.0001 && tAtMouse >= currentTime && tAtMouse <= maxT)
                    return (null, (s, currentTime, maxT));
            }
            return (null, null);
        }

        private void UpdateDetailsPanel(Customer c)
        {
            _lblDetTitle.Text = $"Customer {c.Id:D3}";
            _lblDetId.Text = $"ID: C{c.Id:D3}";
            _lblDetServerVal.Text = $"Cashier {c.AssignedServer:D2}";
            _lblDetStatusVal.Text = "Completed";

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
        //  TOOLBAR ACTIONS & PUBLIC API
        // ═══════════════════════════════════════════════════════════════════════

        private void ChangeZoom(float factor) { _zoomLevel = Math.Max(0.4f, Math.Min(4f, _zoomLevel * factor)); _canvasPanel?.Invalidate(); }
        private void ResetZoomLevel() { _zoomLevel = 1.0f; _canvasPanel?.Invalidate(); }
        private void ResetAllFilters()
        {
            _zoomLevel = 1.0f; _search = ""; _serverFilter = 0; _selected = null; _hovered = null; _hoveredIdle = null;
            if (_cmbServer != null) _cmbServer.SelectedIndex = 0;
            if (_txtSearch != null) { _txtSearch.Text = "Search Customer by ID or Name..."; _txtSearch.ForeColor = TextMuted; }
            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        private void ExportToPngImage()
        {
            try
            {
                using var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"GanttTimeline_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                using var bmp = new Bitmap(_scroll.Width, _scroll.Height);
                _scroll.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(sfd.FileName, ImageFormat.Png);
                MessageBox.Show("Gantt Monitoring Dashboard exported as PNG.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export Failed"); }
        }

        private void OpenFullScreenModal()
        {
            var modal = new Form
            {
                Text = "Server Activity Timeline (Continuous Gantt Monitoring Dashboard)",
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = BgColor
            };
            var ctrl = new EnterpriseGanttControl { Dock = DockStyle.Fill };
            if (_result != null) ctrl.LoadResults(_result);
            modal.Controls.Add(ctrl);
            modal.ShowDialog();
        }

        public void LoadResults(SimulationResult result)
        {
            _result = result;
            _selected = null;
            _hovered = null;
            _hoveredIdle = null;

            if (result != null)
            {
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

                _cmbServer.Items.Clear();
                _cmbServer.Items.Add("All Servers");
                for (int s = 1; s <= result.NumServers; s++)
                    _cmbServer.Items.Add($"Cashier {s:D2}");
                _cmbServer.SelectedIndex = 0;
                _serverFilter = 0;

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
                BackColor = GridMinorPen
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(1, 1, p.Width - 3, p.Height - 3);
                using var bg = new SolidBrush(GridMinorPen);
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
