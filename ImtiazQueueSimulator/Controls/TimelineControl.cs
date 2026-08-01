using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Modern vertical timeline control showing complete customer journey:
    /// Store Arrival → Queue Waiting → Service Start → Checkout Service → Departure.
    /// Features dynamic vertical spacing calculation based on container bounds to prevent overflow.
    /// </summary>
    public class TimelineControl : UserControl
    {
        private string _arrivalTime = "--:--:--";
        private string _serviceStartTime = "--:--:--";
        private string _departureTime = "--:--:--";
        private string _waitingDuration = "--";
        private string _serviceDuration = "--";
        private string _totalDuration = "--";
        private string _assignedServer = "Cashier 01";

        public string ArrivalTime { get => _arrivalTime; set { _arrivalTime = value; Invalidate(); } }
        public string ServiceStartTime { get => _serviceStartTime; set { _serviceStartTime = value; Invalidate(); } }
        public string DepartureTime { get => _departureTime; set { _departureTime = value; Invalidate(); } }
        public string WaitingDuration { get => _waitingDuration; set { _waitingDuration = value; Invalidate(); } }
        public string ServiceDuration { get => _serviceDuration; set { _serviceDuration = value; Invalidate(); } }
        public string TotalDuration { get => _totalDuration; set { _totalDuration = value; Invalidate(); } }
        public string AssignedServer { get => _assignedServer; set { _assignedServer = value; Invalidate(); } }

        public TimelineControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(500, 560);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int marginX = 24;
            int y = 20;

            // ── Section Title ──────────────────────────────────────────────────
            using (var tf = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Color.FromArgb(30, 41, 59)))
                g.DrawString("CUSTOMER JOURNEY", tf, tb, marginX, y);

            y += 28;
            using (var subFont = new Font("Segoe UI", 8.5f))
            using (var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                g.DrawString("Complete event timeline from store arrival to checkout departure", subFont, subBrush, marginX, y);

            y += 32;

            int cx = marginX + 20; // X center of timeline line

            // Dynamic vertical spacing math based on available container height
            int summaryH = 76;
            int availableH = Height - y - summaryH - 30;
            int spacing = Math.Max(75, availableH / 3);

            int node1Y = y;
            int node2Y = node1Y + spacing;
            int node3Y = node2Y + spacing;

            // ── NODE 1: ARRIVAL ───────────────────────────────────────────────
            DrawNode(g, cx, node1Y, Color.FromArgb(22, 163, 74), "STORE ARRIVAL", _arrivalTime, "Customer entered supermarket checkout area");

            // ── SEGMENT 1: WAITING ─────────────────────────────────────────────
            DrawSegment(g, cx, node1Y + 28, node2Y, Color.FromArgb(217, 119, 6), "Waiting in Queue", _waitingDuration, Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6));

            // ── NODE 2: SERVICE START ─────────────────────────────────────────
            DrawNode(g, cx, node2Y, Color.FromArgb(37, 99, 235), "SERVICE START", _serviceStartTime, $"Called to {_assignedServer} counter");

            // ── SEGMENT 2: SERVICE ────────────────────────────────────────────
            DrawSegment(g, cx, node2Y + 28, node3Y, Color.FromArgb(124, 58, 237), "Checkout Service", _serviceDuration, Color.FromArgb(243, 232, 255), Color.FromArgb(124, 58, 237));

            // ── NODE 3: DEPARTURE ─────────────────────────────────────────────
            DrawNode(g, cx, node3Y, Color.FromArgb(220, 38, 38), "CHECKOUT DEPARTURE", _departureTime, "Transaction completed & customer departed");

            // ── HIGHLIGHTED SUMMARY CARD: TOTAL SYSTEM TIME ───────────────────
            int summaryY = Math.Max(node3Y + 50, Height - summaryH - 24);
            var summaryRect = new Rectangle(marginX, summaryY, Width - (marginX * 2), summaryH);

            using (var path = CreateRoundedPath(summaryRect, 12))
            {
                using var bgBrush = new SolidBrush(Color.FromArgb(239, 246, 255)); // #EFF6FF
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(Color.FromArgb(191, 219, 254), 1.2f); // #BFDBFE
                g.DrawPath(borderPen, path);
            }

            using (var f = new Font("Segoe UI Semibold", 8.5f))
            using (var b = new SolidBrush(Color.FromArgb(30, 64, 175))) // #1E40AF
                g.DrawString("⏱ TOTAL TIME IN SYSTEM (W)", f, b, summaryRect.X + 16, summaryRect.Y + 12);

            using (var f = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (var b = new SolidBrush(Color.FromArgb(30, 58, 138))) // #1E3A8A
                g.DrawString(_totalDuration, f, b, summaryRect.X + 16, summaryRect.Y + 34);
        }

        private void DrawNode(Graphics g, int cx, int y, Color color, string label, string time, string description)
        {
            // Outer halo circle (28px)
            using (var haloBrush = new SolidBrush(Color.FromArgb(35, color)))
                g.FillEllipse(haloBrush, cx - 14, y, 28, 28);

            // Inner solid circle (14px)
            using (var innerBrush = new SolidBrush(color))
                g.FillEllipse(innerBrush, cx - 7, y + 7, 14, 14);

            int textX = cx + 26;
            float textW = Math.Max(50, Width - textX - 24);
            var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };

            // Title & Timestamp line (Height 24px, shifted Y+2 to align vertically with node circle and avoid top text clipping)
            using (var f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var b = new SolidBrush(color))
                g.DrawString(label, f, b, new RectangleF(textX, y + 2, textW - 90, 24), sf);

            using (var f = new Font("Segoe UI Semibold", 9.5f))
            using (var b = new SolidBrush(Color.FromArgb(30, 41, 59)))
                g.DrawString(time, f, b, new RectangleF(Width - 130, y + 2, 100, 24), new StringFormat { Alignment = StringAlignment.Far });

            // Description (Placed at Y+26 with 20px height)
            using (var f = new Font("Segoe UI", 8.5f))
            using (var b = new SolidBrush(Color.FromArgb(100, 116, 139)))
                g.DrawString(description, f, b, new RectangleF(textX, y + 26, textW, 20), sf);
        }

        private void DrawSegment(Graphics g, int cx, int y1, int y2, Color color, string label, string duration, Color badgeBg, Color badgeFg)
        {
            // Dotted connecting line
            using (var pen = new Pen(color, 2.5f) { DashStyle = DashStyle.Dot })
                g.DrawLine(pen, cx, y1, cx, y2);

            // Pill badge on the right
            int midY = (y1 + y2) / 2;
            int textX = cx + 26;

            string badgeText = $"{label}: {duration}";
            using var font = new Font("Segoe UI Semibold", 8.25f);
            SizeF textSize = g.MeasureString(badgeText, font);

            int badgeW = (int)textSize.Width + 24;
            int badgeH = 26;
            var badgeRect = new Rectangle(textX, midY - (badgeH / 2), badgeW, badgeH);

            using (var path = CreateRoundedPath(badgeRect, 13))
            {
                using var bgB = new SolidBrush(badgeBg);
                g.FillPath(bgB, path);

                using var borderP = new Pen(badgeFg, 1.2f);
                g.DrawPath(borderP, path);
            }

            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textB = new SolidBrush(badgeFg);
            g.DrawString(badgeText, font, textB, badgeRect, sfCenter);
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
