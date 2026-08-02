using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Settings panel with preset scenarios and simulation configuration.
    /// Redesigned with fully responsive cards and dynamic auto-expanding info box.
    /// </summary>
    public class SettingsPanel : UserControl
    {
        public event Action<double, double, int>? OnPresetSelected;

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg    = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color TextDark  = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid   = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight = Color.FromArgb(100, 116, 139);
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);

        public SettingsPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            int y = 20;

            // ── Page Title ──
            var titleLabel = MakeLabel("⚙  SETTINGS & PRESETS", new Font("Segoe UI", 14f, FontStyle.Bold), TextDark, 20, y, true);
            Controls.Add(titleLabel);
            y += 32;

            // ── Subtitle ──
            var subTitleLabel = MakeLabel("Configure simulation presets for common Imtiaz supermarket scenarios.", new Font("Segoe UI", 9.5f), TextLight, 20, y, false);
            subTitleLabel.AutoSize = true;
            Controls.Add(subTitleLabel);
            y += 42;

            // ── Section Heading ──
            var sectionLabel = MakeLabel("IMTIAZ SCENARIO PRESETS", new Font("Segoe UI", 8.5f, FontStyle.Bold), TextLight, 20, y, true);
            Controls.Add(sectionLabel);
            y += 24;

            // ── Preset Cards Flow Layout ──
            var presetFlow = new FlowLayoutPanel
            {
                Location     = new Point(20, y),
                Size         = new Size(Math.Max(300, Width - 40), 220),
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

            // ── Info Section Heading ──
            var lblHow = MakeLabel("HOW PRESETS WORK", new Font("Segoe UI", 8.5f, FontStyle.Bold), TextLight, 20, y + 240, true);
            Controls.Add(lblHow);

            // ── Info Card (Auto-expanding) ──
            var noteCard = CreateInfoCard(20, lblHow.Bottom + 12, Math.Max(300, Width - 40), 160);
            
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
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            noteCard.Controls.Add(noteText);

            // ── Responsive Layout Logic ──
            Resize += (s, e) =>
            {
                int availW = Math.Max(300, Width - 40);
                
                titleLabel.Location = new Point(20, 20);
                subTitleLabel.Location = new Point(20, titleLabel.Bottom + 6);
                subTitleLabel.MaximumSize = new Size(availW, 0);
                
                sectionLabel.Location = new Point(20, subTitleLabel.Bottom + 24);
                
                presetFlow.Location = new Point(20, sectionLabel.Bottom + 12);
                presetFlow.Width = availW;

                // Adjust card sizes depending on container width (Desktop = 4 col, Tablet = 2 col, Phone = 1 col)
                int gap = 16;
                int numCols = 1;
                if (availW >= 920) numCols = 4;
                else if (availW >= 520) numCols = 2;
                else numCols = 1;

                int cardW = (availW - (numCols - 1) * gap) / numCols;
                foreach (Control card in presetFlow.Controls)
                {
                    card.Width = cardW;
                    card.Height = 210;
                }

                presetFlow.PerformLayout();

                lblHow.Location = new Point(20, presetFlow.Bottom + 28);
                noteCard.Location = new Point(20, lblHow.Bottom + 12);
                noteCard.Width = availW;

                // Make the info text auto-wrap and grow the card naturally
                noteText.MaximumSize = new Size(availW - 36, 0);
                noteCard.Height = noteText.Height + 36;

                AutoScrollMinSize = new Size(0, noteCard.Bottom + 30);
            };
        }

        // ── Preset card ────────────────────────────────────────────────────────

        private Panel CreatePresetCard(string title, string desc, string paramText,
            double lambda, double mu, int n, Color accentColor)
        {
            var card = new Panel
            {
                Size      = new Size(205, 210),
                BackColor = CardBg,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 16, 16)
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
            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = false,
                Location  = new Point(12, 18),
                BackColor = Color.Transparent
            };
            card.Controls.Add(titleLbl);

            // Description
            var descLbl = new Label
            {
                Text      = desc,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = TextLight,
                Location  = new Point(12, 44),
                BackColor = Color.Transparent,
                AutoSize  = false
            };
            card.Controls.Add(descLbl);

            // Params
            var paramLbl = new Label
            {
                Text      = paramText,
                Font      = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize  = false,
                Location  = new Point(12, 88),
                BackColor = Color.Transparent
            };
            card.Controls.Add(paramLbl);

            // ρ value
            var rhoLbl = new Label
            {
                Text      = $"ρ = {lambda / (n * mu):F3}",
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = TextMid,
                AutoSize  = false,
                Location  = new Point(12, 112),
                BackColor = Color.Transparent
            };
            card.Controls.Add(rhoLbl);

            // Apply button
            var applyBtn = new Button
            {
                Text      = "APPLY PRESET",
                Size      = new Size(181, 36),
                Location  = new Point(12, 154),
                FlatStyle = FlatStyle.Flat,
                BackColor = accentColor,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            applyBtn.FlatAppearance.BorderSize = 0;
            applyBtn.Click += (s, e) => OnPresetSelected?.Invoke(lambda, mu, n);
            card.Controls.Add(applyBtn);

            // Clicking card background also triggers select
            card.Click += (s, e) => OnPresetSelected?.Invoke(lambda, mu, n);

            // Responsive layout inside card
            card.Resize += (s, e) =>
            {
                int w = card.Width;
                titleLbl.Width = w - 24;
                descLbl.Width = w - 24;
                descLbl.Height = 38;
                paramLbl.Width = w - 24;
                rhoLbl.Width = w - 24;
                applyBtn.Width = w - 24;
                applyBtn.Location = new Point(12, card.Height - 48);
            };

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
