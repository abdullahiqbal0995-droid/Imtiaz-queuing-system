using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Professional Metric Card (220×155).
    /// Features:
    ///   - Dedicated non-overlapping vertical zones:
    ///       Title Zone:    Y=10..42  (Line 1: 9pt Bold uppercase, Line 2: 8.5pt Bold symbol)
    ///       Value Zone:    Y=48..104 (26pt Bold number - 56px height ensures zero digit clipping)
    ///       Subtitle Zone: Y=112..138 (8.5pt Semibold unit label)
    ///   - Auto-scaling font size for multi-digit numbers (12.58, 100.00, etc.)
    ///   - Left accent pillar (5px wide)
    /// </summary>
    public class MetricCard : UserControl
    {
        private string _title       = "METRIC";
        private string _value       = "0.00";
        private string _subtitle    = "";
        private Color  _accentColor = Color.FromArgb(37, 99, 235);
        private bool   _isHovered   = false;

        public string Title       { get => _title;       set { _title       = value; Invalidate(); } }
        public string Value       { get => _value;       set { _value       = value; Invalidate(); } }
        public string Subtitle    { get => _subtitle;    set { _subtitle    = value; Invalidate(); } }
        public Color  AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color CardBg        = Color.White;
        private static readonly Color CardBgHover   = Color.FromArgb(250, 252, 255);
        private static readonly Color TextDark      = Color.FromArgb(15, 23, 42);    // Slate 900
        private static readonly Color TextMid       = Color.FromArgb(51, 65, 85);    // Slate 700
        private static readonly Color TextMuted     = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color BorderColor   = Color.FromArgb(226, 232, 240); // Slate 200

        public MetricCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size      = new Size(220, 155);
            BackColor = Color.Transparent;
            Cursor    = Cursors.Default;
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
            using (var sb = new SolidBrush(Color.FromArgb(10, 0, 0, 0)))
                FillRounded(g, sb, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height), 12);
            using (var bgb = new SolidBrush(_isHovered ? CardBgHover : CardBg))
                FillRounded(g, bgb, rect, 12);

            Color bColor = _isHovered ? Color.FromArgb(180, _accentColor) : BorderColor;
            using (var pen = new Pen(bColor, _isHovered ? 1.5f : 1f))
                DrawRounded(g, pen, rect, 12);

            // Left Accent Pillar (5px wide)
            using (var barBrush = new LinearGradientBrush(
                new Rectangle(rect.X, rect.Y + 10, 5, Math.Max(10, rect.Height - 20)),
                _accentColor, Color.FromArgb(140, _accentColor), 90f))
            {
                FillRoundedLeft(g, barBrush, new Rectangle(rect.X, rect.Y, 5, rect.Height), 12);
            }

            int contentX = rect.X + 16;
            int availW   = Math.Max(20, rect.Width - 24);
            int currentY = rect.Y + 14;

            // ── ZONE 1: TITLE & FORMULA ───────────────────────────────────────
            SplitTitle(_title, out string line1, out string line2);

            using (var titleFont  = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(TextMid))
            {
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                var titleSize = g.MeasureString(line1.ToUpper(), titleFont, availW);
                g.DrawString(line1.ToUpper(), titleFont, titleBrush,
                    new RectangleF(contentX, currentY, availW, titleSize.Height + 2), sf);
                currentY += (int)Math.Ceiling(titleSize.Height) + 2;
            }

            if (!string.IsNullOrEmpty(line2))
            {
                using var symFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var symBrush = new SolidBrush(_accentColor);
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                var formulaSize = g.MeasureString(line2, symFont, availW);
                g.DrawString(line2, symFont, symBrush,
                    new RectangleF(contentX, currentY, availW, formulaSize.Height + 2), sf);
                currentY += (int)Math.Ceiling(formulaSize.Height) + 6;
            }
            else
            {
                currentY += 4;
            }

            // ── ZONE 2: VALUE ─────────────────────────────────────────────────
            float fontPt = 28f;
            using (var tempFont = new Font("Segoe UI", fontPt, FontStyle.Bold))
            {
                var measured = g.MeasureString(_value, tempFont);
                while (measured.Width > availW && fontPt > 11f)
                {
                    fontPt -= 1.5f;
                    using var testFont = new Font("Segoe UI", fontPt, FontStyle.Bold);
                    measured = g.MeasureString(_value, testFont);
                }
            }

            using (var valueFont  = new Font("Segoe UI", fontPt, FontStyle.Bold))
            using (var valueBrush = new SolidBrush(TextDark))
            {
                var valSize = g.MeasureString(_value, valueFont, availW);
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming      = StringTrimming.EllipsisCharacter
                };
                g.DrawString(_value, valueFont, valueBrush,
                    new RectangleF(contentX, currentY, availW, valSize.Height + 2), sf);
                currentY += (int)Math.Ceiling(valSize.Height) + 6;
            }

            // ── ZONE 3: SUBTITLE / UNIT ───────────────────────────────────────
            if (!string.IsNullOrEmpty(_subtitle))
            {
                using var subFont  = new Font("Segoe UI Semibold", 8.5f);
                using var subBrush = new SolidBrush(TextMuted);
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                var subSize = g.MeasureString(_subtitle, subFont, availW);
                g.DrawString(_subtitle, subFont, subBrush,
                    new RectangleF(contentX, currentY, availW, subSize.Height + 2), sf);
            }
        }

        private static void SplitTitle(string title, out string line1, out string line2)
        {
            int paren = title.LastIndexOf('(');
            if (paren > 1 && title.TrimEnd().EndsWith(')'))
            {
                line1 = title.Substring(0, paren).Trim();
                line2 = title.Substring(paren).Trim();
            }
            else
            {
                line1 = title;
                line2 = "";
            }
        }

        private static void FillRounded(Graphics g, Brush b, Rectangle r, int rad)
        {
            using var p = RoundPath(r, rad); g.FillPath(b, p);
        }
        private static void DrawRounded(Graphics g, Pen pen, Rectangle r, int rad)
        {
            using var p = RoundPath(r, rad); g.DrawPath(pen, p);
        }
        private static void FillRoundedLeft(Graphics g, Brush b, Rectangle r, int rad)
        {
            using var p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddLine(r.X + r.Width, r.Y, r.X + r.Width, r.Bottom);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            g.FillPath(b, p);
        }
        private static GraphicsPath RoundPath(Rectangle r, int rad)
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
