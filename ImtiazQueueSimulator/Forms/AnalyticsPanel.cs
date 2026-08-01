using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Executive Analytics Panel featuring responsive GDI+ custom-drawn charts.
    /// Includes responsive 2-column layout, full vertical scrolling, zero text overlap,
    /// precise graph plot clipping, and dynamic Server Activity Gantt Charts (1 track per cashier).
    /// </summary>
    public class AnalyticsPanel : UserControl
    {
        private SimulationResult? _result;
        private Panel _mainContainer = null!;
        private Label _titleLabel = null!;
        private Label _subTitleLabel = null!;

        private Panel _chartPanel1 = null!;
        private Panel _chartPanel2 = null!;
        private Panel _chartPanel3 = null!;
        private Panel _chartPanel4 = null!;
        private Panel _chartPanel5 = null!;
        private Panel _chartPanel6 = null!; // Gantt Chart

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg      = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg      = Color.White;
        private static readonly Color TextDark    = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid     = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight   = Color.FromArgb(100, 116, 139);
        private static readonly Color Border      = Color.FromArgb(226, 232, 240);
        private static readonly Color GridLinePen = Color.FromArgb(241, 245, 249);

        public AnalyticsPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            _mainContainer = new Panel
            {
                Location  = new Point(20, 20),
                Width     = Math.Max(400, ClientSize.Width - 40),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            Controls.Add(_mainContainer);

            int y = 0;

            // ── Page title ─────────────────────────────────────────────────────
            _titleLabel = new Label
            {
                Text      = "📊  ANALYTICS & CHARTS",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(0, y),
                BackColor = Color.Transparent
            };
            _mainContainer.Controls.Add(_titleLabel);
            y += 32;

            _subTitleLabel = new Label
            {
                Text      = "Visual analysis of simulation results — run a simulation first to populate real-time charts & Gantt timeline.",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(0, y),
                BackColor = Color.Transparent
            };
            _mainContainer.Controls.Add(_subTitleLabel);
            y += 42;

            // ── 6 Chart Panels ─────────────────────────────────────────────────
            _chartPanel1 = CreateChartPanel("Queue Length vs Time");
            _chartPanel2 = CreateChartPanel("Customers in System vs Time");
            _chartPanel3 = CreateChartPanel("Waiting Time Distribution");
            _chartPanel4 = CreateChartPanel("Server Utilization");
            _chartPanel5 = CreateChartPanel("Cumulative Arrivals vs Departures");
            _chartPanel6 = CreateChartPanel("Server Activity Timeline (Gantt Chart)");

            _chartPanel1.Paint += PaintQueueLengthChart;
            _chartPanel2.Paint += PaintSystemSizeChart;
            _chartPanel3.Paint += PaintWaitingTimeChart;
            _chartPanel4.Paint += PaintUtilizationChart;
            _chartPanel5.Paint += PaintArrivalDepartureChart;
            _chartPanel6.Paint += PaintGanttChart;

            _mainContainer.Controls.Add(_chartPanel1);
            _mainContainer.Controls.Add(_chartPanel2);
            _mainContainer.Controls.Add(_chartPanel3);
            _mainContainer.Controls.Add(_chartPanel4);
            _mainContainer.Controls.Add(_chartPanel5);
            _mainContainer.Controls.Add(_chartPanel6);

            Resize += (s, e) => PerformCustomLayout();
            PerformCustomLayout();
        }

        private Panel CreateChartPanel(string title)
        {
            var panel = new Panel
            {
                Size      = new Size(500, 290),
                BackColor = CardBg
            };

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var r = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

                // Card fill
                using (var bgb = new SolidBrush(CardBg))
                {
                    using var cp = RoundPath(r, 10);
                    g.FillPath(bgb, cp);
                }

                // Border
                using (var pen = new Pen(Border, 1.2f))
                {
                    using var cp = RoundPath(r, 10);
                    g.DrawPath(pen, cp);
                }

                // Title background strip
                using (var hb = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    g.FillRectangle(hb, 1, 1, panel.Width - 2, 38);

                // Title divider
                using (var dp = new Pen(Border, 1f))
                    g.DrawLine(dp, 0, 38, panel.Width, 38);

                // Title text
                using var tf = new Font("Segoe UI Semibold", 9.5f);
                using var tb = new SolidBrush(TextDark);
                g.DrawString(title, tf, tb, 14, 10);
            };

            return panel;
        }

        private void PerformCustomLayout()
        {
            if (_mainContainer == null || _chartPanel1 == null) return;

            int availW = Math.Max(400, ClientSize.Width - 40);
            _mainContainer.Width = availW;

            bool isWide = availW >= 880;
            int gap = 20;
            int chartH = 290;

            int chartW = isWide ? (availW - gap) / 2 : availW;

            int numServers = _result != null && _result.NumServers > 0 ? _result.NumServers : 1;
            int ganttH = Math.Max(290, 85 + numServers * 42);

            _chartPanel1.Size = new Size(chartW, chartH);
            _chartPanel2.Size = new Size(chartW, chartH);
            _chartPanel3.Size = new Size(chartW, chartH);
            _chartPanel4.Size = new Size(chartW, chartH);
            _chartPanel5.Size = new Size(chartW, chartH);
            _chartPanel6.Size = new Size(isWide ? availW : chartW, ganttH);

            int startY = 74;

            if (isWide)
            {
                // Row 1
                _chartPanel1.Location = new Point(0, startY);
                _chartPanel2.Location = new Point(chartW + gap, startY);

                // Row 2
                int r2Y = startY + chartH + gap;
                _chartPanel3.Location = new Point(0, r2Y);
                _chartPanel4.Location = new Point(chartW + gap, r2Y);

                // Row 3
                int r3Y = r2Y + chartH + gap;
                _chartPanel5.Location = new Point(0, r3Y);

                // Row 4: Gantt Chart (Spans full width for optimal timeline resolution!)
                int r4Y = r3Y + chartH + gap;
                _chartPanel6.Location = new Point(0, r4Y);

                _mainContainer.Height = r4Y + ganttH + 30;
            }
            else
            {
                // Single column layout
                int currY = startY;
                _chartPanel1.Location = new Point(0, currY); currY += chartH + gap;
                _chartPanel2.Location = new Point(0, currY); currY += chartH + gap;
                _chartPanel3.Location = new Point(0, currY); currY += chartH + gap;
                _chartPanel4.Location = new Point(0, currY); currY += chartH + gap;
                _chartPanel5.Location = new Point(0, currY); currY += chartH + gap;
                _chartPanel6.Location = new Point(0, currY); currY += ganttH + gap;

                _mainContainer.Height = currY + 10;
            }

            AutoScrollMinSize = new Size(0, _mainContainer.Bottom + 30);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void LoadResults(SimulationResult result)
        {
            _result = result;
            PerformCustomLayout();
            _chartPanel1.Invalidate();
            _chartPanel2.Invalidate();
            _chartPanel3.Invalidate();
            _chartPanel4.Invalidate();
            _chartPanel5.Invalidate();
            _chartPanel6.Invalidate();
        }

        // ── Chart rendering ────────────────────────────────────────────────────

        private Rectangle GetPlotArea(Panel panel)
        {
            return new Rectangle(55, 48, Math.Max(50, panel.Width - 80), Math.Max(50, panel.Height - 105));
        }

        private void PaintQueueLengthChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            var area = GetPlotArea(panel);

            if (_result == null || _result.QueueLengthOverTime.Count < 2)
            {
                DrawEmptyState(g, area, "Run a simulation to generate Queue Length data.");
                return;
            }

            DrawLineChart(g, area,
                _result.QueueLengthOverTime.Select(p => (p.Time, (double)p.QueueLength)).ToList(),
                Color.FromArgb(220, 38, 38), "Time (hours)", "Queue Length (Lq)");
        }

        private void PaintSystemSizeChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            var area = GetPlotArea(panel);

            if (_result == null || _result.SystemSizeOverTime.Count < 2)
            {
                DrawEmptyState(g, area, "Run a simulation to generate Customers in System data.");
                return;
            }

            DrawLineChart(g, area,
                _result.SystemSizeOverTime.Select(p => (p.Time, (double)p.SystemSize)).ToList(),
                Color.FromArgb(37, 99, 235), "Time (hours)", "Customers in System (L)");
        }

        private void PaintWaitingTimeChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = GetPlotArea(panel);

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                DrawEmptyState(g, area, "Run a simulation to generate Wait Time Distribution.");
                return;
            }

            var waitTimes = _result.AllCustomers
                .Where(c => c.Status == "Completed")
                .Select(c => c.WaitingTime * 60)
                .ToList();

            if (waitTimes.Count == 0)
            {
                DrawEmptyState(g, area, "No completed customers to calculate waiting times.");
                return;
            }

            double maxWait  = Math.Max(0.1, waitTimes.Max());
            int    numBins  = Math.Min(15, Math.Max(5, waitTimes.Count / 8));
            double binWidth = maxWait / numBins;

            int[] bins = new int[numBins];
            foreach (var w in waitTimes)
            {
                int bin = (int)(w / binWidth);
                if (bin >= numBins) bin = numBins - 1;
                if (bin >= 0) bins[bin]++;
            }

            int maxCount = Math.Max(1, bins.Max());

            DrawBackgroundGrid(g, area, maxCount, "Count");

            int barW = Math.Max(4, area.Width / numBins);
            for (int i = 0; i < numBins; i++)
            {
                int barH = (int)((double)bins[i] / maxCount * area.Height);
                int bx = area.X + i * barW;
                int by = area.Bottom - barH;

                using var brush = new SolidBrush(Color.FromArgb(180, 217, 119, 6));
                g.FillRectangle(brush, bx + 1, by, barW - 2, barH);
                using var outline = new Pen(Color.FromArgb(217, 119, 6), 1f);
                g.DrawRectangle(outline, bx + 1, by, barW - 2, barH);

                if (barH > 14)
                {
                    using var f = new Font("Segoe UI Semibold", 7.5f);
                    using var b = new SolidBrush(Color.FromArgb(30, 41, 59));
                    g.DrawString(bins[i].ToString(), f, b, new RectangleF(bx, by - 14, barW, 14),
                        new StringFormat { Alignment = StringAlignment.Center });
                }
            }

            DrawAxesAndLabels(g, area, "Wait Time (minutes)", "Customer Count");
        }

        private void PaintUtilizationChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = GetPlotArea(panel);

            if (_result == null || _result.ServerUtilizations.Length == 0)
            {
                DrawEmptyState(g, area, "Run a simulation to view Server Utilization.");
                return;
            }

            int n = _result.ServerUtilizations.Length;
            DrawBackgroundGrid(g, area, 100, "%");

            int barW   = Math.Min(70, Math.Max(20, area.Width / (n * 2)));
            int groupW = area.Width / n;

            var sfCenter = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming      = StringTrimming.EllipsisCharacter
            };

            for (int i = 0; i < n; i++)
            {
                double util = _result.ServerUtilizations[i];
                int barH = (int)(Math.Min(1.0, util) * area.Height);
                int bx   = area.X + i * groupW + (groupW - barW) / 2;
                int by   = area.Bottom - barH;

                Color barColor = util > 0.85 ? Color.FromArgb(220, 38, 38)
                               : util > 0.65 ? Color.FromArgb(217, 119, 6)
                               : Color.FromArgb(22, 163, 74);

                using (var brush = new SolidBrush(Color.FromArgb(200, barColor)))
                    g.FillRectangle(brush, bx, by, barW, barH);
                using (var outline = new Pen(barColor, 1.2f))
                    g.DrawRectangle(outline, bx, by, barW, barH);

                // Percentage text above bar
                using (var valFont = new Font("Segoe UI Bold", 8.5f))
                using (var valBrush = new SolidBrush(TextDark))
                {
                    g.DrawString($"{util * 100:F0}%", valFont, valBrush,
                        new RectangleF(bx - 10, Math.Max(area.Y - 2, by - 16), barW + 20, 16),
                        new StringFormat { Alignment = StringAlignment.Center });
                }

                // X-axis cashier label cleanly positioned below baseline with zero overlap
                using (var lblFont = new Font("Segoe UI Semibold", 8f))
                using (var lblBrush = new SolidBrush(TextMid))
                {
                    g.DrawString($"Cashier {i + 1:D2}", lblFont, lblBrush,
                        new RectangleF(bx - 15, area.Bottom + 6, barW + 30, 18), sfCenter);
                }
            }

            DrawAxesAndLabels(g, area, "Checkout Cashiers", "Utilization (%)");
        }

        private void PaintArrivalDepartureChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = GetPlotArea(panel);

            if (_result == null || _result.ArrivalDepartureOverTime.Count < 2)
            {
                DrawEmptyState(g, area, "Run a simulation to generate Arrivals vs Departures data.");
                return;
            }

            var data = _result.ArrivalDepartureOverTime;
            double maxTime = Math.Max(0.001, data.Max(d => d.Time));
            int maxVal = Math.Max(1, data.Max(d => Math.Max(d.Arrivals, d.Departures)));

            DrawBackgroundGrid(g, area, maxVal, "Count");

            var arrPoints = data.Select(d => new PointF(
                area.X + (float)(d.Time / maxTime * area.Width),
                area.Bottom - (float)((double)d.Arrivals / maxVal * area.Height)
            )).ToArray();

            var depPoints = data.Select(d => new PointF(
                area.X + (float)(d.Time / maxTime * area.Width),
                area.Bottom - (float)((double)d.Departures / maxVal * area.Height)
            )).ToArray();

            // Set clip region strictly inside area so line never leaks below area.Bottom
            var oldClip = g.Clip;
            g.SetClip(new Rectangle(area.X - 1, area.Y - 1, area.Width + 3, area.Height + 3));

            if (arrPoints.Length >= 2)
            {
                using var arrPen = new Pen(Color.FromArgb(22, 163, 74), 2f);
                g.DrawLines(arrPen, arrPoints);
            }
            if (depPoints.Length >= 2)
            {
                using var depPen = new Pen(Color.FromArgb(220, 38, 38), 2f);
                g.DrawLines(depPen, depPoints);
            }

            g.Clip = oldClip;

            // Dedicated Legend Box in top-left
            int legX = area.X + 10;
            int legY = area.Y + 8;
            var legBox = new Rectangle(legX, legY, 115, 38);
            using (var legBg = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                g.FillRectangle(legBg, legBox);
            using (var legPen = new Pen(Border, 1f))
                g.DrawRectangle(legPen, legBox);

            using var lf = new Font("Segoe UI Semibold", 8f);
            g.FillRectangle(new SolidBrush(Color.FromArgb(22, 163, 74)), legX + 8, legY + 8, 10, 10);
            g.DrawString("Arrivals", lf, new SolidBrush(TextDark), legX + 22, legY + 5);
            g.FillRectangle(new SolidBrush(Color.FromArgb(220, 38, 38)), legX + 8, legY + 22, 10, 10);
            g.DrawString("Departures", lf, new SolidBrush(TextDark), legX + 22, legY + 19);

            DrawAxesAndLabels(g, area, "Time (hours)", "Total Customers");
        }

        private void PaintGanttChart(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Plot area with 90px left margin for "Cashier 01" track labels
            var area = new Rectangle(90, 48, Math.Max(50, panel.Width - 115), Math.Max(50, panel.Height - 105));

            if (_result == null || _result.AllCustomers.Count == 0)
            {
                DrawEmptyState(g, area, "Run a simulation to view the Server Activity Gantt Chart.");
                return;
            }

            int numServers = Math.Max(1, _result.NumServers);
            double maxTime = _result.AllCustomers.Where(c => c.Status == "Completed" || c.DepartureTime > 0)
                                                 .Select(c => c.DepartureTime)
                                                 .DefaultIfEmpty(_result.SimulationTime)
                                                 .Max();
            if (maxTime <= 0) maxTime = Math.Max(1.0, _result.SimulationTime);

            // Draw vertical time grid lines and bottom timestamps
            DrawGanttBackgroundGrid(g, area, maxTime);

            int trackH = area.Height / numServers;

            Color[] barColors = new Color[]
            {
                Color.FromArgb(37, 99, 235),   // Blue
                Color.FromArgb(16, 185, 129),  // Emerald Green
                Color.FromArgb(124, 58, 237),  // Purple
                Color.FromArgb(217, 119, 6),   // Amber
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(14, 165, 233)   // Sky Blue
            };

            var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            for (int s = 1; s <= numServers; s++)
            {
                int trackY = area.Y + (s - 1) * trackH;
                int barY   = trackY + Math.Max(3, (trackH - 26) / 2);
                int barH   = Math.Min(26, trackH - 6);

                // 1. Cashier track label on left
                using (var lblFont = new Font("Segoe UI Semibold", 8.5f))
                using (var lblBrush = new SolidBrush(TextDark))
                {
                    g.DrawString($"Cashier {s:D2}", lblFont, lblBrush,
                        new RectangleF(area.X - 85, trackY, 78, trackH), sfRight);
                }

                // 2. Idle background strip (Light Gray Strip)
                var trackRect = new Rectangle(area.X, barY, area.Width, barH);
                using (var idleBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    using var path = RoundPath(trackRect, 4);
                    g.FillPath(idleBrush, path);
                }
                using (var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    using var path = RoundPath(trackRect, 4);
                    g.DrawPath(borderPen, path);
                }

                // 3. Busy Service Blocks for Customers served by this cashier
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

                    float bx1 = area.X + (float)(startT / maxTime * area.Width);
                    float bx2 = area.X + (float)(endT / maxTime * area.Width);
                    float bw  = Math.Max(2f, bx2 - bx1);

                    var blockRect = new RectangleF(bx1, barY, bw, barH);

                    Color color = barColors[(c.Id - 1) % barColors.Length];

                    using (var blockBrush = new SolidBrush(Color.FromArgb(210, color)))
                    {
                        if (bw > 6)
                        {
                            using var path = RoundPath(Rectangle.Round(blockRect), 3);
                            g.FillPath(blockBrush, path);
                        }
                        else
                        {
                            g.FillRectangle(blockBrush, blockRect);
                        }
                    }

                    using (var borderPen = new Pen(color, 1f))
                    {
                        if (bw > 6)
                        {
                            using var path = RoundPath(Rectangle.Round(blockRect), 3);
                            g.DrawPath(borderPen, path);
                        }
                        else
                        {
                            g.DrawRectangle(borderPen, blockRect.X, blockRect.Y, blockRect.Width, blockRect.Height);
                        }
                    }

                    // Render Customer ID text inside block if width permits
                    if (bw >= 32)
                    {
                        using var txtFont = new Font("Segoe UI Bold", 7.5f);
                        using var txtBrush = new SolidBrush(Color.White);
                        g.DrawString($"C{c.Id:D3}", txtFont, txtBrush, blockRect, sfCenter);
                    }
                }
            }

            // Legend (Busy Service vs Idle Track)
            int legX = area.X + 10;
            int legY = area.Y + 6;
            var legBox = new Rectangle(legX, legY, 125, 24);
            using (var legBg = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                g.FillRectangle(legBg, legBox);
            using (var legPen = new Pen(Border, 1f))
                g.DrawRectangle(legPen, legBox);

            using var legFont = new Font("Segoe UI Semibold", 8f);
            g.FillRectangle(new SolidBrush(Color.FromArgb(37, 99, 235)), legX + 6, legY + 7, 10, 10);
            g.DrawString("Busy (Service)", legFont, new SolidBrush(TextDark), legX + 20, legY + 4);

            DrawAxesAndLabels(g, area, "Simulation Time (hours)", "Servers");
        }

        private void DrawGanttBackgroundGrid(Graphics g, Rectangle area, double maxTime)
        {
            using var gridPen = new Pen(GridLinePen, 1f) { DashStyle = DashStyle.Dash };
            using var font = new Font("Segoe UI", 7.5f);
            using var brush = new SolidBrush(TextLight);

            int numTicks = 5;
            for (int i = 0; i <= numTicks; i++)
            {
                float x = area.X + (float)i / numTicks * area.Width;
                g.DrawLine(gridPen, x, area.Y, x, area.Bottom);

                double t = (double)i / numTicks * maxTime;
                string timeStr = Customer.FormatTime(t);
                g.DrawString(timeStr, font, brush, new RectangleF(x - 30, area.Bottom + 4, 60, 16),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }

        private void DrawLineChart(Graphics g, Rectangle area, List<(double X, double Y)> data,
            Color lineColor, string xLabel, string yLabel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double maxX = Math.Max(0.001, data.Max(d => d.X));
            double maxY = Math.Max(1.0, data.Max(d => d.Y));

            DrawBackgroundGrid(g, area, maxY, yLabel);

            var points = data;
            if (points.Count > 500)
            {
                int step = points.Count / 500;
                points = points.Where((_, i) => i % step == 0).ToList();
            }

            var gdiPoints = points.Select(d => new PointF(
                area.X + (float)(d.X / maxX * area.Width),
                area.Bottom - (float)(d.Y / maxY * area.Height)
            )).ToArray();

            if (gdiPoints.Length >= 2)
            {
                var oldClip = g.Clip;
                g.SetClip(new Rectangle(area.X - 1, area.Y - 1, area.Width + 3, area.Height + 3));

                var fillPoints = new List<PointF>(gdiPoints);
                fillPoints.Add(new PointF(gdiPoints.Last().X, area.Bottom));
                fillPoints.Add(new PointF(gdiPoints.First().X, area.Bottom));

                using (var fillBrush = new SolidBrush(Color.FromArgb(30, lineColor)))
                    g.FillPolygon(fillBrush, fillPoints.ToArray());

                using (var linePen = new Pen(lineColor, 2f))
                    g.DrawLines(linePen, gdiPoints);

                g.Clip = oldClip;
            }

            DrawAxesAndLabels(g, area, xLabel, yLabel);
        }

        private void DrawBackgroundGrid(Graphics g, Rectangle area, double maxY, string yLabel)
        {
            using var gridPen = new Pen(GridLinePen, 1f) { DashStyle = DashStyle.Dash };
            using var font = new Font("Segoe UI", 7.5f);
            using var brush = new SolidBrush(TextLight);

            int numTicks = 4;
            for (int i = 0; i <= numTicks; i++)
            {
                float y = area.Bottom - (float)i / numTicks * area.Height;
                g.DrawLine(gridPen, area.X, y, area.Right, y);

                double val = (double)i / numTicks * maxY;
                string valStr = maxY >= 10 ? $"{val:F0}" : $"{val:F1}";
                g.DrawString(valStr, font, brush, new RectangleF(area.X - 48, y - 6, 42, 14),
                    new StringFormat { Alignment = StringAlignment.Far });
            }
        }

        private void DrawAxesAndLabels(Graphics g, Rectangle area, string xLabel, string yLabel)
        {
            // Axes Lines
            using var axisPen = new Pen(Border, 1.2f);
            g.DrawLine(axisPen, area.X, area.Bottom, area.Right, area.Bottom);
            g.DrawLine(axisPen, area.X, area.Y, area.X, area.Bottom);

            using var font = new Font("Segoe UI Semibold", 8.5f);
            using var brush = new SolidBrush(TextMid);

            // X-Axis Label
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(xLabel, font, brush, new RectangleF(area.X, area.Bottom + 26, area.Width, 20), sfCenter);

            // Y-Axis Rotated Label
            var state = g.Save();
            g.TranslateTransform(14, area.Y + area.Height / 2);
            g.RotateTransform(-90);
            g.DrawString(yLabel, font, brush, new RectangleF(-area.Height / 2, -10, area.Height, 20), sfCenter);
            g.Restore(state);
        }

        private void DrawEmptyState(Graphics g, Rectangle area, string message)
        {
            using var font = new Font("Segoe UI Semibold", 9f);
            using var brush = new SolidBrush(TextLight);
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
