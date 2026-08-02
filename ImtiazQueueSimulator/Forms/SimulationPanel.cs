using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;
using ImtiazQueueSimulator.Simulation;
using ImtiazQueueSimulator.Statistics;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Simulation control page with model selection, input parameters (label-above-input 2-column layout),
    /// dedicated control toolbar ([ ▶ START ] [ ■ STOP ] [ ↻ RESET ]) with status indicator,
    /// and results card. Zero simulation logic changed.
    /// </summary>
    public class SimulationPanel : UserControl
    {
        // Events (unchanged)
        public event Action<SimulationEngine, string>? OnStartSimulation;
        public event Action? OnStopSimulation;
        public event Action? OnResetSimulation;
        public event Action? OnSaveReport;

        // Input controls
        private TextBox _txtLambda = null!;
        private TextBox _txtMu = null!;
        private NumericUpDown _nudServers = null!;
        private TextBox _txtSimTime = null!;
        private ComboBox _cmbSpeed = null!;
        private ComboBox _cmbArrivalDist = null!;
        private ComboBox _cmbServiceDist = null!;
        private TextBox _txtSvcParam1 = null!;
        private TextBox _txtSvcParam2 = null!;
        private TextBox _txtArrParam1 = null!;
        private TextBox _txtArrParam2 = null!;
        private TextBox _txtSeed = null!;
        private Label _lblSvcParam1 = null!;
        private Label _lblSvcParam2 = null!;
        private Label _lblArrParam1 = null!;
        private Label _lblArrParam2 = null!;
        private Label _lblServers = null!;
        private Label _lblModelDesc = null!;
        private Panel _modelCardsPanel = null!;

        // Dedicated Toolbar Controls
        private Panel _controlToolbar = null!;
        private Button _btnStart = null!;
        private Button _btnStop = null!;
        private Button _btnReset = null!;
        private Button _btnTheory = null!;
        private Button _btnSave = null!;
        private Label _lblStatusDot = null!;
        private Label _lblStatusText = null!;

        private string _selectedModel = "M/M/1";

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg      = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg      = Color.White;
        private static readonly Color TextDark    = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid     = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight   = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color Border      = Color.FromArgb(226, 232, 240);
        private static readonly Color InputBg     = Color.FromArgb(248, 250, 252);
        private static readonly Color DisabledBg  = Color.FromArgb(241, 245, 249);
        private static readonly Color AccentBlue  = Color.FromArgb(37, 99, 235);
        private static readonly Color AccentRed   = Color.FromArgb(220, 38, 38);
        private static readonly Color WarnAmber   = Color.FromArgb(217, 119, 6);
        private static readonly Color GreenAccent = Color.FromArgb(22, 163, 74);
        private static readonly Color GrayBtn     = Color.FromArgb(100, 116, 139);

        public SimulationPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            Padding    = new Padding(20);
            BuildUI();
            UpdateVisibility();
        }

        private Panel           _mainContainer    = null!;
        private Panel           _headerPanel      = null!;
        private Panel           _modelCard        = null!;
        private Panel           _paramCard        = null!;
        private TableLayoutPanel _inputGrid       = null!;
        private Panel           _leftColumn       = null!;
        private Panel           _resultCard       = null!;
        private RichTextBox     _rtbResults       = null!;

        private void BuildUI()
        {
            // ── Main Centered Container (Max Width 1400px) ───────────────────
            _mainContainer = new Panel
            {
                Location  = new Point(20, 20),
                Width     = 1360,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_mainContainer);

            int y = 0;

            // ── 1. Page Header (Sticky Style Header) ──────────────────────────
            _headerPanel = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(1360, 75),
                BackColor = Color.Transparent
            };
            _mainContainer.Controls.Add(_headerPanel);

            var title = new Label
            {
                Text        = "SIMULATION CONTROL",
                Font        = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor   = TextDark,
                AutoSize    = true,
                Location    = new Point(0, 0),
                UseMnemonic = false
            };
            _headerPanel.Controls.Add(title);

            var sub = new Label
            {
                Text        = "Select a queueing model, configure parameters, run discrete-event simulation & compare analytical results",
                Font        = new Font("Segoe UI", 9.5f),
                ForeColor   = TextLight,
                AutoSize    = true,
                Location    = new Point(0, 40),
                UseMnemonic = false
            };
            _headerPanel.Controls.Add(sub);
            y += 85;

            // ── 2. Model Selection Card (12-16px Border Radius Card) ──────────
            _modelCard = CreateCard(0, y, 1360, 110);
            _mainContainer.Controls.Add(_modelCard);

            AddCardSectionLabel(_modelCard, "SELECT QUEUEING MODEL", 20, 14);

            _modelCardsPanel = new FlowLayoutPanel
            {
                Location      = new Point(20, 38),
                Size          = new Size(1320, 44),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0)
            };
            _modelCard.Controls.Add(_modelCardsPanel);

            string[] models = { "M/M/1", "M/M/N", "M/G/1", "M/G/N", "G/G/1", "G/G/N" };
            foreach (var m in models)
                _modelCardsPanel.Controls.Add(CreateModelButton(m));

            _lblModelDesc = new Label
            {
                Text        = "M/M/1: Poisson arrivals • Exponential service • 1 server",
                Font        = new Font("Segoe UI Semibold", 8.5f, FontStyle.Italic),
                ForeColor   = AccentBlue,
                AutoSize    = true,
                Location    = new Point(20, 84),
                UseMnemonic = false
            };
            _modelCard.Controls.Add(_lblModelDesc);
            y += 126;

            // ── 3. Two Column Grid (Left: Inputs + Toolbar, Right: Results) ───
            int leftW  = 620;
            int rightW = 720;
            int rightX = leftW + 20;

            // ── Left Column Container ─────────────────────────────────────────
            _leftColumn = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(leftW, 660),
                BackColor = Color.Transparent
            };
            _mainContainer.Controls.Add(_leftColumn);

            int leftY = 0;

            // ── Card 1: Input Parameters Grid Card ────────────────────────────
            _paramCard = CreateCard(0, leftY, leftW, 440);
            _leftColumn.Controls.Add(_paramCard);

            AddCardSectionLabel(_paramCard, "INPUT PARAMETERS", 20, 14);

            // Table layout for 2-column clean layout (Label above Input, 64px row height)
            _inputGrid = new TableLayoutPanel
            {
                Location    = new Point(20, 42),
                Size        = new Size(leftW - 40, 384),
                ColumnCount = 2,
                RowCount    = 6,
                BackColor   = Color.Transparent
            };
            _inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            for (int i = 0; i < 6; i++)
                _inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));

            // Row 0: Lambda & Mu
            _inputGrid.Controls.Add(CreateInputField("Arrival Rate λ", "cust/hr", out _txtLambda, "20"), 0, 0);
            _inputGrid.Controls.Add(CreateInputField("Service Rate μ", "cust/hr", out _txtMu, "12"), 1, 0);

            // Row 1: Servers & Sim Time
            _inputGrid.Controls.Add(CreateNudField("Number of Servers N", out _lblServers, out _nudServers, 3), 0, 1);
            _inputGrid.Controls.Add(CreateInputField("Simulation Time", "hours", out _txtSimTime, "2"), 1, 1);

            // Row 2: Speed & Arrival Dist
            _inputGrid.Controls.Add(CreateComboField("Simulation Speed", out _cmbSpeed, new[] { "Slow", "Normal", "Fast" }, 1), 0, 2);
            _inputGrid.Controls.Add(CreateComboField("Arrival Distribution", out _cmbArrivalDist, new[] { "Exponential", "Uniform", "Normal", "Deterministic" }, 0), 1, 2);

            // Row 3: Service Dist & Seed
            _inputGrid.Controls.Add(CreateComboField("Service Distribution", out _cmbServiceDist, new[] { "Exponential", "Uniform", "Normal", "Deterministic" }, 0), 0, 3);
            _inputGrid.Controls.Add(CreateInputField("Random Seed", "auto/int", out _txtSeed, ""), 1, 3);

            // Row 4: Service Param 1 & 2
            _inputGrid.Controls.Add(CreateLabeledInputField("Service Param 1", out _lblSvcParam1, out _txtSvcParam1), 0, 4);
            _inputGrid.Controls.Add(CreateLabeledInputField("Service Param 2", out _lblSvcParam2, out _txtSvcParam2), 1, 4);

            // Row 5: Arrival Param 1 & 2
            _inputGrid.Controls.Add(CreateLabeledInputField("Arrival Param 1", out _lblArrParam1, out _txtArrParam1), 0, 5);
            _inputGrid.Controls.Add(CreateLabeledInputField("Arrival Param 2", out _lblArrParam2, out _txtArrParam2), 1, 5);

            _paramCard.Controls.Add(_inputGrid);
            leftY += _paramCard.Height + 16;

            // ── Card 2: Simulation Controls Toolbar Card ──────────────────────
            _controlToolbar = CreateCard(0, leftY, leftW, 115);
            _leftColumn.Controls.Add(_controlToolbar);

            AddCardSectionLabel(_controlToolbar, "SIMULATION CONTROLS", 20, 14);

            // Status Indicator (● READY / ● RUNNING / ● COMPLETED)
            _lblStatusDot = new Label
            {
                Text      = "●",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = GreenAccent,
                AutoSize  = true,
                Location  = new Point(leftW - 140, 12)
            };
            _lblStatusText = new Label
            {
                Text      = "READY",
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(leftW - 122, 15)
            };
            _controlToolbar.Controls.Add(_lblStatusDot);
            _controlToolbar.Controls.Add(_lblStatusText);

            // Control Buttons Row (Equal heights 44px, distinct colors, clear gaps)
            _btnStart = new Button
            {
                Text      = "▶  START",
                Size      = new Size(180, 44),
                Location  = new Point(20, 46),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                BackColor = GreenAccent,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += BtnStart_Click;
            _controlToolbar.Controls.Add(_btnStart);

            _btnStop = new Button
            {
                Text      = "■  STOP",
                Size      = new Size(130, 44),
                Location  = new Point(210, 46),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                BackColor = AccentRed,
                ForeColor = Color.White,
                Enabled   = false,
                Cursor    = Cursors.Hand
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += (s, e) => OnStopSimulation?.Invoke();
            _controlToolbar.Controls.Add(_btnStop);

            _btnReset = new Button
            {
                Text      = "↻  RESET",
                Size      = new Size(130, 44),
                Location  = new Point(350, 46),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                BackColor = GrayBtn,
                ForeColor = Color.White,
                Enabled   = false,
                Cursor    = Cursors.Hand
            };
            _btnReset.FlatAppearance.BorderSize = 0;
            _btnReset.Click += (s, e) =>
            {
                OnResetSimulation?.Invoke();
                SetStatus("READY", GreenAccent);
                _btnStop.Enabled  = false;
                _btnStart.Enabled = true;
                _btnReset.Enabled = false;
                ResetResultsText();
            };
            _controlToolbar.Controls.Add(_btnReset);
            leftY += _controlToolbar.Height + 16;

            // Theoretical & Save buttons
            _btnTheory = CreateStyledButton("📐  VIEW THEORETICAL RESULTS", leftY, AccentBlue, Color.White, leftW);
            _btnTheory.Click += BtnTheory_Click;
            _leftColumn.Controls.Add(_btnTheory);
            leftY += 52;

            _btnSave = CreateStyledButton("📄  SAVE TXT REPORT", leftY, AccentBlue, Color.White, leftW);
            _btnSave.Click += (s, e) => OnSaveReport?.Invoke();
            _leftColumn.Controls.Add(_btnSave);

            // ── Right Column: Results & Analytical Comparison Card ─────────────
            _resultCard = CreateCard(rightX, y, rightW, 560);
            _mainContainer.Controls.Add(_resultCard);

            var resultHeader = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(rightW, 46),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            resultHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1f);
                e.Graphics.DrawLine(pen, 0, resultHeader.Height - 1,
                    resultHeader.Width, resultHeader.Height - 1);
                using var f = new Font("Segoe UI Semibold", 10f);
                using var b = new SolidBrush(TextDark);
                e.Graphics.DrawString("📊 RESULTS & ANALYTICAL COMPARISON", f, b, 20, 13);
            };
            _resultCard.Controls.Add(resultHeader);

            _rtbResults = new RichTextBox
            {
                Location    = new Point(20, 58),
                Size        = new Size(rightW - 40, 480),
                BorderStyle = BorderStyle.None,
                BackColor   = Color.White,
                ReadOnly    = true,
                Font        = new Font("Segoe UI", 9.5f),
                ForeColor   = TextDark,
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };
            _resultCard.Controls.Add(_rtbResults);

            Resize += (s, e) => PerformCustomLayout();
            PerformCustomLayout();

            _cmbModel_Changed(null, EventArgs.Empty);
            _cmbServiceDist.SelectedIndexChanged += (s, e) => UpdateDistParams();
            _cmbArrivalDist.SelectedIndexChanged += (s, e) => UpdateDistParams();
        }

        private void PerformCustomLayout()
        {
            if (_mainContainer == null || _headerPanel == null || _modelCard == null ||
                _leftColumn == null || _paramCard == null || _controlToolbar == null ||
                _resultCard == null || _inputGrid == null || _rtbResults == null ||
                _modelCardsPanel == null) return;

            int totalW = Math.Min(1400, Math.Max(400, ClientSize.Width - 40));
            int startX = Math.Max(20, (ClientSize.Width - totalW) / 2);

            _mainContainer.Location = new Point(startX, 20);
            _mainContainer.Width    = totalW;

            _headerPanel.Width     = totalW;
            _modelCard.Width       = totalW;
            _modelCardsPanel.Width = totalW - 40;

            int numModels  = _modelCardsPanel.Controls.Count;
            if (numModels > 0)
            {
                int targetBtnW = Math.Max(110, (totalW - 40 - (numModels - 1) * 10) / numModels);
                foreach (Control c in _modelCardsPanel.Controls)
                {
                    if (c is Button b) b.Width = targetBtnW;
                }
            }

            int flowH = Math.Max(44, _modelCardsPanel.PreferredSize.Height);
            _modelCardsPanel.Height = flowH;
            if (_lblModelDesc != null)
            {
                _lblModelDesc.Location = new Point(20, _modelCardsPanel.Bottom + 6);
                _modelCard.Height = _lblModelDesc.Bottom + 12;
            }
            else
            {
                _modelCard.Height = _modelCardsPanel.Bottom + 14;
            }

            int y = _modelCard.Bottom + 16;

            bool isWide = totalW >= 950;
            int leftW   = isWide ? (int)(totalW * 0.46) - 10 : totalW;
            int rightW  = isWide ? (int)(totalW * 0.54) - 10 : totalW;
            int rightX  = isWide ? leftW + 20 : 0;

            _leftColumn.Location = new Point(0, y);
            _leftColumn.Width    = leftW;

            _paramCard.Width      = leftW;
            _controlToolbar.Width = leftW;
            _btnTheory.Width      = leftW;
            _btnSave.Width        = leftW;

            _inputGrid.Width = leftW - 40;
            foreach (Control c in _inputGrid.Controls)
            {
                if (c is Panel p) p.Width = Math.Max(120, (_inputGrid.Width / 2) - 10);
            }

            int neededLeftH    = _btnSave.Bottom + 20;
            _leftColumn.Height = neededLeftH;

            int targetRightH   = Math.Max(580, neededLeftH);
            _resultCard.Location = isWide ? new Point(rightX, y) : new Point(0, _leftColumn.Bottom + 20);
            _resultCard.Width    = rightW;
            _resultCard.Height   = targetRightH;
            _rtbResults.Width    = rightW - 40;
            _rtbResults.Height   = targetRightH - 75;

            int bottomY = isWide ? Math.Max(_leftColumn.Bottom, _resultCard.Bottom) : _resultCard.Bottom;
            _mainContainer.Height = bottomY + 30;
            AutoScrollMinSize     = new Size(0, _mainContainer.Bottom + 40);

            PositionToolbarStatus();
        }

        private void PositionToolbarStatus()
        {
            if (_lblStatusDot == null || _lblStatusText == null || _controlToolbar == null) return;
            int rightMargin = 20;
            int dotW   = _lblStatusDot.PreferredSize.Width > 0 ? _lblStatusDot.PreferredSize.Width : 14;
            int textW  = _lblStatusText.PreferredSize.Width > 0 ? _lblStatusText.PreferredSize.Width : 70;
            int totalW = dotW + 6 + textW;

            int startX = _controlToolbar.Width - totalW - rightMargin;
            _lblStatusDot.Location  = new Point(startX, 13);
            _lblStatusText.Location = new Point(startX + dotW + 6, 15);
        }

        // ── Input Field Component Factory (Label Above Input) ─────────────────
        private Panel CreateInputField(string labelText, string unitText, out TextBox box, string defaultVal)
        {
            var p = new Panel { Size = new Size(270, 58), Margin = new Padding(0, 0, 10, 4) };
            var lbl = new Label
            {
                Text        = labelText,
                Font        = new Font("Segoe UI Semibold", 8.5f),
                ForeColor   = TextMid,
                Location    = new Point(0, 0),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(lbl);

            int boxW = !string.IsNullOrEmpty(unitText) ? 170 : 250;
            box = new TextBox
            {
                Text        = defaultVal,
                Font        = new Font("Segoe UI", 9.5f),
                Location    = new Point(0, 24),
                Size        = new Size(boxW, 28),
                BackColor   = InputBg,
                BorderStyle = BorderStyle.FixedSingle
            };
            p.Controls.Add(box);

            if (!string.IsNullOrEmpty(unitText))
            {
                var unit = new Label
                {
                    Text        = unitText,
                    Font        = new Font("Segoe UI", 8.5f),
                    ForeColor   = TextLight,
                    Location    = new Point(boxW + 8, 28),
                    AutoSize    = true,
                    UseMnemonic = false
                };
                p.Controls.Add(unit);
            }
            return p;
        }

        private Panel CreateNudField(string labelText, out Label labelCtrl, out NumericUpDown nud, int defaultVal)
        {
            var p = new Panel { Size = new Size(270, 58), Margin = new Padding(0, 0, 10, 4) };
            labelCtrl = new Label
            {
                Text        = labelText,
                Font        = new Font("Segoe UI Semibold", 8.5f),
                ForeColor   = TextMid,
                Location    = new Point(0, 0),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(labelCtrl);

            nud = new NumericUpDown
            {
                Location  = new Point(0, 24),
                Size      = new Size(250, 28),
                Minimum   = 1,
                Maximum   = 50,
                Value     = defaultVal,
                Font      = new Font("Segoe UI", 9.5f),
                BackColor = InputBg
            };
            p.Controls.Add(nud);
            return p;
        }

        private Panel CreateComboField(string labelText, out ComboBox cmb, string[] items, int defIdx)
        {
            var p = new Panel { Size = new Size(270, 58), Margin = new Padding(0, 0, 10, 4) };
            var lbl = new Label
            {
                Text        = labelText,
                Font        = new Font("Segoe UI Semibold", 8.5f),
                ForeColor   = TextMid,
                Location    = new Point(0, 0),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(lbl);

            cmb = new ComboBox
            {
                Font          = new Font("Segoe UI", 9f),
                Location      = new Point(0, 24),
                Size          = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = InputBg
            };
            cmb.Items.AddRange(items);
            if (items.Length > defIdx) cmb.SelectedIndex = defIdx;
            p.Controls.Add(cmb);
            return p;
        }

        private Panel CreateLabeledInputField(string labelText, out Label lblCtrl, out TextBox box)
        {
            var p = new Panel { Size = new Size(270, 58), Margin = new Padding(0, 0, 10, 4) };
            lblCtrl = new Label
            {
                Text        = labelText,
                Font        = new Font("Segoe UI Semibold", 8.5f),
                ForeColor   = TextMid,
                Location    = new Point(0, 0),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(lblCtrl);

            box = new TextBox
            {
                Text            = "",
                Font            = new Font("Segoe UI", 9.5f),
                Location        = new Point(0, 24),
                Size            = new Size(250, 28),
                BackColor       = InputBg,
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "optional"
            };
            p.Controls.Add(box);
            return p;
        }

        private Panel CreateCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = CardBg
            };
            card.Paint += (s, e) =>
            {
                DrawRoundedBorder(e.Graphics,
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12, Border);
            };
            Controls.Add(card);
            return card;
        }

        private void DrawRoundedBorder(Graphics g, Rectangle rect, int radius, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            using var pen = new Pen(color, 1.2f);
            g.DrawPath(pen, path);
        }

        private void AddCardSectionLabel(Panel parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(x, y),
                BackColor = Color.Transparent
            });
        }

        private Button CreateModelButton(string model)
        {
            var btn = new Button
            {
                Text      = model,
                Size      = new Size(130, 44),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10.5f),
                BackColor = model == _selectedModel ? AccentBlue : CardBg,
                ForeColor = model == _selectedModel ? Color.White : TextDark,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0),
                Tag       = model
            };
            btn.FlatAppearance.BorderColor = model == _selectedModel ? AccentBlue : Border;
            btn.FlatAppearance.BorderSize  = 1;
            btn.Click += ModelCard_Click;
            return btn;
        }

        private Button CreateStyledButton(string text, int y, Color bgColor, Color fgColor, int width)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(width, 44),
                Location  = new Point(0, y),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                BackColor = bgColor,
                ForeColor = fgColor,
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetStatus(string text, Color dotColor)
        {
            _lblStatusText.Text = text;
            _lblStatusDot.ForeColor = dotColor;
            PositionToolbarStatus();
        }

        // ── Model Logic (Unchanged) ────────────────────────────────────────────

        private void ModelCard_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string model)
            {
                _selectedModel = model;
                foreach (Control c in _modelCardsPanel.Controls)
                {
                    if (c is Button b)
                    {
                        bool active = (string)b.Tag! == model;
                        b.BackColor = active ? AccentBlue : CardBg;
                        b.ForeColor = active ? Color.White : TextDark;
                        b.FlatAppearance.BorderColor = active ? AccentBlue : Border;
                    }
                }
                UpdateVisibility();
                UpdateModelDescription();
            }
        }

        private void _cmbModel_Changed(object? sender, EventArgs e)
        {
            UpdateVisibility();
            UpdateModelDescription();
        }

        private void UpdateVisibility()
        {
            bool multiServer   = _selectedModel.Contains("/N");
            bool generalArrival= _selectedModel.StartsWith("G/");
            bool generalService= _selectedModel.Contains("/G/");

            _nudServers.Enabled = multiServer;
            _nudServers.Value   = multiServer ? Math.Max(_nudServers.Value, 2) : 1;
            if (!multiServer) _nudServers.Value = 1;

            _cmbArrivalDist.Enabled = generalArrival;
            if (!generalArrival) _cmbArrivalDist.SelectedIndex = 0;

            _cmbServiceDist.Enabled = generalService || generalArrival;
            if (!generalService && !generalArrival) _cmbServiceDist.SelectedIndex = 0;

            UpdateDistParams();
        }

        private void UpdateDistParams()
        {
            string svcDist = _cmbServiceDist.SelectedItem?.ToString() ?? "Exponential";
            bool showSvc   = svcDist != "Exponential" && svcDist != "Deterministic";
            _lblSvcParam1.Visible = showSvc;
            _txtSvcParam1.Visible = showSvc;
            _lblSvcParam2.Visible = svcDist == "Uniform";
            _txtSvcParam2.Visible = svcDist == "Uniform";

            if (svcDist == "Uniform") { _lblSvcParam1.Text = "Min Service Time"; _lblSvcParam2.Text = "Max Service Time"; }
            else if (svcDist == "Normal") { _lblSvcParam1.Text = "Std Deviation"; }
        }

        private void UpdateModelDescription()
        {
            _lblModelDesc.Text = _selectedModel switch
            {
                "M/M/1" => "M/M/1: Poisson arrivals • Exponential service • 1 server",
                "M/M/N" => "M/M/N: Poisson arrivals • Exponential service • N parallel servers",
                "M/G/1" => "M/G/1: Poisson arrivals • General service distribution • 1 server",
                "M/G/N" => "M/G/N: Poisson arrivals • General service • N servers (simulation)",
                "G/G/1" => "G/G/1: General arrivals • General service • 1 server (Kingman approx.)",
                "G/G/N" => "G/G/N: General arrivals • General service • N servers (simulation only)",
                _ => ""
            };
        }

        private void BtnTheory_Click(object? sender, EventArgs e)
        {
            if (!double.TryParse(_txtLambda.Text, out double lambda) || lambda <= 0)
            { ShowError("Arrival rate λ must be a positive number."); return; }
            if (!double.TryParse(_txtMu.Text, out double mu) || mu <= 0)
            { ShowError("Service rate μ must be a positive number."); return; }

            int servers = (int)_nudServers.Value;

            SimulationResult? res = _selectedModel switch
            {
                "M/M/1" => AnalyticalSolver.SolveMM1(lambda, mu),
                "M/M/N" => AnalyticalSolver.SolveMMN(lambda, mu, servers),
                "M/G/1" => AnalyticalSolver.SolveMG1(lambda, mu, _cmbServiceDist.SelectedItem?.ToString() ?? "Exponential"),
                "G/G/1" => AnalyticalSolver.SolveGG1(lambda, mu,
                    _cmbArrivalDist.SelectedItem?.ToString() ?? "Exponential",
                    _cmbServiceDist.SelectedItem?.ToString() ?? "Exponential"),
                _ => null
            };

            if (res == null || double.IsNaN(res.AnalyticalLq))
            {
                if (_selectedModel == "M/G/N" || _selectedModel == "G/G/N")
                {
                    _rtbResults.Text =
                        $"📐 THEORETICAL RESULTS ({_selectedModel})\n" +
                        "───────────────────────────────────────────────\n\n" +
                        "No closed-form analytical formula exists for M/G/N and G/G/N.\n\n" +
                        "Results are obtained strictly via Discrete-Event Simulation.\n\n" +
                        "Click ▶ START SIMULATION to run the simulation.";
                }
                else
                {
                    _rtbResults.Text =
                        $"⚠ SYSTEM IS UNSTABLE (ρ = {lambda / (servers * mu):F3} ≥ 1)\n\n" +
                        "Arrival rate exceeds total server capacity.\nQueue length and waiting time grow infinitely.";
                }
            }
            else
            {
                string approxLabel = _selectedModel == "G/G/1" ? " (Kingman Approx.)" : "";
                _rtbResults.Text =
                    $"📐 THEORETICAL RESULTS ({_selectedModel}){approxLabel}\n" +
                    "───────────────────────────────────────────────\n\n" +
                    $"  Arrival Rate (λ):   {lambda:F2} cust/hr\n" +
                    $"  Service Rate (μ):   {mu:F2} cust/hr\n" +
                    $"  Servers (N):        {servers}\n" +
                    $"  Utilization (ρ):    {res.AnalyticalRho * 100:F1}%\n\n" +
                    $"  Queue Length (Lq):  {res.AnalyticalLq:F4} customers\n" +
                    $"  In System (L):      {res.AnalyticalL:F4} customers\n" +
                    $"  Wait Time (Wq):     {res.AnalyticalWq * 60:F2} min ({res.AnalyticalWq:F4} hr)\n" +
                    $"  System Time (W):    {res.AnalyticalW * 60:F2} min ({res.AnalyticalW:F4} hr)\n" +
                    (!double.IsNaN(res.AnalyticalP0) ? $"  Idle Prob (P0):     {res.AnalyticalP0 * 100:F1}%\n" : "");
            }
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (!double.TryParse(_txtLambda.Text, out double lambda) || lambda <= 0)
            { ShowError("Arrival rate λ must be a positive number."); return; }
            if (!double.TryParse(_txtMu.Text, out double mu) || mu <= 0)
            { ShowError("Service rate μ must be a positive number."); return; }
            if (!double.TryParse(_txtSimTime.Text, out double simTime) || simTime <= 0)
            { ShowError("Simulation time must be positive."); return; }

            int servers = (int)_nudServers.Value;

            var (isStable, msg) = QueueStatistics.CheckStability(lambda, mu, servers);
            if (!isStable)
            {
                var dr = MessageBox.Show(msg + "\n\nContinue simulation anyway?",
                    "Stability Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
            }

            double.TryParse(_txtSvcParam1.Text, out double sp1);
            double.TryParse(_txtSvcParam2.Text, out double sp2);
            double.TryParse(_txtArrParam1.Text, out double ap1);
            double.TryParse(_txtArrParam2.Text, out double ap2);
            int? seed = null;
            if (int.TryParse(_txtSeed.Text, out int s)) seed = s;

            var engine = new SimulationEngine
            {
                Lambda              = lambda,
                Mu                  = mu,
                NumServers          = servers,
                SimulationTime      = simTime,
                ModelName           = _selectedModel,
                ArrivalDistribution = _cmbArrivalDist.SelectedItem?.ToString() ?? "Exponential",
                ServiceDistribution = _cmbServiceDist.SelectedItem?.ToString() ?? "Exponential",
                ServiceParam1       = sp1,
                ServiceParam2       = sp2,
                ArrivalParam1       = ap1,
                ArrivalParam2       = ap2,
                RandomSeed          = seed
            };

            _btnStart.Enabled = false;
            _btnStop.Enabled  = true;
            _btnReset.Enabled = true;

            SetStatus("RUNNING", WarnAmber);

            _rtbResults.Text =
                "● SIMULATION RUNNING…\n" +
                "───────────────────────────────────────────────\n\n" +
                "Live metrics and checkout queue visualization are updating on the Dashboard.\n\n" +
                "Final queueing statistics will be populated upon completion.";

            string speed = _cmbSpeed.SelectedItem?.ToString() ?? "Normal";
            OnStartSimulation?.Invoke(engine, speed);
        }

        public void OnSimulationFinished()
        {
            _btnStart.Enabled = true;
            _btnStop.Enabled  = false;
            _btnReset.Enabled = true;
            SetStatus("COMPLETED", GreenAccent);
        }

        public void ShowFinalResultsText(string text) => _rtbResults.Text = text;

        public void ResetResultsText()
        {
            _rtbResults.Text =
                "● READY\n\nSelect a model, enter parameters, and click:\n\n" +
                "  📐  VIEW THEORETICAL RESULTS\n      for analytical calculations\n\n" +
                "  ▶  START SIMULATION\n      to run the discrete-event simulation";
        }

        public void SetParameters(double lambda, double mu, int n)
        {
            _txtLambda.Text   = lambda.ToString("F1");
            _txtMu.Text       = mu.ToString("F1");
            _nudServers.Value = n;
        }

        private void ShowError(string message) =>
            MessageBox.Show(message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
