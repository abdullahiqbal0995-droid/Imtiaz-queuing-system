using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Executive Server/Cashier Card (220×140).
    /// Features:
    ///   - Large readable cashier title (10.5pt Bold)
    ///   - Distinct status badge (● BUSY / ● AVAILABLE)
    ///   - Assigned customer display
    ///   - High contrast utilization progress bar + % badge
    ///   - Guaranteed zero text collision
    /// </summary>
    public class ServerCard : UserControl
    {
        private string _serverName   = "Cashier 01";
        private bool   _isBusy       = false;
        private string _customerName = "";
        private double _utilization  = 0.0;
        private string _serviceTime  = "";
        private bool   _isHovered    = false;

        public string ServerName   { get => _serverName;   set { _serverName   = value; Invalidate(); } }
        public bool   IsBusy       { get => _isBusy;       set { _isBusy       = value; Invalidate(); } }
        public string CustomerName { get => _customerName; set { _customerName = value; Invalidate(); } }
        public double Utilization  { get => _utilization;  set { _utilization  = value; Invalidate(); } }
        public string ServiceTime  { get => _serviceTime;  set { _serviceTime  = value; Invalidate(); } }

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color TextDark      = Color.FromArgb(15, 23, 42);    // Slate 900
        private static readonly Color TextMid       = Color.FromArgb(51, 65, 85);    // Slate 700
        private static readonly Color TextMuted     = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color BarBg         = Color.FromArgb(226, 232, 240); // Slate 200
        private static readonly Color SuccessGreen  = Color.FromArgb(16, 185, 129); // Emerald 500
        private static readonly Color DangerRed     = Color.FromArgb(239, 68, 68);   // Red 500
        private static readonly Color WarnAmber     = Color.FromArgb(217, 119, 6);

        // Fixed Row Offsets (Y)
        private const int Row0Y = 12;  // Dot + Cashier Name
        private const int Row1Y = 32;  // Status Badge
        private const int Row2Y = 50;  // Customer Name
        private const int Row3Y = 68;  // Service Time Info
        private const int Row4Y = 88;  // Utilization Bar
        private const int BarH  = 10;

        public ServerCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size      = new Size(260, 155);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(2, 2, Width - 5, Height - 5);

            // Shadow & Card Surface
            using (var sb = new SolidBrush(Color.FromArgb(12, 0, 0, 0)))
                FillRounded(g, sb, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height), 12);

            Color statusColor = _isBusy ? DangerRed : SuccessGreen;
            Color bg = _isHovered
                ? (_isBusy ? Color.FromArgb(254, 242, 242) : Color.FromArgb(240, 253, 244))
                : (_isBusy ? Color.FromArgb(255, 248, 248) : Color.FromArgb(248, 255, 250));

            using (var bgb = new SolidBrush(bg)) FillRounded(g, bgb, rect, 12);
            using (var bp  = new Pen(Color.FromArgb(80, statusColor), 1.2f)) DrawRounded(g, bp, rect, 12);

            // Top Accent Bar
            g.FillRectangle(new SolidBrush(Color.FromArgb(80, statusColor)),
                rect.X + 1, rect.Y + 1, rect.Width - 2, 5);

            int x        = rect.X + 14;
            int availW   = Math.Max(20, rect.Width - 28);
            int currentY = rect.Y + 12;
            var sfClip   = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            // ── Row 0: Status Dot + Server Name (10.5pt Bold) ─────────────────
            g.FillEllipse(new SolidBrush(statusColor), x, currentY + 4, 10, 10);
            using (var nf = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var nb = new SolidBrush(TextDark))
            {
                var nameSize = g.MeasureString(_serverName, nf, Math.Max(10, availW - 16));
                g.DrawString(_serverName, nf, nb,
                    new RectangleF(x + 16, currentY, Math.Max(10, availW - 16), nameSize.Height + 2), sfClip);
                currentY += (int)Math.Ceiling(nameSize.Height) + 3;
            }

            // ── Row 1: Status Badge (● BUSY / ● AVAILABLE) ────────────────────
            string statusText = _isBusy ? "● BUSY" : "● AVAILABLE";
            using (var stf = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var stb = new SolidBrush(statusColor))
            {
                var stSize = g.MeasureString(statusText, stf, availW);
                g.DrawString(statusText, stf, stb,
                    new RectangleF(x, currentY, availW, stSize.Height + 2), sfClip);
                currentY += (int)Math.Ceiling(stSize.Height) + 3;
            }

            // ── Row 2: Customer Name (8.5pt) ──────────────────────────────────
            string custDisplay = _isBusy && !string.IsNullOrEmpty(_customerName)
                ? $"Customer: {_customerName}"
                : "No customer currently";
            Color custColor = _isBusy && !string.IsNullOrEmpty(_customerName) ? TextMid : TextMuted;
            using (var cf = _isBusy ? new Font("Segoe UI Semibold", 8.5f) : new Font("Segoe UI", 8.5f, FontStyle.Italic))
            using (var cb = new SolidBrush(custColor))
            {
                var custSize = g.MeasureString(custDisplay, cf, availW);
                g.DrawString(custDisplay, cf, cb,
                    new RectangleF(x, currentY, availW, custSize.Height + 2), sfClip);
                currentY += (int)Math.Ceiling(custSize.Height) + 6;
            }

            // ── Row 3: Utilization Bar ────────────────────────────────────────
            int barH = 8;
            var barBgRect = new Rectangle(x, currentY, availW, barH);
            using (var bbb = new SolidBrush(BarBg)) FillRounded(g, bbb, barBgRect, 4);

            int fillW = (int)(availW * Math.Min(Math.Max(0, _utilization), 1.0));
            if (fillW > 4)
            {
                Color barFill = _utilization > 0.85 ? DangerRed
                              : _utilization > 0.65 ? WarnAmber
                              : SuccessGreen;
                using var fb = new LinearGradientBrush(barBgRect,
                    Color.FromArgb(200, barFill), barFill, 0f);
                FillRounded(g, fb, new Rectangle(x, currentY, fillW, barH), 4);
            }
            currentY += barH + 6;

            // ── Row 4: Utilization % Text ─────────────────────────────────────
            string utilText = $"Utilization: {_utilization * 100:F0}%";
            using (var uf = new Font("Segoe UI Semibold", 8.5f))
            using (var ub = new SolidBrush(TextMuted))
            {
                var utilSize = g.MeasureString(utilText, uf, availW);
                g.DrawString(utilText, uf, ub,
                    new RectangleF(x, currentY, availW, utilSize.Height + 2), sfClip);
            }
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
