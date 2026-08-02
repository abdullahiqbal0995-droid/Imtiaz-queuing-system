using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Settings panel with preset scenarios and simulation configuration.
    /// Redesigned with rounded preset cards and consistent spacing.
    /// </summary>
    public class SettingsPanel : UserControl
    {
        public event Action<double, double, int>? OnPresetSelected;

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg    = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color TextDark  = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid   = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);

        public SettingsPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            int y = 20;

            // ── Page title ─────────────────────────────────────────────────────
            Controls.Add(MakeLabel("⚙  SETTINGS & PRESETS",
                new Font("Segoe UI", 14f, FontStyle.Bold), TextDark, 20, y, true));
            y += 30;

            Controls.Add(MakeLabel(
                "Configure simulation presets for common Imtiaz supermarket scenarios.",
                new Font("Segoe UI", 9f), TextLight, 20, y, true));
            y += 36;

            // ── Preset section heading ─────────────────────────────────────────
            Controls.Add(MakeLabel("IMTIAZ SCENARIO PRESETS",
                new Font("Segoe UI", 8.5f, FontStyle.Bold), TextLight, 20, y, true));
            y += 24;

            // ── Preset cards ───────────────────────────────────────────────────
            var presetFlow = new FlowLayoutPanel
            {
                Location     = new Point(20, y),
                Size         = new Size(Math.Max(300, Width - 40), 200),
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor    = Color.Transparent,
                Padding      = new Padding(0)
            };
            Controls.Add(presetFlow);

            presetFlow.Controls.Add(CreatePresetCard(
                "🕐  Normal Hours", "Low traffic — weekday afternoons",
                "λ = 10, μ = 12, N = 2", 10, 12, 2, Color.FromArgb(22, 163, 74)));

            presetFlow.Controls.Add(CreatePresetCard(
                "🔥  Peak Hours", "High traffic — evening rush hour",
                "λ = 20, μ = 12, N = 3", 20, 12, 3, Color.FromArgb(220, 38, 38)));

            presetFlow.Controls.Add(CreatePresetCard(
                "🛒  Weekend Rush", "Maximum traffic — weekend shoppers",
                "λ = 25, μ = 12, N = 4", 25, 12, 4, Color.FromArgb(217, 119, 6)));

            presetFlow.Controls.Add(CreatePresetCard(
                "⚡  Overloaded", "Arrival rate exceeds capacity",
                "λ = 30, μ = 12, N = 2", 30, 12, 2, Color.FromArgb(124, 58, 237)));

            y += 215;

            // ── Info card ──────────────────────────────────────────────────────
            var lblHow = MakeLabel("HOW PRESETS WORK",
                new Font("Segoe UI", 8.5f, FontStyle.Bold), TextLight, 20, y, true);
            Controls.Add(lblHow);
            y += 24;

            var noteCard = CreateInfoCard(20, y, Math.Max(300, Width - 40), 165);
            var noteText = new Label
            {
                Text =
                    "Each preset configures the arrival rate (λ), service rate (μ), and number of servers (N).\n\n" +
                    "  •  Increasing N reduces queue length and waiting time\n" +
                    "  •  Utilization  ρ = λ / (N × μ)  determines server load\n" +
                    "  •  When  ρ ≥ 1  the system becomes unstable (queue grows unbounded)\n" +
                    "  •  Click a preset card to load its parameters into the Simulation panel",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextMid,
                Location  = new Point(18, 18),
                Size      = new Size(Math.Max(260, noteCard.Width - 36), 135)
            };
            noteCard.Controls.Add(noteText);

            Resize += (s, e) =>
            {
                int cardW = Math.Max(300, Width - 40);
                presetFlow.Width = cardW;
                lblHow.Top = presetFlow.Bottom + 16;
                noteCard.Top = lblHow.Bottom + 8;
                noteCard.Width = cardW;
                noteText.Width = Math.Max(260, noteCard.Width - 36);
            };
        }

        // ── Preset card ────────────────────────────────────────────────────────

        private Panel CreatePresetCard(string title, string desc, string paramText,
            double lambda, double mu, int n, Color accentColor)
        {
            var card = new Panel
            {
                Size      = new Size(205, 185),
                BackColor = CardBg,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 12, 0)
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);

                // Rounded card border
                DrawRoundedRect(g, new Pen(Color.FromArgb(55, accentColor), 1.5f), r, 10);

                // Top accent strip
                using var stripBrush = new SolidBrush(accentColor);
                FillRoundedRectTop(g, stripBrush, r, 10);
            };

            // Title
            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(12, 18),
                BackColor = Color.Transparent
            });

            // Description
            card.Controls.Add(new Label
            {
                Text      = desc,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextLight,
                Location  = new Point(12, 40),
                Size      = new Size(180, 32),
                BackColor = Color.Transparent
            });

            // Params
            card.Controls.Add(new Label
            {
                Text      = paramText,
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = accentColor,
                AutoSize  = true,
                Location  = new Point(12, 78),
                BackColor = Color.Transparent
            });

            // ρ value
            card.Controls.Add(new Label
            {
                Text      = $"ρ = {lambda / (n * mu):F3}",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(12, 100),
                BackColor = Color.Transparent
            });

            // Apply button
            var applyBtn = new Button
            {
                Text      = "APPLY PRESET",
                Size      = new Size(181, 36),
                Location  = new Point(12, 135),
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9f),
                Cursor    = Cursors.Hand
            };
            applyBtn.FlatAppearance.BorderSize = 0;
            applyBtn.Click += (s, e) => OnPresetSelected?.Invoke(lambda, mu, n);
            card.Controls.Add(applyBtn);

            return card;
        }

        // ── Info card ──────────────────────────────────────────────────────────

        private Panel CreateInfoCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = CardBg
            };
            card.Paint += (s, e) =>
            {
                DrawRoundedRect(e.Graphics, new Pen(Border, 1f),
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };
            Controls.Add(card);
            return card;
        }

        // ── Label factory ──────────────────────────────────────────────────────

        private Label MakeLabel(string text, Font font, Color color, int x, int y, bool autoSize)
        {
            return new Label
            {
                Text      = text,
                Font      = font,
                ForeColor = color,
                AutoSize  = autoSize,
                Location  = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        // ── Drawing helpers ────────────────────────────────────────────────────

        private void DrawRoundedRect(Graphics g, Pen pen, Rectangle r, int rad)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundPath(r, rad);
            g.DrawPath(pen, path);
        }

        private void FillRoundedRectTop(Graphics g, Brush brush, Rectangle r, int rad)
        {
            // Only fill the top 5px as an accent strip
            g.FillRectangle(brush, r.X + 1, r.Y + 1, r.Width - 2, 5);
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
