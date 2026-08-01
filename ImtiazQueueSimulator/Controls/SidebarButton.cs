using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Controls
{
    /// <summary>
    /// Styled navigation button for the sidebar menu.
    /// 48px height, icon at left, text centered vertically.
    /// Features:
    ///   - Perfect icon and text vertical baseline alignment
    ///   - Bounded non-overlapping rendering with auto-ellipsis
    ///   - Smooth frame-interpolated hover/active transitions (color fade, indicator bar expansion, text slide)
    /// </summary>
    public class SidebarButton : UserControl
    {
        private string _icon = "▣";
        private string _text = "Menu Item";
        private bool _isActive = false;
        private bool _isHovered = false;

        // Smooth animation state (0.0 to 1.0)
        private float _hoverVal = 0.0f;
        private float _activeVal = 0.0f;
        private System.Windows.Forms.Timer? _animTimer;

        // ── Design tokens (High Contrast) ──────────────────────────────────────
        private static readonly Color ActiveBar     = Color.FromArgb(37, 99, 235);    // Blue 600
        private static readonly Color ActiveBg      = Color.FromArgb(40, 37, 99, 235);
        private static readonly Color HoverBg       = Color.FromArgb(20, 255, 255, 255);
        private static readonly Color TextActive    = Color.White;
        private static readonly Color TextInactive  = Color.FromArgb(203, 213, 225); // Slate 300 (High Contrast)
        private static readonly Color TextHover     = Color.White;
        private static readonly Color IconActive    = Color.FromArgb(96, 165, 250);   // Blue 400
        private static readonly Color IconInactive  = Color.FromArgb(148, 163, 184); // Slate 400 (High Contrast)

        public string Icon       { get => _icon;     set { _icon = value;   Invalidate(); } }
        public string ButtonText { get => _text;     set { _text = value;   Invalidate(); } }
        public bool   IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                EnsureTimer();
                Invalidate();
            }
        }

        public event EventHandler? ButtonClicked;

        public SidebarButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(240, 48);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        private void EnsureTimer()
        {
            if (_animTimer == null)
            {
                _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
                _animTimer.Tick += (s, e) =>
                {
                    bool stepNeeded = false;

                    float targetHover = _isHovered ? 1.0f : 0.0f;
                    if (Math.Abs(_hoverVal - targetHover) > 0.01f)
                    {
                        _hoverVal += (targetHover - _hoverVal) * 0.25f;
                        stepNeeded = true;
                    }
                    else _hoverVal = targetHover;

                    float targetActive = _isActive ? 1.0f : 0.0f;
                    if (Math.Abs(_activeVal - targetActive) > 0.01f)
                    {
                        _activeVal += (targetActive - _activeVal) * 0.25f;
                        stepNeeded = true;
                    }
                    else _activeVal = targetActive;

                    Invalidate();

                    if (!stepNeeded && _animTimer != null)
                    {
                        _animTimer.Stop();
                    }
                };
            }

            if (!_animTimer.Enabled)
                _animTimer.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            EnsureTimer();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            EnsureTimer();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            ButtonClicked?.Invoke(this, e);
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bgRect = new Rectangle(8, 2, Width - 16, Height - 4);

            // Interpolate background opacity
            int hoverAlpha = (int)(20 * _hoverVal);
            int activeAlpha = (int)(40 * _activeVal);

            if (activeAlpha > 0)
            {
                using var brush = new SolidBrush(Color.FromArgb(activeAlpha, 37, 99, 235));
                FillRoundedRect(g, brush, bgRect, 8);
            }
            else if (hoverAlpha > 0)
            {
                using var brush = new SolidBrush(Color.FromArgb(hoverAlpha, 255, 255, 255));
                FillRoundedRect(g, brush, bgRect, 8);
            }

            // Left active indicator bar with smooth height expansion
            if (_activeVal > 0.01f)
            {
                int barH = (int)((Height - 16) * _activeVal);
                int barY = (Height - barH) / 2;
                int barAlpha = (int)(255 * _activeVal);
                using var barBrush = new SolidBrush(Color.FromArgb(barAlpha, ActiveBar));
                g.FillRectangle(barBrush, 0, barY, 4, barH);
            }

            // Interpolate Colors
            Color iconColor = BlendColors(IconInactive, IconActive, Math.Max(_hoverVal * 0.5f, _activeVal));
            Color textColor = BlendColors(TextInactive, TextActive, Math.Max(_hoverVal * 0.7f, _activeVal));

            // Smooth text slide (+3px on hover/active)
            float slideOffset = (_hoverVal * 2.0f) + (_activeVal * 3.0f);

            // ── Icon Bounding Box Alignment ─────────────────────────────────────
            Font iconFont;
            try   { iconFont = new Font("Segoe UI Emoji", 12.5f); }
            catch { iconFont = new Font("Segoe UI Symbol", 12.5f); }

            var iconBounds = new RectangleF(10, 0, 32, Height);
            using (iconFont)
            using (var iconBrush = new SolidBrush(iconColor))
            {
                var sfIcon = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_icon, iconFont, iconBrush, iconBounds, sfIcon);
            }

            // ── Label Bounding Box Alignment ────────────────────────────────────
            FontStyle textStyle = _isActive ? FontStyle.Bold : FontStyle.Regular;
            using var font = new Font("Segoe UI", 9.5f, textStyle);
            using var labelBrush = new SolidBrush(textColor);

            var textBounds = new RectangleF(46 + slideOffset, 0, Math.Max(10, Width - 52 - slideOffset), Height);
            var sfText = new StringFormat
            {
                Alignment     = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            };
            g.DrawString(_text, font, labelBrush, textBounds, sfText);
        }

        private static Color BlendColors(Color from, Color to, float factor)
        {
            factor = Math.Max(0.0f, Math.Min(1.0f, factor));
            int r = (int)(from.R + (to.R - from.R) * factor);
            int g = (int)(from.G + (to.G - from.G) * factor);
            int b = (int)(from.B + (to.B - from.B) * factor);
            int a = (int)(from.A + (to.A - from.A) * factor);
            return Color.FromArgb(a, r, g, b);
        }

        private void FillRoundedRect(Graphics g, Brush b, Rectangle r, int rad)
        {
            using var p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            g.FillPath(b, p);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _animTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}

