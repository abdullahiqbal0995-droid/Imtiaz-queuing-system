using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Reports;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// About panel with educational content, formula display, and model explanations.
    /// Redesigned with rounded cards, code box for formulas, proper spacing throughout.
    /// </summary>
    public class AboutPanel : UserControl
    {
        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg    = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color CodeBg    = Color.FromArgb(248, 250, 252);
        private static readonly Color TextDark  = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid   = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight = Color.FromArgb(71, 85, 105);   // Slate 600 (High Contrast)
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);
        private static readonly Color AccentRed = Color.FromArgb(220, 38, 38);

        public AboutPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            int y = 20;

            // ── Page title ─────────────────────────────────────────────────────
            AddControl(MakeLbl("ℹ  ABOUT & EDUCATIONAL CONTENT",
                new Font("Segoe UI", 14f, FontStyle.Bold), TextDark, 20, y));
            y += 30;
            AddControl(MakeLbl("Reference material for queueing theory concepts used in this simulation.",
                new Font("Segoe UI", 9f), TextLight, 20, y));
            y += 40;

            // ── App info card ──────────────────────────────────────────────────
            var appCard = MakeCard(20, y, 720, 88);
            AddCardTitle(appCard, "🛒  IMTIAZ QUEUE ANALYZER", 16, 14);
            appCard.Controls.Add(MakeLbl(
                "A comprehensive supermarket checkout queueing simulation system.\n" +
                "Built for university-level Queueing Theory / Operations Research projects.",
                new Font("Segoe UI", 9.5f), TextMid, 16, 40));
            y += 100;

            // ── Key metrics card ───────────────────────────────────────────────
            var metricsCard = MakeCard(20, y, 720, 230);
            AddCardTitle(metricsCard, "📐  KEY METRICS EXPLAINED", 16, 14);
            int my = 44;
            AddMetricRow(metricsCard, "Lq",
                "Average Queue Length — customers waiting, not yet being served.", ref my);
            AddMetricRow(metricsCard, "L",
                "Average System Size — customers in queue + those being served.", ref my);
            AddMetricRow(metricsCard, "Wq",
                "Average Waiting Time — time spent in queue before service begins.", ref my);
            AddMetricRow(metricsCard, "W",
                "Average System Time — total time in system (wait + service).", ref my);
            AddMetricRow(metricsCard, "ρ",
                "Server Utilization — fraction of time servers are busy. ρ = λ/(Nμ). Must be < 1.", ref my);
            y += 242;

            // ── Queueing model explanations ────────────────────────────────────
            var modelsCard = MakeCard(20, y, 720, 310);
            AddCardTitle(modelsCard, "📋  QUEUEING MODEL EXPLANATIONS", 16, 14);
            int mdy = 44;
            string[] models = { "M/M/1", "M/M/N", "M/G/1", "M/G/N", "G/G/1", "G/G/N" };
            foreach (var m in models)
            {
                var mLbl = new Label
                {
                    Text      = $"▸  {m}",
                    Font      = new Font("Segoe UI Semibold", 9.5f),
                    ForeColor = AccentRed,
                    AutoSize  = true,
                    Location  = new Point(16, mdy),
                    BackColor = Color.Transparent
                };
                modelsCard.Controls.Add(mLbl);

                var dLbl = new Label
                {
                    Text      = ReportGenerator.GetModelExplanation(m).Replace("\n", " "),
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = TextMid,
                    Location  = new Point(86, mdy + 1),
                    Size      = new Size(620, 34),
                    AutoSize  = false
                };
                modelsCard.Controls.Add(dLbl);
                mdy += 44;
            }
            y += 322;

            // ── Formulas card (monospace code box) ────────────────────────────
            var formulaCard = MakeCard(20, y, 720, 310);
            AddCardTitle(formulaCard, "📝  FORMULAS", 16, 14);

            // Code background box
            var codeBox = new Panel
            {
                Location  = new Point(12, 44),
                Size      = new Size(696, 254),
                BackColor = CodeBg
            };
            codeBox.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, codeBox.Width - 1, codeBox.Height - 1);
            };
            formulaCard.Controls.Add(codeBox);

            codeBox.Controls.Add(new Label
            {
                Text =
                    "M/M/1 FORMULAS:\n" +
                    "  ρ  = λ / μ\n" +
                    "  Lq = ρ² / (1 - ρ)        =  λ² / (μ(μ-λ))\n" +
                    "  L  = ρ / (1 - ρ)          =  λ / (μ-λ)\n" +
                    "  Wq = ρ / (μ(1-ρ))         =  λ / (μ(μ-λ))\n" +
                    "  W  = 1 / (μ - λ)\n\n" +
                    "M/G/1 (Pollaczek-Khinchine):\n" +
                    "  Wq = λ·E[S²] / (2(1-ρ))\n\n" +
                    "G/G/1 (Kingman's Approximation):\n" +
                    "  Wq ≈ (ρ/(1-ρ)) × ((Ca²+Cs²)/2) × E[S]\n" +
                    "  where  Ca = σA/E[A],   Cs = σS/E[S]\n\n" +
                    "LITTLE'S LAW (applies to all models):\n" +
                    "  L  = λ × W\n" +
                    "  Lq = λ × Wq",
                Font      = new Font("Consolas", 9.5f),
                ForeColor = TextDark,
                Location  = new Point(12, 10),
                Size      = new Size(670, 240)
            });
            y += 322;

            // ── Kendall's notation card ────────────────────────────────────────
            var notationCard = MakeCard(20, y, 720, 155);
            AddCardTitle(notationCard, "📖  KENDALL'S NOTATION", 16, 14);
            var notationLbl = new Label
            {
                Text =
                    "A / B / N   where:\n" +
                    "  A = Arrival process    (M = Markovian/Poisson,  G = General)\n" +
                    "  B = Service process    (M = Markovian/Exponential,  G = General)\n" +
                    "  N = Number of servers  (1 = single server,  N = multiple)\n\n" +
                    "'M' indicates memoryless (exponential) distributions.\n" +
                    "'G' indicates any general distribution (Uniform, Normal, etc.).",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextMid,
                Location  = new Point(16, 44),
                Size      = new Size(690, 105)
            };
            notationCard.Controls.Add(notationLbl);

            Panel[] allCards = { appCard, metricsCard, modelsCard, formulaCard, notationCard };

            Resize += (s, e) =>
            {
                int cardW = Math.Max(320, Width - 40);
                foreach (var c in allCards)
                {
                    c.Width = cardW;
                    foreach (Control child in c.Controls)
                    {
                        if (child is Panel p && p != codeBox) p.Width = cardW - 32;
                    }
                }
                codeBox.Width = cardW - 24;
                notationLbl.Width = cardW - 32;
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private Panel MakeCard(int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = CardBg
            };
            card.Paint += (s, e) =>
            {
                DrawRoundedBorder(e.Graphics,
                    new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };
            Controls.Add(card);
            return card;
        }

        private void AddCardTitle(Panel card, string text, int x, int y)
        {
            card.Controls.Add(new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI Semibold", 10f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(x, y),
                BackColor = Color.Transparent
            });

            // Thin divider under title
            var div = new Panel
            {
                Location  = new Point(x, y + 24),
                Size      = new Size(card.Width - x * 2, 1),
                BackColor = Border
            };
            card.Controls.Add(div);
        }

        private void AddMetricRow(Panel card, string symbol, string explanation, ref int y)
        {
            // Symbol badge
            var symLbl = new Label
            {
                Text      = symbol,
                Font      = new Font("Segoe UI Semibold", 10f),
                ForeColor = AccentRed,
                AutoSize  = true,
                Location  = new Point(16, y + 1),
                BackColor = Color.Transparent
            };
            card.Controls.Add(symLbl);

            // Explanation
            var expLbl = new Label
            {
                Text      = explanation,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextMid,
                Location  = new Point(46, y + 1),
                Size      = new Size(660, 26),
                BackColor = Color.Transparent
            };
            card.Controls.Add(expLbl);
            y += 34;
        }

        private Label MakeLbl(string text, Font font, Color color, int x, int y)
        {
            return new Label
            {
                Text      = text,
                Font      = font,
                ForeColor = color,
                AutoSize  = true,
                Location  = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        private void AddControl(Control c) => Controls.Add(c);

        private void DrawRoundedBorder(Graphics g, Rectangle r, int rad)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            using var pen = new Pen(Border, 1f);
            g.DrawPath(pen, path);
        }
    }
}
