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
    /// Premium Enterprise Gantt Monitoring Dashboard.
    /// Designed to match Microsoft Project, Azure DevOps Timeline,
    /// Monday.com, Jira Advanced Roadmaps, and Grafana quality standards.
    ///
    /// Architecture:
    ///  Section 1 — Header Card (title + subtitle + right-side zoom toolbar)
    ///  Section 2 — Filter Bar Card (server filter | legend | search | time range)
    ///  Section 3 — Interactive Gantt Timeline (pinned server column + canvas)
    ///  Section 4 — Selected Customer Details Panel (event timeline + metrics)
    ///  Section 5 — Bottom KPI Cards (8 cards)
    ///  Section 6 — Help Card
    /// </summary>
    public class EnterpriseGanttControl : UserControl
    {
        // ── State ──────────────────────────────────────────────────────────────
        private SimulationResult? _result;
        private float  _zoomLevel  = 1.0f;
        private string _search     = "";
        private int    _serverFilter = 0;   // 0 = All
        private Customer? _selected = null;
        private Customer? _hovered  = null;
        private ToolTip _tt = new ToolTip { InitialDelay = 400, ReshowDelay = 100 };

        // ── Layout handles ─────────────────────────────────────────────────────
        private Panel       _scroll        = null!;  // outer scrollable wrapper
        private Panel       _headerCard    = null!;
        private Panel       _filterCard    = null!;
        private Panel       _ganttWrapper  = null!;  // card that holds the gantt
        private Panel       _serverColPanel= null!;  // pinned left server labels
        private Panel       _canvasPanel   = null!;  // scrollable timeline canvas
        private Panel       _detailsCard   = null!;
        private FlowLayoutPanel _kpiPanel  = null!;
        private Panel       _helpCard      = null!;

        // Filter controls
        private ComboBox    _cmbServer     = null!;
        private TextBox     _txtSearch     = null!;

        // Details card labels
        private Label _detCustName    = null!;
        private Label _detCustId      = null!;
        private Label _detServer      = null!;
        private Label _detArrival     = null!;
        private Label _detQueue       = null!;
        private Label _detServiceStart= null!;
        private Label _detDeparture   = null!;
        private Label _detWait        = null!;
        private Label _detService     = null!;
        private Label _detSystem      = null!;
        private Label _detStatus      = null!;
        private Panel _detEventTimeline= null!;

        // KPI metric cards
        private MetricCard _kSimTime = null!, _kServers = null!, _kUtil = null!, _kServed = null!;
        private MetricCard _kIdle = null!, _kAvgWait = null!, _kAvgSvc = null!, _kPeakQ = null!;

        // ── Design Tokens ─────────────────────────────────────────────────────
        private static readonly Color Bg          = Color.FromArgb(244, 246, 250);
        private static readonly Color White       = Color.White;
        private static readonly Color TextH       = Color.FromArgb(15,  23,  42);   // Slate 900
        private static readonly Color TextM       = Color.FromArgb(51,  65,  85);   // Slate 700
        private static readonly Color TextL       = Color.FromArgb(100, 116, 139);  // Slate 500
        private static readonly Color Border      = Color.FromArgb(226, 232, 240);  // Slate 200
        private static readonly Color TrackBg     = Color.FromArgb(248, 250, 252);  // Slate 50
        private static readonly Color GridLine    = Color.FromArgb(241, 245, 249);  // Slate 100
        private static readonly Color SelGold     = Color.FromArgb(250, 176, 5);    // Gold highlight
        private static readonly Color AccentBlue  = Color.FromArgb(37,  99,  235);

        // Row geometry
        private const int RowH    = 90;   // lane height
        private const int TaskH   = 46;   // bar height
        private const int TaskPad = 10;   // padding inside bar
        private const int TaskR   = 10;   // corner radius
        private const int AxisH   = 50;   // top time axis height
        private const int LeftW   = 200;  // pinned server label column

        // ── 8-color modern palette ─────────────────────────────────────────────
        private static readonly Color[] Palette = new Color[]
        {
            Color.FromArgb(37,  99,  235),  // Blue     #2563EB
            Color.FromArgb(16,  185, 129),  // Green    #10B981
            Color.FromArgb(124, 58,  237),  // Purple   #7C3AED
            Color.FromArgb(249, 115, 22),   // Orange   #F97316
            Color.FromArgb(20,  184, 166),  // Teal     #14B8A6
            Color.FromArgb(236, 72,  153),  // Pink     #EC4899
            Color.FromArgb(6,   182, 212),  // Cyan     #06B6D4
            Color.FromArgb(99,  102, 241),  // Indigo   #6366F1
        };

        public EnterpriseGanttControl()
        {
            BackColor = Bg;
            AutoScroll = true;
            DoubleBuffered = true;
            Build();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  UI BUILDER
        // ═══════════════════════════════════════════════════════════════════════

        private void Build()
        {
            Controls.Clear();

            _scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Bg,
                Padding = new Padding(24, 24, 24, 24)
            };
            Controls.Add(_scroll);

            int y = 0;

            // ── 1. HEADER CARD ────────────────────────────────────────────────
            _headerCard = Card(y, 82);
            BuildHeader(_headerCard);
            _scroll.Controls.Add(_headerCard);
            y += 82 + 20;

            // ── 2. FILTER BAR CARD ────────────────────────────────────────────
            _filterCard = Card(y, 58);
            BuildFilterBar(_filterCard);
            _scroll.Controls.Add(_filterCard);
            y += 58 + 20;

            // ── 3. GANTT TIMELINE CARD ────────────────────────────────────────
            _ganttWrapper = Card(y, 400);
            BuildGanttArea(_ganttWrapper);
            _scroll.Controls.Add(_ganttWrapper);
            y += 400 + 20;

            // ── 4. CUSTOMER DETAILS PANEL ─────────────────────────────────────
            _detailsCard = Card(y, 210);
            BuildDetailsPanel(_detailsCard);
            _scroll.Controls.Add(_detailsCard);
            y += 210 + 20;

            // ── 5. KPI CARDS ─────────────────────────────────────────────────
            _kpiPanel = new FlowLayoutPanel
            {
                Location  = new Point(0, y),
                Height    = 165,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                WrapContents = true,
                Padding   = new Padding(0)
            };
            BuildKPICards(_kpiPanel);
            _scroll.Controls.Add(_kpiPanel);
            y += 165 + 20;

            // ── 6. HELP CARD ──────────────────────────────────────────────────
            _helpCard = Card(y, 110);
            BuildHelpCard(_helpCard);
            _scroll.Controls.Add(_helpCard);
            y += 110 + 30;

            _scroll.Resize += (s, e) => Relayout();
            Relayout();
        }

        // ── Section Builders ──────────────────────────────────────────────────

        private void BuildHeader(Panel card)
        {
            // Left: Title + subtitle
            var lblTitle = new Label
            {
                Text      = "SERVER ACTIVITY TIMELINE — GANTT MONITORING DASHBOARD",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextH,
                AutoSize  = true,
                Location  = new Point(22, 14),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text      = "Visualize server utilization, customer checkout activity and waiting metrics in real time.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextL,
                AutoSize  = true,
                Location  = new Point(22, 44),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);

            // Right: Zoom/action toolbar
            var flow = new FlowLayoutPanel
            {
                Anchor        = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Location      = new Point(card.Width - 570, 22)
            };
            card.Controls.Add(flow);
            card.Resize += (s, e) => flow.Location = new Point(card.Width - flow.PreferredSize.Width - 20, 22);

            foreach (var (txt, act) in new (string, Action)[]
            {
                ("🔍+", () => Zoom(1.25f)),
                ("🔍−", () => Zoom(0.8f)),
                ("⤢ Fit", ResetZoom),
                ("↺ Reset", ResetAll),
                ("📷 PNG", ExportPng),
                ("⛶", FullScreen)
            })
            {
                var t = txt; var a = act;
                var b = new Button
                {
                    Text      = t,
                    Font      = new Font("Segoe UI Semibold", 8.5f),
                    FlatStyle = FlatStyle.Flat,
                    Size      = new Size(t.Length <= 2 ? 38 : 76, 34),
                    BackColor = White,
                    ForeColor = TextM,
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(0, 0, 6, 0)
                };
                b.FlatAppearance.BorderColor = Border;
                b.FlatAppearance.BorderSize  = 1;
                b.Click += (s, e) => a();
                flow.Controls.Add(b);
            }
        }

        private void BuildFilterBar(Panel card)
        {
            // Server filter
            var lblSrv = new Label { Text = "Server:", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextM, AutoSize = true, Location = new Point(18, 18), BackColor = Color.Transparent };
            card.Controls.Add(lblSrv);

            _cmbServer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 8.5f),
                Size          = new Size(130, 26),
                Location      = new Point(70, 14),
                FlatStyle     = FlatStyle.Flat
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

            // Legend badges  (middle area)
            var (busyClr, idleClr, waitClr, compClr, setupClr, brkClr, ovClr) =
                (Palette[0], Border, Palette[4], Palette[1], Palette[6], Palette[4], Color.FromArgb(220,38,38));

            var legends = new (string L, Color C)[]
            {
                ("Busy",  Palette[0]),
                ("Idle",  Color.FromArgb(203,213,225)),
                ("Waiting", Palette[3]),
                ("Done", Palette[1]),
                ("Setup", Palette[6]),
                ("Break", Palette[4]),
                ("Overload", Color.FromArgb(220,38,38))
            };

            int lx = 220;
            foreach (var (l, c) in legends)
            {
                var dot = new Panel { Size = new Size(11, 11), Location = new Point(lx, 22), BackColor = c };
                dot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundRect(new Rectangle(0, 0, 10, 10), 3);
                    using var b = new SolidBrush(c);
                    e.Graphics.FillPath(b, path);
                };
                card.Controls.Add(dot);

                var lbl = new Label { Text = l, Font = new Font("Segoe UI Semibold", 8f), ForeColor = TextM, AutoSize = true, Location = new Point(lx + 14, 19), BackColor = Color.Transparent };
                card.Controls.Add(lbl);
                lx += lbl.PreferredWidth + 30;
            }

            // Search
            var lblSearch = new Label { Text = "Search:", Font = new Font("Segoe UI Semibold", 9f), ForeColor = TextM, AutoSize = true, BackColor = Color.Transparent };
            lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            card.Controls.Add(lblSearch);

            _txtSearch = new TextBox
            {
                Text          = "Customer ID…",
                ForeColor     = TextL,
                Font          = new Font("Segoe UI", 8.5f),
                BorderStyle   = BorderStyle.FixedSingle,
                Size          = new Size(145, 24),
                BackColor     = TrackBg
            };
            _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _txtSearch.GotFocus  += (s, e) => { if (_txtSearch.Text == "Customer ID…") { _txtSearch.Text = ""; _txtSearch.ForeColor = TextH; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = "Customer ID…"; _txtSearch.ForeColor = TextL; } };
            _txtSearch.TextChanged += (s, e) =>
            {
                _search = _txtSearch.Text == "Customer ID…" ? "" : _txtSearch.Text.Trim();
                _canvasPanel?.Invalidate();
            };
            card.Controls.Add(_txtSearch);

            card.Resize += (s, e) =>
            {
                _txtSearch.Location  = new Point(card.Width - 158, 15);
                lblSearch.Location   = new Point(card.Width - 212, 19);
            };
        }

        private void BuildGanttArea(Panel card)
        {
            // Title strip inside card
            var strip = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = TrackBg };
            strip.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1f);
                e.Graphics.DrawLine(pen, 0, strip.Height - 1, strip.Width, strip.Height - 1);
            };
            var lbl = new Label { Text = "📅  Interactive Server Activity Gantt Timeline", Font = new Font("Segoe UI Semibold", 10f), ForeColor = TextH, AutoSize = true, Location = new Point(16, 9), BackColor = Color.Transparent };
            strip.Controls.Add(lbl);
            card.Controls.Add(strip);

            // Main gantt area: pinned server col + scrollable canvas
            var ganttArea = new Panel { Dock = DockStyle.Fill, BackColor = White };
            card.Controls.Add(ganttArea);
            ganttArea.BringToFront();

            // Left: pinned server column
            _serverColPanel = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = LeftW,
                BackColor = TrackBg
            };
            _serverColPanel.Paint += PaintServerColumn;
            ganttArea.Controls.Add(_serverColPanel);

            // Right: scrollable canvas
            _canvasPanel = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = White,
                AutoScroll = true
            };
            _canvasPanel.Paint      += PaintCanvas;
            _canvasPanel.MouseMove  += Canvas_MouseMove;
            _canvasPanel.MouseClick += Canvas_Click;
            ganttArea.Controls.Add(_canvasPanel);
            _canvasPanel.BringToFront();
        }

        private void BuildDetailsPanel(Panel card)
        {
            var strip = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = TrackBg };
            strip.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1f);
                e.Graphics.DrawLine(pen, 0, strip.Height - 1, strip.Width, strip.Height - 1);
            };
            var lbl = new Label { Text = "🔍  Selected Customer Details", Font = new Font("Segoe UI Semibold", 10f), ForeColor = TextH, AutoSize = true, Location = new Point(16, 9), BackColor = Color.Transparent };
            strip.Controls.Add(lbl);
            card.Controls.Add(strip);

            // placeholder when nothing selected
            var ph = new Label
            {
                Name      = "ph",
                Text      = "ℹ  Click any customer block on the timeline to view complete journey details here.",
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = TextL,
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            card.Controls.Add(ph);
            ph.BringToFront();

            // Details grid (hidden until selection)
            var grid = new TableLayoutPanel
            {
                Name        = "detGrid",
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 5,
                BackColor   = Color.Transparent,
                Visible     = false,
                Padding     = new Padding(16, 12, 16, 12)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            card.Controls.Add(grid);

            // Helper to add a detail row cell
            Label DLbl(string title, string val = "--")
            {
                var cell = new Panel { BackColor = Color.Transparent, Margin = new Padding(4, 2, 4, 2) };
                cell.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 7.5f), ForeColor = TextL, AutoSize = true, Location = new Point(0, 0) });
                var v = new Label { Text = val, Font = new Font("Segoe UI Bold", 9.5f), ForeColor = TextH, AutoSize = true, Location = new Point(0, 16) };
                cell.Controls.Add(v);
                grid.Controls.Add(cell);
                return v;
            }

            _detCustName     = DLbl("CUSTOMER NAME");
            _detCustId       = DLbl("CUSTOMER ID");
            _detServer       = DLbl("SERVER");
            _detArrival      = DLbl("ARRIVAL TIME");
            _detQueue        = DLbl("QUEUE ENTRY");
            _detServiceStart = DLbl("SERVICE START");
            _detDeparture    = DLbl("DEPARTURE");
            _detWait         = DLbl("WAITING TIME");
            _detService      = DLbl("SERVICE TIME");
            _detSystem       = DLbl("SYSTEM TIME");
            _detStatus       = DLbl("STATUS");

            // Event timeline mini-bar (row 4, span all cols)
            _detEventTimeline = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin    = new Padding(4, 4, 4, 4)
            };
            _detEventTimeline.Paint += PaintEventTimeline;
            grid.Controls.Add(_detEventTimeline);
            grid.SetColumnSpan(_detEventTimeline, 3);
        }

        private void BuildKPICards(FlowLayoutPanel panel)
        {
            Color c1 = AccentBlue, c2 = Palette[1], c3 = Palette[2], c4 = Palette[3];
            _kSimTime = MC("SIMULATION TIME",     "--:--:--", "total runtime",         c1);
            _kServers = MC("TOTAL SERVERS",       "--",       "active cashiers",        c3);
            _kUtil    = MC("AVG UTILIZATION",     "--",       "server workload",        c2);
            _kServed  = MC("CUSTOMERS SERVED",    "0",        "completed checkouts",    c2);
            _kIdle    = MC("IDLE CAPACITY",       "--",       "total idle time",        c4);
            _kAvgWait = MC("AVG WAITING (Wq)",    "--",       "minutes in queue",       c4);
            _kAvgSvc  = MC("AVG SERVICE (W)",     "--",       "minutes checkout",       c1);
            _kPeakQ   = MC("PEAK QUEUE (Lq)",     "0",        "max queue length",       Color.FromArgb(220,38,38));

            panel.Controls.AddRange(new Control[] { _kSimTime, _kServers, _kUtil, _kServed, _kIdle, _kAvgWait, _kAvgSvc, _kPeakQ });
        }

        private MetricCard MC(string t, string v, string s, Color a) =>
            new MetricCard { Title = t, Value = v, Subtitle = s, AccentColor = a, Size = new Size(170, 155), Margin = new Padding(0, 0, 12, 0) };

        private void BuildHelpCard(Panel card)
        {
            var strip = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = TrackBg };
            strip.Paint += (s, e) => { using var pen = new Pen(Border, 1f); e.Graphics.DrawLine(pen, 0, strip.Height - 1, strip.Width, strip.Height - 1); };
            strip.Controls.Add(new Label { Text = "💡  How to Read This Dashboard", Font = new Font("Segoe UI Semibold", 10f), ForeColor = TextH, AutoSize = true, Location = new Point(16, 9), BackColor = Color.Transparent });
            card.Controls.Add(strip);

            var lines = new[]
            {
                "• Each horizontal lane (row) represents one checkout server (Cashier 01, 02…). Lane height is 90px for clarity.",
                "• Each colored block is ONE customer. Its width equals the service duration. Wider block = longer service.",
                "• Gray background strips indicate idle periods where the server is waiting for customers.",
                "• Hover any block for full journey details. Click any block to populate the Customer Details panel below the chart."
            };

            int ly = 46;
            foreach (var line in lines)
            {
                card.Controls.Add(new Label
                {
                    Text      = line,
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = TextM,
                    AutoSize  = false,
                    Height    = 16,
                    Width     = card.Width - 40,
                    Location  = new Point(22, ly),
                    BackColor = Color.Transparent
                });
                ly += 17;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LAYOUT ENGINE
        // ═══════════════════════════════════════════════════════════════════════

        private void Relayout()
        {
            if (_scroll == null) return;
            int aw = Math.Max(600, _scroll.ClientSize.Width - 48);

            _headerCard.Width  = aw;
            _filterCard.Width  = aw;
            _ganttWrapper.Width = aw;
            _detailsCard.Width = aw;
            _kpiPanel.Width    = aw;
            _helpCard.Width    = aw;

            // Resize help card inline labels
            foreach (Control c in _helpCard.Controls)
                if (c is Label l && l.Location.X == 22) l.Width = aw - 40;

            // KPI card widths
            int ns = _result?.NumServers ?? 1;
            int ganttH = Math.Max(380, AxisH + 24 + ns * (RowH + 16) + 24);
            _ganttWrapper.Height = ganttH;

            int cnt = _kpiPanel.Controls.Count;
            if (cnt > 0)
            {
                int perRow = Math.Max(1, Math.Min(cnt, aw / 172));
                int w = Math.Max(150, (aw - (perRow - 1) * 12) / perRow);
                foreach (Control c in _kpiPanel.Controls) if (c is MetricCard mc) mc.Width = w;
            }

            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SERVER COLUMN PAINTER (pinned left)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintServerColumn(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int panW = _serverColPanel.Width;
            int panH = _serverColPanel.Height;

            // Axis header placeholder
            using (var hBrush = new SolidBrush(TrackBg))
                g.FillRectangle(hBrush, 0, 0, panW, AxisH);
            using (var pen = new Pen(Border, 1.2f))
                g.DrawLine(pen, 0, AxisH - 1, panW, AxisH - 1);

            if (_result == null) return;
            int ns = Math.Max(1, _result.NumServers);

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;

                int y = AxisH + (s - 1) * (RowH + 16) + 8;
                var cardR = new Rectangle(10, y, panW - 16, RowH);

                // Card bg
                using (var path = RoundRect(cardR, 10))
                {
                    using var bg = new SolidBrush(White);
                    g.FillPath(bg, path);
                    using var pen = new Pen(Border, 1.2f);
                    g.DrawPath(pen, path);
                }

                // Cashier name
                using var fName = new Font("Segoe UI Bold", 10f);
                using var bName = new SolidBrush(TextH);
                g.DrawString($"Cashier {s:D2}", fName, bName, new PointF(cardR.X + 12, cardR.Y + 12));

                // Utilization badge
                double util = (_result.ServerUtilizations != null && _result.ServerUtilizations.Length >= s)
                    ? _result.ServerUtilizations[s - 1] * 100 : 0;

                Color ug = util > 85 ? Color.FromArgb(254, 242, 242)
                         : util > 65 ? Color.FromArgb(255, 251, 235)
                         : Color.FromArgb(240, 253, 244);
                Color uf = util > 85 ? Color.FromArgb(220, 38, 38)
                         : util > 65 ? Color.FromArgb(217, 119, 6)
                         : Color.FromArgb(22, 163, 74);

                var badgeR = new Rectangle(cardR.X + 12, cardR.Y + 40, 130, 22);
                using (var path = RoundRect(badgeR, 6))
                {
                    using var bg = new SolidBrush(ug);
                    g.FillPath(bg, path);
                    using var pen = new Pen(uf, 1f);
                    g.DrawPath(pen, path);
                }

                using var fBadge = new Font("Segoe UI Semibold", 8f);
                using var bBadge = new SolidBrush(uf);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"Utilization {util:F0}%", fBadge, bBadge, badgeR, sf);

                // Indicator dot (green/amber/red)
                using var dotBrush = new SolidBrush(uf);
                g.FillEllipse(dotBrush, cardR.Right - 22, cardR.Y + 12, 10, 10);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MAIN CANVAS PAINTER
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintCanvas(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int cW = _canvasPanel.Width;
            int cH = _canvasPanel.Height;

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                using var ef = new Font("Segoe UI Semibold", 9.5f);
                using var eb = new SolidBrush(TextL);
                var esf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("ℹ  Run a simulation to populate the Gantt Timeline.", ef, eb, new Rectangle(0, 0, cW, cH), esf);
                return;
            }

            int ns     = Math.Max(1, _result.NumServers);
            int tW     = (int)((cW - 20) * _zoomLevel);
            tW = Math.Max(400, tW);

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = Math.Max(1.0, _result.SimulationTime);

            // ── A. Time Axis ──────────────────────────────────────────────────
            using (var axBg = new SolidBrush(TrackBg))
                g.FillRectangle(axBg, 0, 0, tW + 20, AxisH);

            using (var axPen = new Pen(Border, 1.2f))
                g.DrawLine(axPen, 0, AxisH - 1, tW + 20, AxisH - 1);

            // Dynamic tick count based on zoom
            int ticks = Math.Max(4, (int)(10 * _zoomLevel));
            using var tickFont  = new Font("Segoe UI Semibold", 8.5f);
            using var tickBrush = new SolidBrush(TextM);
            using var gridPen   = new Pen(GridLine, 1f) { DashStyle = DashStyle.Dash };
            int totalRowH = ns * (RowH + 16) + 24;

            for (int i = 0; i <= ticks; i++)
            {
                float tx = (float)i / ticks * tW + 10;
                double t = (double)i / ticks * maxT;
                string ts = Customer.FormatTime(t);

                // grid line
                g.DrawLine(gridPen, tx, AxisH, tx, AxisH + totalRowH);

                // tick label (90px wide prevents truncation)
                g.DrawString(ts, tickFont, tickBrush,
                    new RectangleF(tx - 45, 14, 90, 20),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // ── B. Server Lanes & Customer Task Blocks ────────────────────────
            var sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var sfL = new StringFormat { Alignment = StringAlignment.Near,   LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;

                int laneY  = AxisH + (s - 1) * (RowH + 16) + 8;
                int taskY  = laneY + (RowH - TaskH) / 2;

                // Idle background strip
                var trackR = new Rectangle(10, taskY, tW, TaskH);
                using (var path = RoundRect(trackR, 8))
                {
                    using var ib = new SolidBrush(TrackBg);
                    g.FillPath(ib, path);
                    using var ip = new Pen(Border, 1f);
                    g.DrawPath(ip, path);
                }

                // Customer blocks
                var custs = _result.AllCustomers
                    .Where(c => c.AssignedServer == s && (c.ServiceStartTime > 0 || c.DepartureTime > 0))
                    .OrderBy(c => c.ServiceStartTime)
                    .ToList();

                foreach (var c in custs)
                {
                    double st = c.ServiceStartTime;
                    double et = c.DepartureTime > st ? c.DepartureTime : Math.Min(maxT, st + c.ServiceTime);
                    if (et <= st) continue;

                    float bx  = 10 + (float)(st / maxT * tW);
                    float bw  = Math.Max(12f, (float)((et - st) / maxT * tW));

                    // Color from palette (per customer ID, cycles through 8 colors)
                    Color col = Palette[(c.Id - 1) % Palette.Length];

                    var br = new RectangleF(bx, taskY, bw, TaskH);

                    bool isSel  = _selected == c;
                    bool isHov  = _hovered  == c;
                    bool isSrch = !string.IsNullOrEmpty(_search) &&
                        (c.Id.ToString().Contains(_search) ||
                         $"C{c.Id:D3}".IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

                    // Soft glow for selected
                    if (isSel)
                    {
                        var glow = new RectangleF(bx - 4, taskY - 4, bw + 8, TaskH + 8);
                        using var gp = RoundRect(Rectangle.Round(glow), TaskR + 2);
                        using var gb = new SolidBrush(Color.FromArgb(60, SelGold));
                        g.FillPath(gb, gp);
                    }

                    // Block fill
                    using (var path = RoundRect(Rectangle.Round(br), TaskR))
                    {
                        using var fill = new LinearGradientBrush(
                            Rectangle.Round(br),
                            Color.FromArgb(235, col),
                            Color.FromArgb(210, col),
                            90f);
                        g.FillPath(fill, path);

                        // Border
                        Color borderCol = isSel ? SelGold : isSrch ? SelGold : col;
                        float borderW   = (isSel || isSrch) ? 2.5f : 1.2f;
                        using var bp = new Pen(borderCol, borderW);
                        g.DrawPath(bp, path);
                    }

                    // Text — adaptive based on block width
                    using var white = new SolidBrush(Color.White);
                    if (bw >= 190)
                    {
                        // Large: name + ID + time range (two lines)
                        using var fL1 = new Font("Segoe UI Bold",      8.5f);
                        using var fL2 = new Font("Segoe UI Semibold",  7.5f);
                        g.DrawString($"Customer {c.Id:D3}", fL1, white, new RectangleF(bx + 8, taskY + 3,  bw - 16, 18), sfL);
                        g.DrawString($"C{c.Id:D3}  •  {Customer.FormatTime(st)} → {Customer.FormatTime(et)}", fL2, white, new RectangleF(bx + 8, taskY + 22, bw - 16, 16), sfL);
                    }
                    else if (bw >= 100)
                    {
                        // Medium: C001 + time
                        using var fM1 = new Font("Segoe UI Bold",      8f);
                        using var fM2 = new Font("Segoe UI Semibold",  7f);
                        g.DrawString($"C{c.Id:D3}", fM1, white, new RectangleF(bx + 6, taskY + 3,  bw - 12, 18), sfL);
                        g.DrawString($"{Customer.FormatTime(st)} → {Customer.FormatTime(et)}", fM2, white, new RectangleF(bx + 6, taskY + 21, bw - 12, 16), sfL);
                    }
                    else if (bw >= 46)
                    {
                        // Small: just C001 centered
                        using var fS = new Font("Segoe UI Bold", 8f);
                        g.DrawString($"C{c.Id:D3}", fS, white, br, sfC);
                    }
                    else if (bw >= 24)
                    {
                        // Tiny: just ID number
                        using var fT = new Font("Segoe UI Bold", 7f);
                        g.DrawString($"{c.Id}", fT, white, br, sfC);
                    }
                    // else: tiny solid block — no text (spec compliant)
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  EVENT TIMELINE MINI PAINTER (inside details card)
        // ═══════════════════════════════════════════════════════════════════════

        private void PaintEventTimeline(object? sender, PaintEventArgs e)
        {
            if (_selected == null) return;
            var g  = e.Graphics;
            var c  = _selected;
            int w  = _detEventTimeline.Width;
            int h  = _detEventTimeline.Height;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            var events = new (string Label, double Time, Color Clr)[]
            {
                ("Arrival",       c.ArrivalTime,       Palette[1]),
                ("Queue Entry",   c.QueueEntryTime,    Palette[3]),
                ("Service Start", c.ServiceStartTime,  Palette[0]),
                ("Departure",     c.DepartureTime,     Palette[4])
            };

            double t0 = events.Min(x => x.Time);
            double t1 = events.Max(x => x.Time);
            if (t1 <= t0) t1 = t0 + 0.01;

            int step = Math.Max(1, (w - 40) / (events.Length - 1));
            int midY = h / 2 - 4;

            // Connecting line
            int x0 = 20, xN = 20 + step * (events.Length - 1);
            using var lp = new Pen(Border, 2f);
            g.DrawLine(lp, x0, midY, xN, midY);

            for (int i = 0; i < events.Length; i++)
            {
                int ex = 20 + i * step;
                var (label, time, clr) = events[i];

                // dot
                using var db = new SolidBrush(clr);
                g.FillEllipse(db, ex - 7, midY - 7, 14, 14);
                using var dp = new Pen(White, 2f);
                g.DrawEllipse(dp, ex - 7, midY - 7, 14, 14);

                // label above
                using var lf = new Font("Segoe UI Semibold", 7.5f);
                using var lb = new SolidBrush(TextM);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(label, lf, lb, new RectangleF(ex - 45, midY - 30, 90, 16), sf);

                // time below
                using var tf = new Font("Segoe UI", 7f);
                using var tb = new SolidBrush(TextL);
                g.DrawString(Customer.FormatTime(time), tf, tb, new RectangleF(ex - 45, midY + 12, 90, 14), sf);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  MOUSE INTERACTION
        // ═══════════════════════════════════════════════════════════════════════

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_result == null) return;
            var c = HitTest(e.X, e.Y);
            if (c != _hovered)
            {
                _hovered = c;
                _canvasPanel.Cursor = c != null ? Cursors.Hand : Cursors.Default;
                _canvasPanel.Invalidate();
                _tt.Hide(_canvasPanel);

                if (c != null)
                {
                    string tip =
                        $"👤 {c.Name}  (C{c.Id:D3})\n" +
                        $"🖥  Cashier {c.AssignedServer:D2}\n" +
                        $"⏱  Arrival:       {Customer.FormatTime(c.ArrivalTime)}\n" +
                        $"📥  Queue Entry:   {Customer.FormatTime(c.QueueEntryTime)}\n" +
                        $"⚡  Service Start: {Customer.FormatTime(c.ServiceStartTime)}\n" +
                        $"🏁  Departure:     {Customer.FormatTime(c.DepartureTime)}\n" +
                        $"⏳  Wait:          {Customer.FormatDuration(c.WaitingTime)}\n" +
                        $"💳  Service:       {Customer.FormatDuration(c.ServiceTime)}\n" +
                        $"⏱  System Time:   {Customer.FormatDuration(c.TimeInSystem)}\n\n" +
                        "👉 Click to pin details panel";
                    _tt.Show(tip, _canvasPanel, e.X + 16, e.Y + 16, 5000);
                }
            }
        }

        private void Canvas_Click(object? sender, MouseEventArgs e)
        {
            var c = HitTest(e.X, e.Y);
            if (c == null) return;
            _selected = c;
            _canvasPanel.Invalidate();
            PopulateDetails(c);
        }

        private Customer? HitTest(int mx, int my)
        {
            if (_result == null) return null;

            int ns  = Math.Max(1, _result.NumServers);
            int tW  = (int)((_canvasPanel.Width - 20) * _zoomLevel);
            tW = Math.Max(400, tW);

            double maxT = _result.AllCustomers
                .Where(c => c.DepartureTime > 0)
                .Select(c => c.DepartureTime)
                .DefaultIfEmpty(_result.SimulationTime)
                .Max();
            if (maxT <= 0) maxT = 1;

            for (int s = 1; s <= ns; s++)
            {
                if (_serverFilter > 0 && _serverFilter != s) continue;
                int laneY = AxisH + (s - 1) * (RowH + 16) + 8;
                int taskY = laneY + (RowH - TaskH) / 2;

                if (my < taskY || my > taskY + TaskH) continue;

                double tAtMouse = (mx - 10.0) / tW * maxT;

                var hit = _result.AllCustomers.FirstOrDefault(c =>
                    c.AssignedServer == s &&
                    c.ServiceStartTime <= tAtMouse &&
                    (c.DepartureTime > c.ServiceStartTime ? c.DepartureTime : maxT) >= tAtMouse);

                if (hit != null) return hit;
            }
            return null;
        }

        private void PopulateDetails(Customer c)
        {
            // Show grid, hide placeholder
            var ph   = _detailsCard.Controls["ph"];
            var grid = _detailsCard.Controls["detGrid"];
            if (ph != null)   ph.Visible   = false;
            if (grid != null) grid.Visible = true;

            _detCustName.Text     = c.Name;
            _detCustId.Text       = $"C{c.Id:D3}";
            _detServer.Text       = $"Cashier {c.AssignedServer:D2}";
            _detArrival.Text      = Customer.FormatTime(c.ArrivalTime);
            _detQueue.Text        = Customer.FormatTime(c.QueueEntryTime);
            _detServiceStart.Text = Customer.FormatTime(c.ServiceStartTime);
            _detDeparture.Text    = Customer.FormatTime(c.DepartureTime);
            _detWait.Text         = Customer.FormatDuration(c.WaitingTime);
            _detService.Text      = Customer.FormatDuration(c.ServiceTime);
            _detSystem.Text       = Customer.FormatDuration(c.TimeInSystem);
            _detStatus.Text       = c.Status;
            _detEventTimeline.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════════════════════════

        private void Zoom(float f) { _zoomLevel = Math.Max(0.4f, Math.Min(5f, _zoomLevel * f)); _canvasPanel?.Invalidate(); }
        private void ResetZoom() { _zoomLevel = 1f; _canvasPanel?.Invalidate(); }
        private void ResetAll()
        {
            _zoomLevel = 1f; _search = ""; _serverFilter = 0; _selected = null; _hovered = null;
            if (_cmbServer != null) _cmbServer.SelectedIndex = 0;
            if (_txtSearch != null) { _txtSearch.Text = "Customer ID…"; _txtSearch.ForeColor = TextL; }
            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        private void ExportPng()
        {
            try
            {
                using var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"Gantt_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (sfd.ShowDialog() != DialogResult.OK) return;
                using var bmp = new Bitmap(_canvasPanel.Width, _canvasPanel.Height);
                _canvasPanel.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(sfd.FileName, ImageFormat.Png);
                MessageBox.Show("Gantt chart exported as PNG.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export Failed"); }
        }

        private void FullScreen()
        {
            var form = new Form
            {
                Text = "Gantt Monitoring Dashboard — Full Screen",
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Bg
            };
            var ctrl = new EnterpriseGanttControl { Dock = DockStyle.Fill };
            if (_result != null) ctrl.LoadResults(_result);
            form.Controls.Add(ctrl);
            form.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════════════════

        public void LoadResults(SimulationResult result)
        {
            _result   = result;
            _selected = null;
            _hovered  = null;

            if (result != null)
            {
                _kSimTime.Value  = Customer.FormatTime(result.SimulationTime);
                _kServers.Value  = $"{result.NumServers}";
                _kUtil.Value     = double.IsNaN(result.SimRho) ? "--" : $"{result.SimRho * 100:F1}%";
                _kServed.Value   = $"{result.CustomersServed}";
                _kAvgWait.Value  = double.IsNaN(result.SimWq) ? "--" : $"{result.SimWq * 60:F1} m";
                _kAvgSvc.Value   = double.IsNaN(result.SimW)  ? "--" : $"{result.SimW  * 60:F1} m";

                int pq = result.QueueLengthOverTime?.Count > 0
                    ? result.QueueLengthOverTime.Max(x => x.QueueLength) : 0;
                _kPeakQ.Value = $"{pq}";

                double idle = 0;
                if (result.ServerUtilizations != null)
                    foreach (var u in result.ServerUtilizations)
                        idle += Math.Max(0, (1.0 - u) * result.SimulationTime);
                _kIdle.Value = Customer.FormatDuration(idle);

                // Rebuild server filter dropdown
                _cmbServer.Items.Clear();
                _cmbServer.Items.Add("All Servers");
                for (int s = 1; s <= result.NumServers; s++)
                    _cmbServer.Items.Add($"Cashier {s:D2}");
                _cmbServer.SelectedIndex = 0;
                _serverFilter = 0;
            }

            Relayout();
            _canvasPanel?.Invalidate();
            _serverColPanel?.Invalidate();
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        private Panel Card(int y, int h)
        {
            var p = new Panel
            {
                Location  = new Point(0, y),
                Height    = h,
                Width     = Math.Max(400, ClientSize.Width - 48),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = White
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(1, 1, p.Width - 3, p.Height - 3);

                // Shadow
                using var shadow = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
                using var sp = RoundRect(new Rectangle(r.X + 2, r.Y + 3, r.Width, r.Height), 16);
                g.FillPath(shadow, sp);

                // Card
                using var bg = new SolidBrush(White);
                using var cp = RoundRect(r, 16);
                g.FillPath(bg, cp);
                using var bp = new Pen(Border, 1.2f);
                g.DrawPath(bp, cp);
            };
            return p;
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
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
