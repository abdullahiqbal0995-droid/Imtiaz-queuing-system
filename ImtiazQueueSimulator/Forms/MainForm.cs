using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Controls;
using ImtiazQueueSimulator.Models;
using ImtiazQueueSimulator.Simulation;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Executive Application Shell.
    /// Deep Slate Navy Sidebar (#0F172A), 72px clean top header bar,
    /// dynamic status indicator, swappable content area.
    /// </summary>
    public class MainForm : Form
    {
        private Panel _sidebar = null!;
        private Panel _headerPanel = null!;
        private Panel _contentPanel = null!;
        private Label _statusLabel = null!;
        private Label _statusDot = null!;

        // Navigation buttons
        private SidebarButton _btnDashboard = null!;
        private SidebarButton _btnSimulation = null!;
        private SidebarButton _btnCustomers = null!;
        private SidebarButton _btnQueueHistory = null!;
        private SidebarButton _btnAnalytics = null!;
        private SidebarButton _btnComparison = null!;
        private SidebarButton _btnReports = null!;
        private SidebarButton _btnSettings = null!;
        private SidebarButton _btnAbout = null!;

        // Content panels
        private DashboardPanel _dashboardPanel = null!;
        private SimulationPanel _simulationPanel = null!;
        private CustomerRecordsPanel _customerRecordsPanel = null!;
        private QueueHistoryPanel _queueHistoryPanel = null!;
        private AnalyticsPanel _analyticsPanel = null!;
        private ComparisonPanel _comparisonPanel = null!;
        private ReportsPanel _reportsPanel = null!;
        private SettingsPanel _settingsPanel = null!;
        private AboutPanel _aboutPanel = null!;

        // Shared simulation state
        private SimulationEngine _engine = new();
        private SimulationResult? _lastResult;
        private List<SimulationResult> _reportHistory = new();
        private System.Windows.Forms.Timer _simTimer = null!;
        private int _eventsPerTick = 2;
        private string _currentStatus = "System Ready";

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color SidebarBg  = Color.FromArgb(15, 23, 42);    // Slate 900
        private static readonly Color HeaderBg   = Color.White;
        private static readonly Color ContentBg  = Color.FromArgb(244, 246, 250);
        private static readonly Color SidebarSep = Color.FromArgb(30, 41, 59);    // Slate 800
        private static readonly Color AccentRed  = Color.FromArgb(239, 68, 68);   // Red 500
        private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);   // Blue 600
        private static readonly Color GreenDot   = Color.FromArgb(16, 185, 129);  // Emerald 500

        public MainForm()
        {
            InitializeForm();
            BuildSidebar();
            BuildHeader();
            BuildContentArea();
            InitializePanels();
            ShowPanel(_dashboardPanel, _btnDashboard);
        }

        private void InitializeForm()
        {
            Text            = "Imtiaz Queue Analyzer — Supermarket Checkout Simulation";
            Size            = new Size(1400, 900);
            MinimumSize     = new Size(1150, 720);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = ContentBg;
            DoubleBuffered  = true;
            WindowState     = FormWindowState.Maximized;

            _simTimer = new System.Windows.Forms.Timer { Interval = 40 };
            _simTimer.Tick += SimTimer_Tick;
        }

        private void BuildSidebar()
        {
            _sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 240,
                BackColor = SidebarBg
            };
            Controls.Add(_sidebar);

            // Logo Header Area
            var logoPanel = new Panel
            {
                Height    = 85,
                Dock      = DockStyle.Top,
                BackColor = Color.Transparent
            };
            _sidebar.Controls.Add(logoPanel);

            logoPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Cart Circle Icon
                using var circleBrush = new SolidBrush(Color.FromArgb(35, 239, 68, 68));
                g.FillEllipse(circleBrush, 14, 21, 42, 42);

                Font cartFont;
                try   { cartFont = new Font("Segoe UI Emoji", 14f); }
                catch { cartFont = new Font("Segoe UI Symbol", 14f); }

                var sfCenter = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                using (cartFont)
                using (var cb = new SolidBrush(AccentRed))
                    g.DrawString("🛒", cartFont, cb, new RectangleF(14, 21, 42, 42), sfCenter);

                var sfNear = new StringFormat
                {
                    Alignment     = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming      = StringTrimming.EllipsisCharacter
                };

                // Title Text
                using var t1f = new Font("Segoe UI", 13.5f, FontStyle.Bold);
                using var t1b = new SolidBrush(Color.White);
                g.DrawString("IMTIAZ", t1f, t1b, new RectangleF(64, 19, 160, 24), sfNear);

                using var t2f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var t2b = new SolidBrush(Color.FromArgb(203, 213, 225)); // High contrast Slate 300
                g.DrawString("QUEUE ANALYZER", t2f, t2b, new RectangleF(64, 43, 160, 18), sfNear);
            };

            var sep = new Panel
            {
                Height    = 1,
                Dock      = DockStyle.Top,
                BackColor = SidebarSep
            };
            _sidebar.Controls.Add(sep);

            // Nav Panel
            var navPanel = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.Transparent,
                AutoScroll = true,
                Padding    = new Padding(0, 8, 0, 0)
            };
            _sidebar.Controls.Add(navPanel);

            int y = 8;
            _btnDashboard    = AddNavButton(navPanel, "▣",  "Dashboard",        ref y);
            _btnSimulation   = AddNavButton(navPanel, "▶",  "Simulation",       ref y);
            _btnCustomers    = AddNavButton(navPanel, "👥", "Customer Records",  ref y);
            _btnQueueHistory = AddNavButton(navPanel, "📋", "Queue History",    ref y);
            _btnAnalytics    = AddNavButton(navPanel, "📊", "Analytics",        ref y);
            _btnComparison   = AddNavButton(navPanel, "▤",  "Model Comparison", ref y);
            _btnReports      = AddNavButton(navPanel, "📄", "Reports",          ref y);

            y += 12;
            var spacerLine = new Panel
            {
                Location  = new Point(16, y),
                Size      = new Size(navPanel.Width - 32, 1),
                BackColor = SidebarSep,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            navPanel.Controls.Add(spacerLine);
            y += 10;

            _btnSettings = AddNavButton(navPanel, "⚙",  "Settings", ref y);
            _btnAbout    = AddNavButton(navPanel, "ℹ",  "About",    ref y);

            _sidebar.Controls.SetChildIndex(navPanel,  0);
            _sidebar.Controls.SetChildIndex(sep,       1);
            _sidebar.Controls.SetChildIndex(logoPanel, 2);
        }

        private SidebarButton AddNavButton(Panel parent, string icon, string text, ref int y)
        {
            var btn = new SidebarButton
            {
                Icon       = icon,
                ButtonText = text,
                Location   = new Point(0, y),
                Width      = 240
            };
            btn.ButtonClicked += NavButton_Clicked;
            parent.Controls.Add(btn);
            y += 48;
            return btn;
        }

        private void BuildHeader()
        {
            _headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 76,
                BackColor = HeaderBg
            };

            _headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
            };

            var textPanel = new FlowLayoutPanel
            {
                Location      = new Point(24, 10),
                Size          = new Size(800, 58),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                AutoSize      = true
            };
            _headerPanel.Controls.Add(textPanel);

            var titleLabel = new Label
            {
                Text      = "IMTIAZ QUEUE ANALYZER",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 2)
            };
            textPanel.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text      = "Supermarket Checkout Queueing Simulation & Analytics Suite",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Margin    = new Padding(0)
            };
            textPanel.Controls.Add(subtitleLabel);

            _statusDot = new Label
            {
                Text      = "●",
                Font      = new Font("Segoe UI", 13f),
                ForeColor = GreenDot,
                AutoSize  = true
            };
            _statusLabel = new Label
            {
                Text      = "System Ready",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize  = true
            };
            _headerPanel.Controls.Add(_statusDot);
            _headerPanel.Controls.Add(_statusLabel);

            _headerPanel.Resize += (s, e) => PositionStatusLabels();
            _headerPanel.Controls.Add(new Control());

            Controls.Add(_headerPanel);
            PositionStatusLabels();
        }

        private void PositionStatusLabels()
        {
            if (_statusLabel == null || _statusDot == null || _headerPanel == null) return;
            int rightPadding = 32;
            int dotW   = _statusDot.PreferredSize.Width > 0 ? _statusDot.PreferredSize.Width : 16;
            int labelW = _statusLabel.PreferredSize.Width > 0 ? _statusLabel.PreferredSize.Width : 90;
            int totalW = dotW + 6 + labelW;

            int startX = _headerPanel.Width - totalW - rightPadding;
            _statusDot.Location   = new Point(startX, 24);
            _statusLabel.Location = new Point(startX + dotW + 6, 25);
        }

        private void BuildContentArea()
        {
            _contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = ContentBg,
                Padding   = new Padding(0)
            };
            Controls.Add(_contentPanel);
            _contentPanel.BringToFront();
        }

        private void InitializePanels()
        {
            _dashboardPanel       = new DashboardPanel();
            _simulationPanel      = new SimulationPanel();
            _customerRecordsPanel = new CustomerRecordsPanel();
            _queueHistoryPanel    = new QueueHistoryPanel();
            _analyticsPanel       = new AnalyticsPanel();
            _comparisonPanel      = new ComparisonPanel();
            _reportsPanel         = new ReportsPanel();
            _settingsPanel        = new SettingsPanel();
            _aboutPanel           = new AboutPanel();

            _simulationPanel.OnStartSimulation += StartSimulation;
            _simulationPanel.OnStopSimulation  += StopSimulation;
            _simulationPanel.OnResetSimulation += ResetSimulation;
            _simulationPanel.OnSaveReport      += SaveReport;

            _reportsPanel.OnSaveReport += (result) =>
            {
                using var dlg = new SaveFileDialog
                {
                    Filter   = "Text Files|*.txt",
                    FileName = $"Imtiaz_Queue_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Reports.ReportGenerator.SaveToFile(dlg.FileName, result);
                    MessageBox.Show("Report saved successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _settingsPanel.OnPresetSelected += (lambda, mu, n) =>
            {
                _simulationPanel.SetParameters(lambda, mu, n);
                ShowPanel(_simulationPanel, _btnSimulation);
            };
        }

        private void NavButton_Clicked(object? sender, EventArgs e)
        {
            if      (sender == _btnDashboard)  ShowPanel(_dashboardPanel, _btnDashboard);
            else if (sender == _btnSimulation) ShowPanel(_simulationPanel, _btnSimulation);
            else if (sender == _btnCustomers)
            {
                _customerRecordsPanel.LoadCustomers(_engine.AllCustomers.ToList());
                ShowPanel(_customerRecordsPanel, _btnCustomers);
            }
            else if (sender == _btnQueueHistory)
            {
                _queueHistoryPanel.LoadSnapshots(_engine.Snapshots);
                ShowPanel(_queueHistoryPanel, _btnQueueHistory);
            }
            else if (sender == _btnAnalytics)
            {
                if (_lastResult != null) _analyticsPanel.LoadResults(_lastResult);
                ShowPanel(_analyticsPanel, _btnAnalytics);
            }
            else if (sender == _btnComparison)  ShowPanel(_comparisonPanel, _btnComparison);
            else if (sender == _btnReports)
            {
                _reportsPanel.LoadReports(_reportHistory);
                ShowPanel(_reportsPanel, _btnReports);
            }
            else if (sender == _btnSettings) ShowPanel(_settingsPanel, _btnSettings);
            else if (sender == _btnAbout)    ShowPanel(_aboutPanel, _btnAbout);
        }

        private void ShowPanel(UserControl panel, SidebarButton activeBtn)
        {
            _contentPanel.SuspendLayout();
            _contentPanel.Controls.Clear();
            panel.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(panel);
            _contentPanel.ResumeLayout();

            foreach (Control c in _sidebar.Controls)
            {
                if (c is Panel navPanel)
                {
                    foreach (Control btn in navPanel.Controls)
                    {
                        if (btn is SidebarButton sb) sb.IsActive = (sb == activeBtn);
                    }
                }
            }
        }

        private void SetStatus(string text, Color dotColor)
        {
            _currentStatus        = text;
            _statusLabel.Text     = text;
            _statusDot.ForeColor  = dotColor;
            PositionStatusLabels();
        }

        private void StartSimulation(SimulationEngine engine, string speed)
        {
            _engine = engine;
            _dashboardPanel.Reset();
            _engine.Initialize();

            switch (speed)
            {
                case "Slow":
                    _simTimer.Interval = 120;
                    _eventsPerTick = 1;
                    break;
                case "Fast":
                    _simTimer.Interval = 15;
                    _eventsPerTick = 8;
                    break;
                case "Normal":
                default:
                    _simTimer.Interval = 40;
                    _eventsPerTick = 2;
                    break;
            }

            SetStatus("● Simulation Running", Color.FromArgb(217, 119, 6));
            _simTimer.Start();
            ShowPanel(_dashboardPanel, _btnDashboard);
        }

        private void SimTimer_Tick(object? sender, EventArgs e)
        {
            if (!_engine.IsRunning && _engine.IsCompleted)
            {
                _simTimer.Stop();
                FinishSimulation();
                return;
            }

            int processed = _engine.ProcessEvents(_eventsPerTick);
            UpdateDashboardLive();

            if (processed == 0 || _engine.IsCompleted)
            {
                _simTimer.Stop();
                FinishSimulation();
            }
        }

        private void UpdateDashboardLive()
        {
            var serverInfos = new List<QueueVisualization.ServerInfo>();
            var serverCards = new List<(string Name, bool Busy, string Customer, double Util)>();

            foreach (var s in _engine.Servers)
            {
                serverInfos.Add(new QueueVisualization.ServerInfo
                {
                    Name         = s.Name,
                    IsBusy       = !s.IsIdle,
                    CustomerName = s.CurrentCustomer?.Name ?? ""
                });
                serverCards.Add((s.Name, !s.IsIdle,
                    s.CurrentCustomer?.Name ?? "",
                    s.GetUtilization(_engine.CurrentTime)));
            }

            _dashboardPanel.UpdateLiveStatus(
                queueLength:        _engine.CurrentQueueLength,
                systemSize:         _engine.CurrentSystemSize,
                departed:           _engine.TotalDepartures,
                totalArrivals:      _engine.TotalArrivals,
                servedCount:        _engine.CustomersServed,
                currentTime:        _engine.CurrentTime,
                simTime:            _engine.SimulationTime,
                waitingCustomerIds: _engine.GetWaitingCustomerIds(),
                servers:            serverInfos
            );

            _dashboardPanel.UpdateServerCards(serverCards);
        }

        private void FinishSimulation()
        {
            _lastResult            = _engine.GetResults();
            _lastResult.CreatedAt  = DateTime.Now;
            _reportHistory.Add(_lastResult);

            _dashboardPanel.ShowFinalResults(_lastResult);
            SetStatus("✓ Simulation Completed", Color.FromArgb(16, 185, 129));
            _simulationPanel.OnSimulationFinished();

            var res = _lastResult;
            double theoreticalRho = res.ModelName.Contains("/N") ? (res.Lambda / (res.NumServers * res.Mu)) : (res.Lambda / res.Mu);
            bool isUnstable = theoreticalRho >= 1;

            string summaryText =
                (isUnstable ? $"⚠ SIMULATION COMPLETED - UNSTABLE ({res.ModelName})\n" : $"✓ SIMULATION COMPLETED ({res.ModelName})\n") +
                "═══════════════════════════════════════\n\n" +
                $"  Simulation Time:     {res.SimulationTime:F2} hours\n" +
                $"  Customers Generated: {res.TotalCustomers}\n" +
                $"  Customers Served:    {res.CustomersServed}\n" +
                $"  Customers Waited:    {res.CustomersWhoWaited}\n" +
                $"  Max Queue Length:    {res.MaxQueueLength}\n" +
                $"  Effective λ:         {res.EffectiveLambda:F2} cust/hr\n\n" +
                "FINAL SIMULATION METRICS (Time-Average):\n" +
                $"  Queue Length (Lq):   {res.SimLq:F4} customers\n" +
                $"  In System (L):       {res.SimL:F4} customers\n" +
                $"  Waiting Time (Wq):   {res.SimWq * 60:F2} min ({res.SimWq:F4} hr)\n" +
                $"  System Time (W):     {res.SimW * 60:F2} min ({res.SimW:F4} hr)\n" +
                $"  Server Util (ρ):     {res.SimRho * 100:F1}%\n\n";

            if (isUnstable)
            {
                summaryText +=
                    "⚠️ WARNING: Theoretical system stability check indicates UNSTABLE (ρ ≥ 1.0).\n" +
                    "  The queue size and waiting times grow unbounded over time.\n\n";
            }

            if (res.HasAnalyticalResults)
            {
                summaryText +=
                    "ANALYTICAL VS SIMULATION ERROR:\n" +
                    $"  Lq Error: {res.LqError:F1}%\n" +
                    $"  L Error:  {res.LError:F1}%\n" +
                    $"  Wq Error: {res.WqError:F1}%\n" +
                    $"  W Error:  {res.WError:F1}%\n";
            }

            _simulationPanel.ShowFinalResultsText(summaryText);
        }

        private void StopSimulation()
        {
            _simTimer.Stop();
            _engine.Stop();
            FinishSimulation();
        }

        private void ResetSimulation()
        {
            _simTimer.Stop();
            _engine = new SimulationEngine();
            _lastResult = null;
            _dashboardPanel.Reset();
            _simulationPanel.ResetResultsText();
            SetStatus("System Ready", Color.FromArgb(16, 185, 129));
        }

        private void SaveReport()
        {
            if (_lastResult == null)
            {
                MessageBox.Show("Run a simulation first.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Filter   = "Text Files|*.txt",
                FileName = $"Imtiaz_Queue_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Reports.ReportGenerator.SaveToFile(dlg.FileName, _lastResult);
                MessageBox.Show("Report saved successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
