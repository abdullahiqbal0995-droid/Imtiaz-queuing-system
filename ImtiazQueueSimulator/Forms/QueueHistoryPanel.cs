using System.Drawing;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Queue history panel showing snapshot table and color-coded event timeline.
    /// Features:
    ///   - Event Legend bar (🟢 Start, 🟡 Joined Queue, 🔵 Service Started, 🔴 Departed)
    ///   - Color-coded grid rows with subtle background tint matching event types
    ///   - Proper fixed column widths preventing text collision
    ///   - Empty state label when no history snapshots exist
    /// </summary>
    public class QueueHistoryPanel : UserControl
    {
        private DataGridView _grid = null!;
        private Panel _timelinePanel = null!;
        private Label _emptyLabel = null!;

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
                Height    = 34,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            legendPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, legendPanel.Width - 1, legendPanel.Height - 1);
            };
            Controls.Add(legendPanel);

            int lx = 12;
            AddLegendItem(legendPanel, "🟢 Simulation Start", Color.FromArgb(22, 163, 74), ref lx);
            AddLegendItem(legendPanel, "🟡 Joined Queue",     Color.FromArgb(217, 119, 6), ref lx);
            AddLegendItem(legendPanel, "🔵 Service Started", Color.FromArgb(37, 99, 235), ref lx);
            AddLegendItem(legendPanel, "🔴 Departed",       Color.FromArgb(220, 38, 38), ref lx);

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
                SplitterDistance = 600,
                BackColor        = Color.FromArgb(244, 246, 250)
            };
            Controls.Add(splitContainer);

            Resize += (s, e) =>
            {
                legendPanel.Width = Math.Max(100, Width - 30);
                splitContainer.Size = new Size(Math.Max(100, Width - 30), Math.Max(100, Height - contentY - 15));
            };

            // Left: snapshot table
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
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(241, 245, 249),
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Font = new Font("Segoe UI Semibold", 9f),
                    Padding = new Padding(4)
                },
                EnableHeadersVisualStyles = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Time",     HeaderText = "Time",       Width = 75 },
                new DataGridViewTextBoxColumn { Name = "Event",    HeaderText = "Event",      Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer",   Width = 130 },
                new DataGridViewTextBoxColumn { Name = "Queue",    HeaderText = "Queue",      Width = 60 },
                new DataGridViewTextBoxColumn { Name = "System",   HeaderText = "System",     Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Busy",     HeaderText = "Busy Svrs",  Width = 75 }
            });
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            splitContainer.Panel1.Controls.Add(_grid);
            splitContainer.Panel1.Resize += (s, e) =>
            {
                _grid.Size = new Size(splitContainer.Panel1.Width - 10, splitContainer.Panel1.Height - 30);
            };

            // Right: event timeline
            var timelineLabel = new Label
            {
                Text      = "EVENT TIMELINE",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(5, 4)
            };
            splitContainer.Panel2.Controls.Add(timelineLabel);

            _timelinePanel = new Panel
            {
                Location   = new Point(5, 26),
                BackColor  = Color.White,
                AutoScroll = true,
                Anchor     = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _timelinePanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, _timelinePanel.Width - 1, _timelinePanel.Height - 1);
            };
            splitContainer.Panel2.Controls.Add(_timelinePanel);
            splitContainer.Panel2.Resize += (s, e) =>
            {
                _timelinePanel.Size = new Size(splitContainer.Panel2.Width - 10, splitContainer.Panel2.Height - 30);
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
                Location  = new Point(x, 8)
            };
            parent.Controls.Add(lbl);
            x += lbl.PreferredWidth + 24;
        }

        public void LoadSnapshots(List<QueueSnapshot> snapshots)
        {
            _grid.Rows.Clear();
            _timelinePanel.Controls.Clear();
            _emptyLabel.Visible = snapshots.Count == 0;

            int timelineY = 10;

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

                // Timeline Entry
                var entry = new Label
                {
                    Text        = $"{s.EventIcon} {s.FormattedTime} — {s.EventDescription}: {s.CustomerInfo}",
                    Font        = new Font("Segoe UI", 8.5f),
                    ForeColor   = Color.FromArgb(30, 41, 59),
                    AutoSize    = true,
                    Location    = new Point(12, timelineY),
                    MaximumSize = new Size(_timelinePanel.Width - 30, 0)
                };
                _timelinePanel.Controls.Add(entry);
                timelineY += 24;
            }
        }
    }
}
