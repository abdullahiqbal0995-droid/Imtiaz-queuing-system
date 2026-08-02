using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Controls;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Enterprise CRM dashboard modal dialog displaying complete customer profile details,
    /// system arrival metrics, and interactive event timeline.
    /// Rebuilt from scratch using strict docking stack and flex layout containers.
    /// </summary>
    public class CustomerDetailForm : Form
    {
        private readonly Customer _customer;

        // Top-level Structure Panels
        private Panel           _headerBar     = null!;
        private Panel           _heroBanner    = null!;
        private Panel           _bodyPanel     = null!;
        private Panel           _footerBar     = null!;
        private Button          _btnClose      = null!;

        // Grid Container
        private TableLayoutPanel _gridContainer = null!;

        // Left Column Cards
        private Panel           _infoCard      = null!;
        private Panel           _stateCard     = null!;

        // Right Column Card
        private Panel           _journeyCard   = null!;
        private TimelineControl _timelineCtrl  = null!;

        public CustomerDetailForm(Customer customer)
        {
            _customer = customer ?? throw new ArgumentNullException(nameof(customer));
            InitializeForm();
            BuildUI();
        }

        private void InitializeForm()
        {
            Text            = $"Customer Details — {_customer.Name}";
            Size            = new Size(1080, 780);
            MinimumSize     = new Size(960, 680);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(248, 250, 252); // #F8FAFC
        }

        private void BuildUI()
        {
            Controls.Clear();

            // ── 1. Top Header Bar (60px) ──────────────────────────────────────
            _headerBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Color.White
            };
            _headerBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235), 1.2f);
                e.Graphics.DrawLine(pen, 0, _headerBar.Height - 1, _headerBar.Width, _headerBar.Height - 1);
            };

            var lblHeaderTitle = new Label
            {
                Text      = "👤  Customer Details — " + _customer.Name,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(24, 18),
                UseMnemonic = false
            };
            _headerBar.Controls.Add(lblHeaderTitle);

            var btnTopClose = new Button
            {
                Text      = "✕",
                Size      = new Size(38, 38),
                Location  = new Point(_headerBar.Width - 50, 11),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Cursor    = Cursors.Hand,
                BackColor = Color.Transparent
            };
            btnTopClose.FlatAppearance.BorderSize = 0;
            btnTopClose.Click += (s, e) => Close();
            _headerBar.Controls.Add(btnTopClose);

            // ── 2. Hero Profile Banner (150px) ────────────────────────────────
            _heroBanner = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 150,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            _heroBanner.Paint += (s, e) =>
            {
                using var brush = new LinearGradientBrush(
                    _heroBanner.ClientRectangle,
                    Color.FromArgb(15, 23, 42),
                    Color.FromArgb(30, 41, 59),
                    LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brush, _heroBanner.ClientRectangle);
            };

            // Avatar 74x74px Circle (Non-overlapping font size 13pt)
            var avatarPanel = new Panel
            {
                Size      = new Size(74, 74),
                Location  = new Point(28, 38),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                path.AddEllipse(0, 0, avatarPanel.Width - 1, avatarPanel.Height - 1);
                using var bgB = new SolidBrush(Color.FromArgb(37, 99, 235));
                e.Graphics.FillPath(bgB, path);

                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var f  = new Font("Segoe UI", 13f, FontStyle.Bold);
                using var b  = new SolidBrush(Color.White);
                e.Graphics.DrawString($"C{_customer.Id:D3}", f, b, avatarPanel.ClientRectangle, sf);
            };
            _heroBanner.Controls.Add(avatarPanel);

            // Hero Info Layout (FlowStack to guarantee ZERO overlapping text)
            int infoX = 118;

            var lblHeroName = new Label
            {
                Text        = _customer.Name,
                Font        = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor   = Color.White,
                AutoSize    = true,
                Location    = new Point(infoX, 28),
                UseMnemonic = false
            };
            _heroBanner.Controls.Add(lblHeroName);

            // Sub-row: Status Badge + Metadata
            Color statusBg = _customer.Status switch
            {
                "Completed" => Color.FromArgb(22, 163, 74),  // #16A34A
                "InService" => Color.FromArgb(37, 99, 235),  // #2563EB
                "Waiting"   => Color.FromArgb(217, 119, 6),  // #D97706
                _           => Color.FromArgb(100, 116, 139)
            };
            string statusIcon = _customer.Status switch
            {
                "Completed" => "✔ COMPLETED",
                "InService" => "⚡ IN SERVICE",
                "Waiting"   => "⏳ WAITING",
                _           => _customer.Status.ToUpper()
            };

            int subRowY = 76;
            var badgePanel = new Panel
            {
                Size      = new Size(136, 28),
                Location  = new Point(infoX, subRowY),
                BackColor = statusBg
            };
            badgePanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(badgePanel.ClientRectangle, 14);
                using var b = new SolidBrush(statusBg);
                e.Graphics.FillPath(b, path);

                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var f  = new Font("Segoe UI Semibold", 8.75f);
                using var tb = new SolidBrush(Color.White);
                e.Graphics.DrawString(statusIcon, f, tb, badgePanel.ClientRectangle, sf);
            };
            _heroBanner.Controls.Add(badgePanel);

            var lblHeroMeta = new Label
            {
                Text        = $"Customer ID: #{_customer.Id:D3}   •   Assigned Server: {(_customer.AssignedServer > 0 ? $"Cashier {_customer.AssignedServer:D2}" : "—")}",
                Font        = new Font("Segoe UI", 9.5f),
                ForeColor   = Color.FromArgb(148, 163, 184),
                AutoSize    = true,
                Location    = new Point(infoX + 150, subRowY + 4),
                UseMnemonic = false
            };
            _heroBanner.Controls.Add(lblHeroMeta);

            // ── 3. Bottom Footer Bar (70px) ───────────────────────────────────
            _footerBar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 70,
                BackColor = Color.White
            };
            _footerBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235), 1.2f);
                e.Graphics.DrawLine(pen, 0, 0, _footerBar.Width, 0);
            };

            _btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(140, 44),
                Location  = new Point(_footerBar.Width - 164, 13),
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 10f),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Cursor    = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Close();
            _footerBar.Controls.Add(_btnClose);

            // ── 4. Main Scrollable Body Panel (Dock Fill) ────────────────────
            _bodyPanel = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(24),
                BackColor  = Color.FromArgb(248, 250, 252)
            };

            // CRITICAL WINFORMS DOCK ORDER: Top and Bottom controls added FIRST, Fill added LAST!
            Controls.Add(_headerBar);   // Dock Top (60px)
            Controls.Add(_heroBanner);  // Dock Top (150px)
            Controls.Add(_footerBar);   // Dock Bottom (70px)
            Controls.Add(_bodyPanel);   // Dock Fill (takes remaining space y = 210..690)

            _bodyPanel.BringToFront();

            // ── 5. 2-Column Responsive Grid Container ────────────────────────
            _gridContainer = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                AutoSize    = true,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0),
                Padding     = new Padding(0)
            };
            _gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            _gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            _bodyPanel.Controls.Add(_gridContainer);

            // ── LEFT COLUMN (Column 0): Stacked Cards ────────────────────────
            var leftStack = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                AutoSize    = true,
                ColumnCount = 1,
                RowCount    = 2,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0, 0, 12, 0),
                Padding     = new Padding(0)
            };
            leftStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _gridContainer.Controls.Add(leftStack, 0, 0);

            // Card 1: Customer Information Card
            _infoCard = CreateCardPanel();
            leftStack.Controls.Add(_infoCard, 0, 0);

            AddCardTitle(_infoCard, "CUSTOMER INFORMATION", 20, 16);

            var infoTable = new TableLayoutPanel
            {
                Location    = new Point(20, 48),
                Width       = 390,
                AutoSize    = true,
                ColumnCount = 2,
                BackColor   = Color.Transparent,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            AddRowToTable(infoTable, "Customer ID", $"{_customer.Id:D3}");
            AddRowToTable(infoTable, "Customer Name", _customer.Name);
            AddRowToTable(infoTable, "Current Status", _customer.Status, statusBg);
            AddDividerRowToTable(infoTable);

            AddRowToTable(infoTable, "Arrival Time", _customer.DisplayArrival);
            AddRowToTable(infoTable, "Queue Entry", Customer.FormatTime(_customer.QueueEntryTime));
            AddRowToTable(infoTable, "Service Start", _customer.DisplaySvcStart);
            AddRowToTable(infoTable, "Departure Time", _customer.DisplayDeparture);
            AddDividerRowToTable(infoTable);

            AddRowToTable(infoTable, "Waiting Time (Wq)", _customer.DisplayWq, Color.FromArgb(217, 119, 6));
            AddRowToTable(infoTable, "Service Time", _customer.DisplayService);
            AddRowToTable(infoTable, "System Time (W)", _customer.DisplayW, Color.FromArgb(124, 58, 237));
            AddRowToTable(infoTable, "Assigned Server", _customer.AssignedServer > 0 ? $"Cashier {_customer.AssignedServer:D2}" : "—");

            _infoCard.Controls.Add(infoTable);
            _infoCard.Height = infoTable.Bottom + 20;

            // Card 2: System State Card (2x2 Grid of Stat Boxes)
            _stateCard = CreateCardPanel();
            _stateCard.Margin = new Padding(0, 18, 0, 0);
            leftStack.Controls.Add(_stateCard, 0, 1);

            AddCardTitle(_stateCard, "SYSTEM STATE ON ARRIVAL & SERVICE", 20, 16);

            var statGrid = new TableLayoutPanel
            {
                Location    = new Point(20, 48),
                Size        = new Size(390, 130),
                ColumnCount = 2,
                RowCount    = 2,
                BackColor   = Color.Transparent,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            statGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            statGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            statGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            statGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            statGrid.Controls.Add(CreateStatBox("Queue on Arrival", _customer.QueueLengthOnArrival.ToString()), 0, 0);
            statGrid.Controls.Add(CreateStatBox("System on Arrival", _customer.SystemSizeOnArrival.ToString()), 1, 0);
            statGrid.Controls.Add(CreateStatBox("Queue on Svc Start", _customer.QueueLengthOnServiceStart.ToString()), 0, 1);
            statGrid.Controls.Add(CreateStatBox("System on Svc Start", _customer.SystemSizeOnServiceStart.ToString()), 1, 1);

            _stateCard.Controls.Add(statGrid);
            _stateCard.Height = statGrid.Bottom + 20;

            // ── RIGHT COLUMN (Column 1): Journey Card ────────────────────────
            var rightStack = new TableLayoutPanel
            {
                Dock          = DockStyle.Fill,
                AutoSize      = true,
                ColumnCount   = 1,
                RowCount      = 1,
                BackColor     = Color.Transparent,
                Margin        = new Padding(12, 0, 0, 0),
                Padding       = new Padding(0)
            };
            rightStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _gridContainer.Controls.Add(rightStack, 1, 0);

            // Single Journey Card (Timeline + Summary Box inside)
            int minRightHeight = Math.Max(560, _infoCard.Height + _stateCard.Height + 18);
            _journeyCard = CreateCardPanel(520, minRightHeight);
            rightStack.Controls.Add(_journeyCard, 0, 0);

            _timelineCtrl = new TimelineControl
            {
                Dock             = DockStyle.Fill,
                ArrivalTime      = _customer.DisplayArrival,
                ServiceStartTime = _customer.DisplaySvcStart,
                DepartureTime    = _customer.DisplayDeparture,
                WaitingDuration  = _customer.DisplayWq,
                ServiceDuration  = _customer.DisplayService,
                TotalDuration    = _customer.DisplayW,
                AssignedServer   = _customer.AssignedServer > 0 ? $"Cashier {_customer.AssignedServer:D2}" : "Cashier 01"
            };
            _journeyCard.Controls.Add(_timelineCtrl);

            Resize += (s, e) => PerformResponsiveLayout();
            PerformResponsiveLayout();
        }

        private void PerformResponsiveLayout()
        {
            if (_bodyPanel == null || _gridContainer == null || _infoCard == null || _stateCard == null || _journeyCard == null) return;

            int availWidth = _bodyPanel.ClientSize.Width - 48;
            if (availWidth <= 0) return;

            _gridContainer.Width = availWidth;

            bool isWide = availWidth >= 820;
            if (isWide)
            {
                _gridContainer.ColumnCount = 2;
                _gridContainer.ColumnStyles.Clear();
                _gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
                _gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

                int leftW = (int)(availWidth * 0.45) - 12;
                int rightW = (int)(availWidth * 0.55) - 12;

                _infoCard.Width = leftW;
                _stateCard.Width = leftW;

                int targetRightH = Math.Max(560, _infoCard.Height + _stateCard.Height + 18);
                _journeyCard.Size = new Size(rightW, targetRightH);
            }
            else
            {
                _gridContainer.ColumnCount = 1;
                _gridContainer.ColumnStyles.Clear();
                _gridContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

                _infoCard.Width = availWidth;
                _stateCard.Width = availWidth;
                _journeyCard.Size = new Size(availWidth, 560);
            }
        }

        private Panel CreateCardPanel(int width = 430, int height = 300)
        {
            var card = new Panel
            {
                Size      = new Size(width, height),
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                using var pen  = new Pen(Color.FromArgb(229, 231, 235), 1.2f);
                e.Graphics.DrawPath(pen, path);
            };
            return card;
        }

        private Panel CreateStatBox(string title, string value)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), BackColor = Color.FromArgb(248, 250, 252) };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = CreateRoundedPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 8);
                using var pen  = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawPath(pen, path);
            };

            var lblT = new Label
            {
                Text        = title,
                Font        = new Font("Segoe UI Semibold", 8f),
                ForeColor   = Color.FromArgb(100, 116, 139),
                Location    = new Point(10, 5),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(lblT);

            var lblV = new Label
            {
                Text        = value,
                Font        = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor   = Color.FromArgb(30, 41, 59),
                Location    = new Point(10, 24),
                AutoSize    = true,
                UseMnemonic = false
            };
            p.Controls.Add(lblV);

            return p;
        }

        private void AddCardTitle(Panel parent, string title, int x, int y)
        {
            var lbl = new Label
            {
                Text        = title,
                Font        = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor   = Color.FromArgb(30, 41, 59),
                AutoSize    = true,
                Location    = new Point(x, y),
                UseMnemonic = false
            };
            parent.Controls.Add(lbl);
        }

        private void AddRowToTable(TableLayoutPanel table, string label, string value, Color? valColor = null)
        {
            int rowIdx = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

            var lblL = new Label
            {
                Text        = label,
                Font        = new Font("Segoe UI Semibold", 8.5f),
                ForeColor   = Color.FromArgb(100, 116, 139),
                Dock        = DockStyle.Fill,
                TextAlign   = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            table.Controls.Add(lblL, 0, rowIdx);

            var lblV = new Label
            {
                Text         = value,
                Font         = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor    = valColor ?? Color.FromArgb(30, 41, 59),
                Dock         = DockStyle.Fill,
                TextAlign    = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                UseMnemonic  = false
            };
            table.Controls.Add(lblV, 1, rowIdx);
        }

        private void AddDividerRowToTable(TableLayoutPanel table)
        {
            int rowIdx = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));

            var line = new Panel
            {
                Dock      = DockStyle.Fill,
                Margin    = new Padding(0, 4, 0, 4),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            table.Controls.Add(line, 0, rowIdx);
            table.SetColumnSpan(line, 2);
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
