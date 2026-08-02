using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Queue history panel showing snapshot table and color-coded event timeline.
    /// Features:
    ///   - Event Legend bar (🟢 Start, 🟡 Joined Queue, 🔵 Service Started, 🔴 Departed)
    ///   - Color-coded grid rows with subtle background tint matching event types
    ///   - Clean auto-proportioned column widths preventing text truncation
    ///   - Event Timeline with structured, non-overlapping cards and dynamic layout
    /// </summary>
    public class QueueHistoryPanel : UserControl
    {
        private DataGridView _grid = null!;
        private FlowLayoutPanel _timelineFlow = null!;
        private Label _emptyLabel = null!;
        private List<QueueSnapshot> _currentSnapshots = new();

        public QueueHistoryPanel()
        {
            BackColor = Color.FromArgb(244, 246, 250);
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Title ─────────────────────────────────────────────────────────
            var title = new Label
            {
                Text      = "📋 QUEUE HISTORY & TIMELINE",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(15, 12)
            };
            Controls.Add(title);

            // ── Event Legend Bar ──────────────────────────────────────────────
            int legendY = title.Bottom + 10;
            var legendPanel = new Panel
            {
                Location  = new Point(15, legendY),
                Height    = 36,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            legendPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, legendPanel.Width - 1, legendPanel.Height - 1);
            };
            Controls.Add(legendPanel);

            int lx = 16;
            AddLegendItem(legendPanel, "🟢 Simulation Start", Color.FromArgb(22, 163, 74), ref lx);
            AddLegendItem(legendPanel, "🟡 Joined Queue",     Color.FromArgb(217, 119, 6), ref lx);
            AddLegendItem(legendPanel, "🔵 Service Started", Color.FromArgb(37, 99, 235), ref lx);
            AddLegendItem(legendPanel, "🔴 Departed",        Color.FromArgb(220, 38, 38), ref lx);

            int emptyY = legendPanel.Bottom + 10;

            // ── Empty State Label ─────────────────────────────────────────────
            _emptyLabel = new Label
            {
                Text      = "No queue history snapshots yet. Start a simulation to record queue events.",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(15, emptyY),
                Visible   = true
            };
            Controls.Add(_emptyLabel);

            int contentY = emptyY + 28;

            // ── SplitContainer (Grid on left, Timeline on right) ──────────────
            var splitContainer = new SplitContainer
            {
                Location         = new Point(15, contentY),
                Size             = new Size(Math.Max(100, Width - 30), Math.Max(100, Height - contentY - 15)),
                Anchor           = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Orientation      = Orientation.Vertical,
                SplitterDistance = 640,
                BackColor        = Color.FromArgb(244, 246, 250)
            };
            Controls.Add(splitContainer);

            Resize += (s, e) =>
            {
                legendPanel.Width = Math.Max(100, Width - 30);
                splitContainer.Size = new Size(Math.Max(100, Width - 30), Math.Max(100, Height - contentY - 15));
            };

            // ── Left: Snapshot Table ──────────────────────────────────────────
            var gridLabel = new Label
            {
                Text      = "SNAPSHOT LOG",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(5, 4)
            };
            splitContainer.Panel1.Controls.Add(gridLabel);

            _grid = new DataGridView
            {
                Location = new Point(5, 26),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 34 },
                GridColor = Color.FromArgb(241, 245, 249),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(15, 23, 42),      // Dark Navy Slate 900
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Bold", 9.5f),
                    Padding = new Padding(6, 0, 6, 0)
                },
                DefaultCellStyle =
                {
                    SelectionBackColor = Color.FromArgb(239, 246, 255),
                    SelectionForeColor = Color.FromArgb(15, 23, 42),
                    Padding = new Padding(6, 0, 6, 0)
                },
                EnableHeadersVisualStyles = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var colTime     = new DataGridViewTextBoxColumn { Name = "Time",     HeaderText = "Time",         MinimumWidth = 80,  FillWeight = 14, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            var colEvent    = new DataGridViewTextBoxColumn { Name = "Event",    HeaderText = "Event",        MinimumWidth = 140, FillWeight = 24 };
            var colCustomer = new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer",     MinimumWidth = 200, FillWeight = 40 }; // Prevents truncation!
            var colQueue    = new DataGridViewTextBoxColumn { Name = "Queue",    HeaderText = "Queue",        MinimumWidth = 65,  FillWeight = 7,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            var colSystem   = new DataGridViewTextBoxColumn { Name = "System",   HeaderText = "System",       MinimumWidth = 65,  FillWeight = 7,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };
            var colBusy     = new DataGridViewTextBoxColumn { Name = "Busy",     HeaderText = "Busy Servers", MinimumWidth = 90,  FillWeight = 10, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } };

            _grid.Columns.AddRange(new DataGridViewColumn[] { colTime, colEvent, colCustomer, colQueue, colSystem, colBusy });
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            splitContainer.Panel1.Controls.Add(_grid);
            splitContainer.Panel1.Resize += (s, e) =>
            {
                _grid.Size = new Size(Math.Max(50, splitContainer.Panel1.Width - 10), Math.Max(50, splitContainer.Panel1.Height - 30));
            };

            // ── Right: Event Timeline ─────────────────────────────────────────
            var timelineLabel = new Label
            {
                Text      = "EVENT TIMELINE",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(5, 4)
            };
            splitContainer.Panel2.Controls.Add(timelineLabel);

            _timelineFlow = new FlowLayoutPanel
            {
                Location      = new Point(5, 26),
                BackColor     = Color.White,
                AutoScroll    = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                Padding       = new Padding(10),
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _timelineFlow.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, _timelineFlow.Width - 1, _timelineFlow.Height - 1);
            };

            splitContainer.Panel2.Controls.Add(_timelineFlow);
            splitContainer.Panel2.Resize += (s, e) =>
            {
                _timelineFlow.Size = new Size(Math.Max(50, splitContainer.Panel2.Width - 10), Math.Max(50, splitContainer.Panel2.Height - 30));
                UpdateTimelineItemWidths();
            };
        }

        private void AddLegendItem(Panel parent, string text, Color color, ref int x)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = color,
                AutoSize  = true,
                Location  = new Point(x, 9)
            };
            parent.Controls.Add(lbl);
            x += lbl.PreferredWidth + 24;
        }

        public void LoadSnapshots(List<QueueSnapshot> snapshots)
        {
            _currentSnapshots = snapshots;
            _grid.Rows.Clear();
            _timelineFlow.Controls.Clear();
            _emptyLabel.Visible = snapshots.Count == 0;

            _timelineFlow.SuspendLayout();

            int cardWidth = Math.Max(200, _timelineFlow.ClientSize.Width - 28);

            foreach (var s in snapshots)
            {
                int rowIdx = _grid.Rows.Add(
                    s.FormattedTime,
                    $"{s.EventIcon} {s.EventDescription}",
                    s.CustomerInfo,
                    s.QueueLength,
                    s.CustomersInSystem,
                    s.BusyServers
                );

                // Row background color-coding matching event type
                Color rowBg = s.EventIcon switch
                {
                    "🟢" => Color.FromArgb(240, 253, 244), // light green
                    "🟡" => Color.FromArgb(254, 252, 232), // light yellow/amber
                    "🔵" => Color.FromArgb(239, 246, 255), // light blue
                    "🔴" => Color.FromArgb(254, 242, 242), // light red
                    "✅" => Color.FromArgb(240, 253, 244),
                    _   => Color.White
                };
                _grid.Rows[rowIdx].DefaultCellStyle.BackColor = rowBg;

                // Timeline Entry Card (Structured, zero text overlap)
                var card = CreateTimelineCard(s, cardWidth);
                _timelineFlow.Controls.Add(card);
            }

            _timelineFlow.ResumeLayout(true);
        }

        private void UpdateTimelineItemWidths()
        {
            if (_timelineFlow == null || _timelineFlow.Controls.Count == 0) return;
            int targetWidth = Math.Max(200, _timelineFlow.ClientSize.Width - 28);

            _timelineFlow.SuspendLayout();
            foreach (Control c in _timelineFlow.Controls)
            {
                if (c is Panel card)
                {
                    card.Width = targetWidth;
                }
            }
            _timelineFlow.ResumeLayout(true);
        }

        private Panel CreateTimelineCard(QueueSnapshot s, int width)
        {
            Color accentColor = s.EventIcon switch
            {
                "🟢" => Color.FromArgb(22, 163, 74),   // Green
                "🟡" => Color.FromArgb(217, 119, 6),   // Amber
                "🔵" => Color.FromArgb(37, 99, 235),   // Blue
                "🔴" => Color.FromArgb(220, 38, 38),   // Red
                "✅" => Color.FromArgb(22, 163, 74),   // Green
                _   => Color.FromArgb(100, 116, 139)
            };

            Color cardBg = s.EventIcon switch
            {
                "🟢" => Color.FromArgb(240, 253, 244),
                "🟡" => Color.FromArgb(254, 252, 232),
                "🔵" => Color.FromArgb(239, 246, 255),
                "🔴" => Color.FromArgb(254, 242, 242),
                "✅" => Color.FromArgb(240, 253, 244),
                _   => Color.FromArgb(248, 250, 252)
            };

            Color borderColor = s.EventIcon switch
            {
                "🟢" => Color.FromArgb(187, 247, 208),
                "🟡" => Color.FromArgb(254, 240, 138),
                "🔵" => Color.FromArgb(191, 219, 254),
                "🔴" => Color.FromArgb(254, 202, 202),
                "✅" => Color.FromArgb(187, 247, 208),
                _   => Color.FromArgb(226, 232, 240)
            };

            bool hasCustomerInfo = !string.IsNullOrWhiteSpace(s.CustomerInfo);
            int cardHeight = hasCustomerInfo ? 54 : 36;

            var card = new Panel
            {
                Size      = new Size(width, cardHeight),
                BackColor = cardBg,
                Margin    = new Padding(0, 0, 0, 8)
            };

            card.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var borderPen = new Pen(borderColor, 1f);
                DrawRoundedRect(e.Graphics, borderPen, r, 6);

                // Left accent bar (4px wide)
                using var accentBrush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(accentBrush, 0, 0, 4, card.Height);
            };

            // Event Header Label (Icon + Description)
            var lblEvent = new Label
            {
                Text        = $"{s.EventIcon}  {s.EventDescription}",
                Font        = new Font("Segoe UI Semibold", 9f),
                ForeColor   = accentColor,
                AutoSize    = true,
                Location    = new Point(12, 8),
                UseMnemonic = false
            };
            card.Controls.Add(lblEvent);

            // Timestamp Badge (Right Aligned)
            var lblTime = new Label
            {
                Text        = s.FormattedTime,
                Font        = new Font("Segoe UI Bold", 9f),
                ForeColor   = Color.FromArgb(30, 41, 59),
                AutoSize    = true,
                Location    = new Point(width - 75, 8),
                Anchor      = AnchorStyles.Top | AnchorStyles.Right,
                UseMnemonic = false
            };
            card.Controls.Add(lblTime);

            // Sub-details: Customer Info (If present)
            if (hasCustomerInfo)
            {
                var lblCustomer = new Label
                {
                    Text         = s.CustomerInfo,
                    Font         = new Font("Segoe UI", 8.5f),
                    ForeColor    = Color.FromArgb(51, 65, 85),
                    AutoSize     = false,
                    Size         = new Size(Math.Max(50, width - 24), 18),
                    Location     = new Point(12, 28),
                    AutoEllipsis = true,
                    Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    UseMnemonic  = false
                };
                card.Controls.Add(lblCustomer);
            }

            return card;
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
    }
}
