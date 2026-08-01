using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Analytics panel with GDI+ custom-drawn charts for queue metrics.
    /// Redesigned with rounded chart panels, drop shadows, and better labels.
    /// All chart rendering logic is unchanged.
    /// </summary>
    public class AnalyticsPanel : UserControl
    {
        private SimulationResult? _result;
        private Panel _chartPanel1 = null!;
        private Panel _chartPanel2 = null!;
        private Panel _chartPanel3 = null!;
        private Panel _chartPanel4 = null!;
        private Panel _chartPanel5 = null!;

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg    = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color TextDark  = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid   = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);

        public AnalyticsPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            int y = 20;

            // ── Page title ─────────────────────────────────────────────────────
            Controls.Add(new Label
            {
                Text      = "📊  ANALYTICS & CHARTS",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(20, y),
                BackColor = Color.Transparent
            });
            y += 30;

            Controls.Add(new Label
            {
                Text      = "Visual analysis of simulation results — run a simulation first to populate charts.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(20, y),
                BackColor = Color.Transparent
            });
            y += 40;

            int chartW = 490;
            int chartH = 260;
            int gap    = 16;

            _chartPanel1 = CreateChartPanel("Queue Length vs Time",           20,          y, chartW, chartH);
            _chartPanel2 = CreateChartPanel("Customers in System vs Time",    chartW + gap + 20, y, chartW, chartH);
            _chartPanel1.Paint += PaintQueueLengthChart;
            _chartPanel2.Paint += PaintSystemSizeChart;
            y += chartH + gap;

            _chartPanel3 = CreateChartPanel("Waiting Time Distribution",      20,          y, chartW, chartH);
            _chartPanel4 = CreateChartPanel("Server Utilization",             chartW + gap + 20, y, chartW, chartH);
            _chartPanel3.Paint += PaintWaitingTimeChart;
            _chartPanel4.Paint += PaintUtilizationChart;
            y += chartH + gap;

            _chartPanel5 = CreateChartPanel("Cumulative Arrivals vs Departures", 20, y, chartW, chartH);
            _chartPanel5.Paint += PaintArrivalDepartureChart;
        }

        private Panel CreateChartPanel(string title, int x, int y, int w, int h)
        {
            var panel = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = CardBg
            };

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var r = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

                // Drop shadow
                using (var sb = new SolidBrush(Color.FromArgb(12, 0, 0, 0)))
                {
                    using var sp = RoundPath(new Rectangle(r.X + 2, r.Y + 2, r.Width, r.Height), 10);
                    g.FillPath(sb, sp);
                }

                // Card fill
                using (var bgb = new SolidBrush(CardBg))
                {
                    using var cp = RoundPath(r, 10);
                    g.FillPath(bgb, cp);
                }

                // Border
                using (var pen = new Pen(Border, 1f))
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
                g.DrawString(title, tf, tb, 14, 12);
            };

            Controls.Add(panel);
            return panel;
        }

        // ── Public API (unchanged) ─────────────────────────────────────────────

        public void LoadResults(SimulationResult result)
        {
            _result = result;
            _chartPanel1.Invalidate();
            _chartPanel2.Invalidate();
            _chartPanel3.Invalidate();
            _chartPanel4.Invalidate();
            _chartPanel5.Invalidate();
        }

        // ── Chart rendering (unchanged logic) ──────────────────────────────────

        private void PaintQueueLengthChart(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.QueueLengthOverTime.Count < 2) return;
            DrawLineChart(e.Graphics, (Panel)sender!,
                _result.QueueLengthOverTime.Select(p => (p.Time, (double)p.QueueLength)).ToList(),
                Color.FromArgb(220, 38, 38), "Time (h)", "Queue Length");
        }

        private void PaintSystemSizeChart(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.SystemSizeOverTime.Count < 2) return;
            DrawLineChart(e.Graphics, (Panel)sender!,
                _result.SystemSizeOverTime.Select(p => (p.Time, (double)p.SystemSize)).ToList(),
                Color.FromArgb(37, 99, 235), "Time (h)", "In System");
        }

        private void PaintWaitingTimeChart(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.AllCustomers.Count == 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = (Panel)sender!;
            var area  = new Rectangle(50, 50, panel.Width - 70, panel.Height - 70);

            var waitTimes = _result.AllCustomers
                .Where(c => c.Status == "Completed")
                .Select(c => c.WaitingTime * 60)
                .ToList();
            if (waitTimes.Count == 0) return;

            double maxWait  = waitTimes.Max();
            int    numBins  = Math.Min(20, Math.Max(5, waitTimes.Count / 10));
            double binWidth = maxWait / numBins;
            if (binWidth <= 0) binWidth = 1;

            int[] bins = new int[numBins];
            foreach (var w in waitTimes)
            {
                int bin = (int)(w / binWidth);
                if (bin >= numBins) bin = numBins - 1;
                if (bin >= 0) bins[bin]++;
            }

            int maxCount = bins.Max();
            if (maxCount == 0) return;

            int barW = area.Width / numBins;
            for (int i = 0; i < numBins; i++)
            {
                int barH = (int)((double)bins[i] / maxCount * area.Height);
                int bx = area.X + i * barW;
                int by = area.Bottom - barH;
                using var brush = new SolidBrush(Color.FromArgb(180, 217, 119, 6));
                g.FillRectangle(brush, bx + 1, by, barW - 2, barH);
                using var outline = new Pen(Color.FromArgb(217, 119, 6), 1f);
                g.DrawRectangle(outline, bx + 1, by, barW - 2, barH);
            }
            DrawAxes(g, area, "Wait Time (min)", "Count");
        }

        private void PaintUtilizationChart(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.ServerUtilizations.Length == 0) return;
            var g     = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = (Panel)sender!;
            var area  = new Rectangle(50, 50, panel.Width - 70, panel.Height - 70);

            int    n        = _result.ServerUtilizations.Length;
            int    barW     = Math.Min(60, area.Width / n);
            int    totalW   = barW * n;
            int    startX   = area.X + (area.Width - totalW) / 2;

            var sfCenter = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            };

            for (int i = 0; i < n; i++)
            {
                double util = _result.ServerUtilizations[i];
                int    barH = (int)(util * area.Height);
                int    bx   = startX + i * barW;
                int    by   = area.Bottom - barH;

                Color barColor = util > 0.85 ? Color.FromArgb(220, 38, 38)
                               : util > 0.65 ? Color.FromArgb(217, 119, 6)
                               : Color.FromArgb(22, 163, 74);

                using (var brush = new SolidBrush(Color.FromArgb(200, barColor)))
                    g.FillRectangle(brush, bx + 3, by, barW - 6, barH);
                using (var outline = new Pen(barColor, 1f))
                    g.DrawRectangle(outline, bx + 3, by, barW - 6, barH);

                using var f = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                using var b = new SolidBrush(TextLight);
                g.DrawString($"C{i + 1}", f, b, new RectangleF(bx, area.Bottom + 4, barW, 16), sfCenter);
                g.DrawString($"{util * 100:F0}%", f, b, new RectangleF(bx - 10, Math.Max(area.Y, by - 16), barW + 20, 16), sfCenter);
            }
            DrawAxes(g, area, "Cashier", "Utilization");
        }

        private void PaintArrivalDepartureChart(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.ArrivalDepartureOverTime.Count < 2) return;
            var g     = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = (Panel)sender!;
            var area  = new Rectangle(50, 50, panel.Width - 70, panel.Height - 70);

            var    data    = _result.ArrivalDepartureOverTime;
            double maxTime = data.Max(d => d.Time);
            int    maxVal  = data.Max(d => Math.Max(d.Arrivals, d.Departures));
            if (maxTime <= 0 || maxVal <= 0) return;

            var arrPoints = data.Select(d => new PointF(
                area.X + (float)(d.Time / maxTime * area.Width),
                area.Bottom - (float)((double)d.Arrivals / maxVal * area.Height)
            )).ToArray();

            var depPoints = data.Select(d => new PointF(
                area.X + (float)(d.Time / maxTime * area.Width),
                area.Bottom - (float)((double)d.Departures / maxVal * area.Height)
            )).ToArray();

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

            // Dedicated Legend Box container in top-right
            int legX = area.Right - 110;
            int legY = area.Y + 4;
            var legBox = new Rectangle(legX, legY, 105, 34);
            using (var legBg = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                g.FillRectangle(legBg, legBox);
            using (var legPen = new Pen(Border, 1f))
                g.DrawRectangle(legPen, legBox);

            using var lf = new Font("Segoe UI Semibold", 8f);
            g.FillRectangle(new SolidBrush(Color.FromArgb(22, 163, 74)), legX + 6, legY + 6, 10, 10);
            g.DrawString("Arrivals",   lf, new SolidBrush(TextDark), legX + 20, legY + 4);
            g.FillRectangle(new SolidBrush(Color.FromArgb(220, 38, 38)), legX + 6, legY + 19, 10, 10);
            g.DrawString("Departures", lf, new SolidBrush(TextDark), legX + 20, legY + 17);

            DrawAxes(g, area, "Time (h)", "Count");
        }

        private void DrawLineChart(Graphics g, Panel panel, List<(double X, double Y)> data,
            Color lineColor, string xLabel, string yLabel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var area = new Rectangle(50, 50, panel.Width - 70, panel.Height - 70);

            double maxX = data.Max(d => d.X); if (maxX <= 0) maxX = 1;
            double maxY = data.Max(d => d.Y); if (maxY <= 0) maxY = 1;

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
                var fillPoints = new List<PointF>(gdiPoints);
                fillPoints.Add(new PointF(gdiPoints.Last().X, area.Bottom));
                fillPoints.Add(new PointF(gdiPoints.First().X, area.Bottom));
                using var fillBrush = new SolidBrush(Color.FromArgb(25, lineColor));
                g.FillPolygon(fillBrush, fillPoints.ToArray());

                using var linePen = new Pen(lineColor, 2f);
                g.DrawLines(linePen, gdiPoints);
            }
            DrawAxes(g, area, xLabel, yLabel);
        }

        private void DrawAxes(Graphics g, Rectangle area, string xLabel, string yLabel)
        {
            using var axisPen = new Pen(Border, 1f);
            g.DrawLine(axisPen, area.X, area.Bottom, area.Right, area.Bottom);
            g.DrawLine(axisPen, area.X, area.Y, area.X, area.Bottom);

            using var font  = new Font("Segoe UI", 7.5f);
            using var brush = new SolidBrush(TextLight);
            g.DrawString(xLabel, font, brush, area.X + area.Width / 2 - 20, area.Bottom + 8);
            var state = g.Save();
            g.TranslateTransform(area.X - 32, area.Y + area.Height / 2 + 20);
            g.RotateTransform(-90);
            g.DrawString(yLabel, font, brush, 0, 0);
            g.Restore(state);
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
