using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Controls;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Executive Dashboard Panel.
    /// Strict Compact Document Flow (14-16px section spacing):
    ///   1. Dashboard Header (REAL-TIME DASHBOARD)
    ///   2. KPI Cards Panel (Lq, L, Wq, W, ρ, Served) — 155px height cards
    ///   3. Live Status Bar (Clock, Queue, In System, Departed, Arrivals, Progress)
    ///   4. Single CHECKOUT QUEUE VISUALIZATION section (175px height, centered empty state)
    ///   5. Single SERVER STATUS section (FlowLayoutPanel with ServerCards)
    ///   6. Post-Simulation Banner (compact green banner)
    /// </summary>
    public class DashboardPanel : UserControl
    {
        // KPI Cards
        private MetricCard _cardLq     = null!;
        private MetricCard _cardL      = null!;
        private MetricCard _cardWq     = null!;
        private MetricCard _cardW      = null!;
        private MetricCard _cardRho    = null!;
        private MetricCard _cardServed = null!;

        // Layout Containers
        private QueueVisualization  _queueViz         = null!;
        private FlowLayoutPanel     _serverCardsPanel = null!;
        private Panel               _livePanel         = null!;
        private Panel               _postSimPanel      = null!;
        private FlowLayoutPanel     _cardsPanel        = null!;

        // Live counter labels
        private Label _lblQueueLen = null!;
        private Label _lblInSystem = null!;
        private Label _lblDeparted = null!;
        private Label _lblTotal    = null!;
        private Label _lblClock    = null!;
        private Label _lblProgress = null!;
        private ProgressBar _progressBar = null!;

        // Section Headings
        private Label _lblQueueHead  = null!;
        private Label _lblServerHead = null!;

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg       = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg       = Color.White;
        private static readonly Color TextDark     = Color.FromArgb(15, 23, 42);    // Slate 900
        private static readonly Color TextMid      = Color.FromArgb(51, 65, 85);    // Slate 700
        private static readonly Color TextMuted    = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color BorderColor  = Color.FromArgb(226, 232, 240); // Slate 200
        private static readonly Color AccentBlue   = Color.FromArgb(37, 99, 235);
        private static readonly Color AccentRed    = Color.FromArgb(239, 68, 68);
        private static readonly Color WarnAmber    = Color.FromArgb(217, 119, 6);
        private static readonly Color PurpleAccent = Color.FromArgb(124, 58, 237);
        private static readonly Color OrangeAccent = Color.FromArgb(234, 88, 12);
        private static readonly Color GreenAccent  = Color.FromArgb(16, 185, 129);

        private const int CardH   = 155;
        private const int CardGap = 12;
        private const int PagePad = 20;

        public DashboardPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
            Reset();
        }

        private void BuildUI()
        {
            int y = PagePad;

            // ── Header (FlowLayoutPanel ensures zero title/subtitle vertical overlap) ──
            var headerFlow = new FlowLayoutPanel
            {
                Location      = new Point(PagePad, y),
                Size          = new Size(1000, 58),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                AutoSize      = true,
                Margin        = new Padding(0)
            };
            Controls.Add(headerFlow);

            var headingLabel = new Label
            {
                Text      = "REAL-TIME DASHBOARD",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 4)
            };
            headerFlow.Controls.Add(headingLabel);

            var subHeadLabel = new Label
            {
                Text      = "Live supermarket checkout simulation metrics and server analytics overview",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Margin    = new Padding(0)
            };
            headerFlow.Controls.Add(subHeadLabel);

            y += 62;

            // ── Row 1: KPI Metric Cards Panel ──────────────────────────────────
            _cardsPanel = new FlowLayoutPanel
            {
                Location     = new Point(PagePad, y),
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor    = Color.Transparent,
                Padding      = new Padding(0),
                Margin       = new Padding(0)
            };
            Controls.Add(_cardsPanel);

            _cardLq     = CreateCard("AVERAGE QUEUE (Lq)", "--", "customers in queue",  AccentRed);
            _cardL      = CreateCard("IN SYSTEM (L)",      "--", "customers total",     AccentBlue);
            _cardWq     = CreateCard("WAIT TIME (Wq)",     "--", "minutes avg wait",    WarnAmber);
            _cardW      = CreateCard("SYSTEM TIME (W)",    "--", "minutes in system",   PurpleAccent);
            _cardRho    = CreateCard("UTILIZATION (ρ)",   "--", "server utilization",  OrangeAccent);
            _cardServed = CreateCard("SERVED",             "0",  "customers served",    GreenAccent);

            _cardsPanel.Controls.AddRange(new Control[]
                { _cardLq, _cardL, _cardWq, _cardW, _cardRho, _cardServed });

            y += _cardsPanel.Height + 16;

            // ── Row 2: Live Status Bar ─────────────────────────────────────────
            _livePanel = new Panel
            {
                Location  = new Point(PagePad, y),
                Height    = 52,
                BackColor = CardBg
            };
            _livePanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, _livePanel.Width - 1, _livePanel.Height - 1);
                using var pen = new Pen(BorderColor, 1f);
                DrawRoundedRect(g, pen, r, 10);
            };
            Controls.Add(_livePanel);

            int lx = 18;
            _lblClock    = AddLiveCounter(_livePanel, "⏱ Clock",    "00:00:00", ref lx);
            _lblQueueLen = AddLiveCounter(_livePanel, "Queue",       "0",        ref lx);
            _lblInSystem = AddLiveCounter(_livePanel, "In System",   "0",        ref lx);
            _lblDeparted = AddLiveCounter(_livePanel, "Departed",    "0",        ref lx);
            _lblTotal    = AddLiveCounter(_livePanel, "Arrivals",    "0",        ref lx);

            _lblProgress = new Label
            {
                Text      = "Progress",
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Location  = new Point(lx, 10)
            };
            _livePanel.Controls.Add(_lblProgress);
            lx += _lblProgress.PreferredWidth + 6;

            _progressBar = new ProgressBar
            {
                Location = new Point(lx, 16),
                Size     = new Size(130, 16),
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0,
                Style    = ProgressBarStyle.Continuous
            };
            _livePanel.Controls.Add(_progressBar);

            var _lblProgressPct = new Label
            {
                Text      = "0%",
                Font      = new Font("Segoe UI Bold", 9f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(lx + 138, 16)
            };
            _livePanel.Controls.Add(_lblProgressPct);
            _progressBar.Tag = _lblProgressPct;

            y += 62;

            // ── Row 3: Single Queue Visualization Heading & Control ───────────
            _lblQueueHead = AddSectionHeading("CHECKOUT QUEUE VISUALIZATION", PagePad, y);
            y += 28;

            _queueViz = new QueueVisualization
            {
                Location = new Point(PagePad, y),
                Height   = 175
            };
            Controls.Add(_queueViz);
            y += 187;

            // ── Row 4: Single Server Status Heading & Grid ────────────────────
            _lblServerHead = AddSectionHeading("SERVER STATUS", PagePad, y);
            y += 28;

            _serverCardsPanel = new FlowLayoutPanel
            {
                Location     = new Point(PagePad, y),
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor    = Color.Transparent,
                Padding      = new Padding(0),
                Margin       = new Padding(0)
            };
            Controls.Add(_serverCardsPanel);
            y += 150;

            // ── Row 5: Post-Simulation Banner ─────────────────────────────────
            _postSimPanel = new Panel
            {
                Location  = new Point(PagePad, y),
                Height    = 52,
                BackColor = Color.FromArgb(240, 253, 244),
                Visible   = false
            };
            _postSimPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(74, 222, 128), 1.2f);
                e.Graphics.DrawRectangle(pen, 0, 0, _postSimPanel.Width - 1, _postSimPanel.Height - 1);
            };

            var postLabel = new Label
            {
                Text         = "✓ SIMULATION COMPLETED — View Customer Records, Queue History, Analytics & Reports via the Sidebar.",
                Font         = new Font("Segoe UI Semibold", 9.5f),
                ForeColor    = Color.FromArgb(21, 128, 61),
                AutoSize     = false,
                Location     = new Point(16, 13),
                Size         = new Size(950, 26),
                AutoEllipsis = true,
                UseMnemonic  = false,
                Name         = "postLabel"
            };
            _postSimPanel.Controls.Add(postLabel);
            Controls.Add(_postSimPanel);

            // Register handlers for dynamic custom layout recalculation
            Resize += (s, e) => PerformCustomLayout();
            _cardsPanel.SizeChanged += (s, e) => PerformCustomLayout();
            _serverCardsPanel.SizeChanged += (s, e) => PerformCustomLayout();
            
            PerformCustomLayout();
        }

        private void PerformCustomLayout()
        {
            if (_cardsPanel == null || _livePanel == null || _queueViz == null) return;

            int availWidth = Math.Max(400, ClientSize.Width - (PagePad * 2));

            // 1. KPI Cards Panel width & responsive card sizing
            _cardsPanel.Width = availWidth;
            int cardsPerRow   = availWidth >= 1250 ? 6 : (availWidth >= 750 ? 3 : 2);
            int targetCardW   = (availWidth - (cardsPerRow - 1) * CardGap) / cardsPerRow;
            targetCardW       = Math.Max(180, targetCardW);

            foreach (Control c in _cardsPanel.Controls)
            {
                if (c is MetricCard mc) mc.Width = targetCardW;
            }

            // 2. Live Panel width & Y
            _livePanel.Width = availWidth;
            _livePanel.Top   = _cardsPanel.Bottom + 16;

            // 3. Queue Viz & Heading Y
            _lblQueueHead.Top = _livePanel.Bottom + 16;
            _queueViz.Top     = _lblQueueHead.Bottom + 8;
            _queueViz.Width   = availWidth;

            // 4. Server Cards Panel & Heading Y
            _lblServerHead.Top      = _queueViz.Bottom + 18;
            _serverCardsPanel.Top   = _lblServerHead.Bottom + 10;
            _serverCardsPanel.Width = availWidth;

            int numServers = _serverCardsPanel.Controls.Count;
            if (numServers > 0)
            {
                int serversPerRow = Math.Max(1, Math.Min(numServers, availWidth / 260));
                int rawServerW    = (availWidth - (serversPerRow - 1) * 18) / serversPerRow;
                int targetServerW = Math.Max(240, Math.Min(260, rawServerW));

                foreach (Control c in _serverCardsPanel.Controls)
                {
                    if (c is ServerCard sc)
                    {
                        sc.Width  = targetServerW;
                        sc.Height = 155;
                        sc.Margin = new Padding(0, 0, 18, 18);
                    }
                }
            }

            // 5. Post Simulation Panel Y (Clean 18px separation below server cards panel)
            _postSimPanel.Left  = PagePad;
            _postSimPanel.Top   = _serverCardsPanel.Bottom + 18;
            _postSimPanel.Width = availWidth;

            foreach (Control c in _postSimPanel.Controls)
            {
                if (c.Name == "postLabel")
                    c.Size = new Size(Math.Max(200, availWidth - 32), 26);
            }

            AutoScrollMinSize = new Size(0, _postSimPanel.Bottom + 30);
        }

        private MetricCard CreateCard(string title, string value, string subtitle, Color accent)
        {
            return new MetricCard
            {
                Title       = title,
                Value       = value,
                Subtitle    = subtitle,
                AccentColor = accent,
                Size        = new Size(220, CardH),
                Margin      = new Padding(0, 0, CardGap, CardGap)
            };
        }

        private Label AddLiveCounter(Panel parent, string caption, string initVal, ref int x)
        {
            var cap = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Location  = new Point(x, 10)
            };
            parent.Controls.Add(cap);
            x += cap.PreferredWidth + 4;

            var val = new Label
            {
                Text      = initVal,
                Font      = new Font("Segoe UI Bold", 10.5f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(x, 15)
            };
            parent.Controls.Add(val);
            x += Math.Max(val.PreferredWidth + 4, 45) + 18;

            var vsep = new Panel
            {
                Location  = new Point(x - 10, 12),
                Size      = new Size(1, 28),
                BackColor = BorderColor
            };
            parent.Controls.Add(vsep);

            return val;
        }

        private Label AddSectionHeading(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextMid,
                AutoSize  = true,
                Location  = new Point(x, y),
                BackColor = Color.Transparent
            };
            Controls.Add(lbl);
            return lbl;
        }

        private void DrawRoundedRect(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
        }

        // ── Public Update API ──────────────────────────────────────────────────

        public void UpdateLiveStatus(int queueLength, int systemSize, int departed, int totalArrivals,
            int servedCount, double currentTime, double simTime,
            IEnumerable<string> waitingCustomerIds, List<QueueVisualization.ServerInfo> servers)
        {
            _lblQueueLen.Text = queueLength.ToString();
            _lblInSystem.Text = systemSize.ToString();
            _lblDeparted.Text = departed.ToString();
            _lblTotal.Text    = totalArrivals.ToString();
            _cardServed.Value = servedCount.ToString();
            _lblClock.Text    = Customer.FormatTime(currentTime);

            double pct = simTime > 0 ? Math.Min(100.0, (currentTime / simTime) * 100.0) : 0;
            _progressBar.Value = (int)pct;

            if (_progressBar.Tag is Label pctLabel)
                pctLabel.Text = $"{(int)pct}%";

            _queueViz.SetWaitingCustomers(waitingCustomerIds);
            PerformCustomLayout();
        }

        public void UpdateServerCards(List<(string Name, bool Busy, string Customer, double Util)> servers)
        {
            if (_serverCardsPanel.Controls.Count != servers.Count)
            {
                _serverCardsPanel.Controls.Clear();
                foreach (var s in servers)
                {
                    var card = new ServerCard
                    {
                        ServerName   = s.Name,
                        IsBusy       = s.Busy,
                        CustomerName = s.Customer,
                        Utilization  = s.Util,
                        Margin       = new Padding(0, 0, 14, 14)
                    };
                    _serverCardsPanel.Controls.Add(card);
                }
            }
            else
            {
                for (int i = 0; i < servers.Count; i++)
                {
                    var card         = (ServerCard)_serverCardsPanel.Controls[i];
                    card.IsBusy       = servers[i].Busy;
                    card.CustomerName = servers[i].Customer;
                    card.Utilization  = servers[i].Util;
                }
            }
            PerformCustomLayout();
        }

        public void ShowFinalResults(SimulationResult result)
        {
            _cardLq.Value     = double.IsNaN(result.SimLq)  ? "—" : $"{result.SimLq:F2}";
            _cardL.Value      = double.IsNaN(result.SimL)   ? "—" : $"{result.SimL:F2}";
            _cardWq.Value     = double.IsNaN(result.SimWq)  ? "—" : $"{result.SimWq * 60:F1}";
            _cardW.Value      = double.IsNaN(result.SimW)   ? "—" : $"{result.SimW * 60:F1}";
            _cardRho.Value    = double.IsNaN(result.SimRho) ? "—" : $"{result.SimRho * 100:F1}%";
            _cardServed.Value = result.CustomersServed.ToString();
            _postSimPanel.Visible = true;
            PerformCustomLayout();
        }

        public void Reset()
        {
            _cardLq.Value     = "—";
            _cardL.Value      = "—";
            _cardWq.Value     = "—";
            _cardW.Value      = "—";
            _cardRho.Value    = "—";
            _cardServed.Value = "0";

            _lblQueueLen.Text = "0";
            _lblInSystem.Text = "0";
            _lblDeparted.Text = "0";
            _lblTotal.Text    = "0";
            _lblClock.Text    = "00:00:00";
            _progressBar.Value = 0;

            if (_progressBar.Tag is Label pctLabel)
                pctLabel.Text = "0%";

            _queueViz.SetWaitingCustomers(new List<string>());
            _serverCardsPanel.Controls.Clear();
            _postSimPanel.Visible = false;
            PerformCustomLayout();
        }
    }
}
