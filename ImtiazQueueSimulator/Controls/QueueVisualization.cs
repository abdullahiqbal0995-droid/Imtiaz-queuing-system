using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Checkout Queue Visualization control.
    /// Dedicated to displaying waiting queue customers with clear position badges,
    /// avatar icons, customer IDs, and centered empty state.
    /// </summary>
    public class QueueVisualization : UserControl
    {
        private List<string> _waitingIds  = new();
        private int          _queueLength = 0;

        // Legacy compatibility property
        public struct ServerInfo
        {
            public string Name;
            public bool   IsBusy;
            public string CustomerName;
        }

        public void SetServers(List<ServerInfo> servers)
        {
            // Handled in ServerCard flow panel on Dashboard
        }

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color BgCard       = Color.White;
        private static readonly Color BorderCard   = Color.FromArgb(226, 232, 240);
        private static readonly Color TextDark     = Color.FromArgb(15, 23, 42);    // Slate 900
        private static readonly Color TextMid      = Color.FromArgb(51, 65, 85);    // Slate 700
        private static readonly Color TextMuted    = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color AccentBlue   = Color.FromArgb(37, 99, 235);
        private static readonly Color WarnAmber    = Color.FromArgb(217, 119, 6);
        private static readonly Color DangerRed    = Color.FromArgb(239, 68, 68);

        private const int CardH    = 76;  // Height of customer queue card
        private const int CardMinW = 64;  // Min width per card
        private const int CardMaxW = 80;  // Max width per card
        private const int CardGap  = 10;  // Gap between cards

        public int QueueLength { get => _queueLength; set { _queueLength = value; Invalidate(); } }

        public void SetWaitingCustomers(IEnumerable<string> ids)
        {
            _waitingIds  = ids.ToList();
            _queueLength = _waitingIds.Count;
            Invalidate();
        }

        public QueueVisualization()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size      = new Size(700, 180);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(2, 2, Width - 5, Height - 5);

            // Card Background & Shadow
            using (var sb = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
                FillRounded(g, sb, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height), 12);
            using (var bgb = new SolidBrush(BgCard)) FillRounded(g, bgb, rect, 12);
            using (var bp  = new Pen(BorderCard, 1f)) DrawRounded(g, bp, rect, 12);

            // ── Card Header Line ──────────────────────────────────────────────
            int titleY = rect.Y + 14;
            int titleW = 80;
            using (var tf = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var tb = new SolidBrush(TextDark))
                g.DrawString("QUEUE", tf, tb, rect.X + 18, titleY);

            string countText = _queueLength == 0
                ? "0 customers waiting"
                : $"● {_queueLength} Customer{(_queueLength != 1 ? "s" : "")} Waiting";
            Color countColor = _queueLength == 0 ? TextMuted : WarnAmber;

            using (var wf = new Font("Segoe UI Semibold", 9.5f))
            using (var wb = new SolidBrush(countColor))
            {
                var ws = g.MeasureString(countText, wf);
                float countX = Math.Max(rect.X + 18 + titleW, rect.Right - ws.Width - 18);
                float countAvailW = Math.Max(20, rect.Right - 18 - countX);
                var sfCount = new StringFormat
                {
                    Alignment     = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center,
                    Trimming      = StringTrimming.EllipsisCharacter
                };
                g.DrawString(countText, wf, wb,
                    new RectangleF(rect.X + 18 + titleW, titleY + 2, rect.Width - 36 - titleW, 20), sfCount);
            }

            // Divider under header
            int divY = titleY + 28;
            using (var dp = new Pen(Color.FromArgb(241, 245, 249), 1.5f))
                g.DrawLine(dp, rect.X + 16, divY, rect.Right - 16, divY);

            // ── Customer Cards Row / Empty State ──────────────────────────────
            int queueLeft  = rect.X + 16;
            int queueRight = rect.Right - 16;
            int queueW     = queueRight - queueLeft;
            int rowY       = divY + 16;

            if (_queueLength == 0)
            {
                DrawEmptyQueueState(g, rect, divY + 10, rect.Bottom - 10);
            }
            else
            {
                DrawCustomerCardsRow(g, queueLeft, queueW, rowY, CardH);
            }
        }

        private void DrawCustomerCardsRow(Graphics g, int left, int width, int rowY, int cardH)
        {
            int maxFit    = Math.Max(1, (width + CardGap) / (CardMinW + CardGap));
            int displayN  = Math.Min(_queueLength, maxFit);
            int overflow  = _queueLength - displayN;

            int badgeW     = overflow > 0 ? 52 : 0;
            int cardAvailW = width - badgeW - (overflow > 0 ? CardGap : 0);

            int totalCards    = displayN;
            int gapSpace      = Math.Max(0, totalCards - 1) * CardGap;
            int rawCardW      = totalCards > 0 ? (cardAvailW - gapSpace) / totalCards : CardMaxW;
            int cardW         = Math.Max(CardMinW, Math.Min(CardMaxW, rawCardW));

            // Connector dashed queue line
            int lineY = rowY + cardH / 2;
            using (var linePen = new Pen(Color.FromArgb(203, 213, 225), 2f) { DashStyle = DashStyle.Dot })
            {
                int lineEnd = left + displayN * (cardW + CardGap) - CardGap;
                if (overflow > 0) lineEnd += CardGap + badgeW;
                g.DrawLine(linePen, left, lineY, Math.Min(lineEnd, left + width), lineY);
            }

            for (int i = 0; i < displayN; i++)
            {
                int cx = left + i * (cardW + CardGap);
                string id = i < _waitingIds.Count ? _waitingIds[i] : $"#{i + 1:D3}";
                DrawSingleCustomerCard(g, cx, rowY, cardW, cardH, id, i + 1, WarnAmber);
            }

            if (overflow > 0)
            {
                int bx = left + displayN * (cardW + CardGap);
                var bRect = new Rectangle(bx, rowY + 14, badgeW, 48);
                using var bBrush = new SolidBrush(Color.FromArgb(254, 242, 242));
                FillRounded(g, bBrush, bRect, 8);
                using var bPen = new Pen(DangerRed, 1.2f);
                DrawRounded(g, bPen, bRect, 8);
                using var bf = new Font("Segoe UI", 10f, FontStyle.Bold);
                using var bb = new SolidBrush(DangerRed);
                var bs = g.MeasureString($"+{overflow}", bf);
                g.DrawString($"+{overflow}", bf, bb, bx + (badgeW - bs.Width) / 2, rowY + 28);
            }
        }

        private void DrawSingleCustomerCard(Graphics g, int x, int y, int w, int h,
            string idText, int position, Color themeColor)
        {
            var cardRect = new Rectangle(x, y, w, h);

            // Card Bg & Border
            using (var bgb = new SolidBrush(Color.FromArgb(35, themeColor))) FillRounded(g, bgb, cardRect, 8);
            using (var bp  = new Pen(Color.FromArgb(140, themeColor), 1.2f)) DrawRounded(g, bp, cardRect, 8);

            // Position pill badge "#1"
            using var pf = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var pb = new SolidBrush(themeColor);
            string posText = $"#{position}";
            var ps = g.MeasureString(posText, pf);
            g.DrawString(posText, pf, pb, x + (w - ps.Width) / 2, y + 4);

            // Avatar Icon Circle
            int iconR = 20;
            int iconX = x + (w - iconR) / 2;
            int iconY = y + 18;
            using (var iconBg = new SolidBrush(themeColor))
                g.FillEllipse(iconBg, iconX, iconY, iconR, iconR);
            using (var headBg = new SolidBrush(Color.White))
                g.FillEllipse(headBg, iconX + 5, iconY + 3, 10, 10);

            // ID Label (Clipped to card width)
            using var idf = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var idb = new SolidBrush(TextDark);
            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                Trimming      = StringTrimming.EllipsisCharacter
            };
            g.DrawString(idText, idf, idb, new RectangleF(x + 2, y + h - 20, w - 4, 18), sf);
        }

        private void DrawEmptyQueueState(Graphics g, Rectangle containerRect, int top, int bottom)
        {
            int midX   = containerRect.X + containerRect.Width / 2;
            int startY = top + 12;

            // 1. Empty Circle Icon (32x32)
            using (var cp = new Pen(Color.FromArgb(203, 213, 225), 2f))
                g.DrawEllipse(cp, midX - 16, startY, 32, 32);

            int titleY = startY + 38;

            // 2. QUEUE EMPTY title
            using var ef = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var eb = new SolidBrush(TextMuted);
            var es = g.MeasureString("QUEUE EMPTY", ef);
            g.DrawString("QUEUE EMPTY", ef, eb, midX - es.Width / 2, titleY);

            int subY = titleY + (int)Math.Ceiling(es.Height) + 4;

            // 3. Subtitle (No customers waiting in line)
            using var sf = new Font("Segoe UI", 9f);
            using var sb = new SolidBrush(TextMuted);
            var ss = g.MeasureString("No customers waiting in line", sf);
            g.DrawString("No customers waiting in line", sf, sb, midX - ss.Width / 2, subY);
        }

        private static void FillRounded(Graphics g, Brush b, Rectangle r, int rad)
        {
            using var p = RPath(r, rad); g.FillPath(b, p);
        }
        private static void DrawRounded(Graphics g, Pen pen, Rectangle r, int rad)
        {
            using var p = RPath(r, rad); g.DrawPath(pen, p);
        }
        private static GraphicsPath RPath(Rectangle r, int rad)
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
