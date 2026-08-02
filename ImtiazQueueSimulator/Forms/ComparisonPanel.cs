using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Simulation;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Queueing Models Performance Comparison Panel.
    /// Distinguishes analytical steady-state stability from finite simulation observations.
    /// </summary>
    public class ComparisonPanel : UserControl
    {
        private TextBox _txtLambda = null!;
        private TextBox _txtMu = null!;
        private NumericUpDown _nudServers = null!;
        private TextBox _txtSimTime = null!;
        private DataGridView _grid = null!;
        private Label _lblStatus = null!;
        private Label _lblWarning = null!;

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg      = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg      = Color.White;
        private static readonly Color TextDark    = Color.FromArgb(15, 23, 42);
        private static readonly Color TextMid     = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight   = Color.FromArgb(100, 116, 139);
        private static readonly Color Border      = Color.FromArgb(226, 232, 240);
        private static readonly Color AccentBlue  = Color.FromArgb(29, 78, 216);

        public ComparisonPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();

            // ── Page Title ──
            var title = new Label
            {
                Text      = "Queueing Models Performance Comparison",
                Font      = new Font("Segoe UI Bold", 14f),
                ForeColor = TextDark,
                AutoSize  = true,
                Location  = new Point(20, 16)
            };
            Controls.Add(title);

            // ── Subtitle ──
            var desc = new Label
            {
                Text      = "Analytical vs Simulation Results  |  Total Models Compared: 6",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(20, title.Bottom + 4)
            };
            desc.MaximumSize = new Size(Math.Max(300, Width - 40), 0);
            Controls.Add(desc);

            // ── Parameters Bar ──
            int paramY = desc.Bottom + 12;
            var paramPanel = new FlowLayoutPanel
            {
                Location     = new Point(20, paramY),
                Size         = new Size(Math.Max(300, Width - 40), 52),
                WrapContents = true,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor    = Color.White,
                Padding      = new Padding(16, 10, 16, 10),
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            paramPanel.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, paramPanel.Width - 1, paramPanel.Height - 1);
                using var path = RoundPath(r, 10);
                using var bg = new SolidBrush(Color.White);
                g.FillPath(bg, path);
                using var pen = new Pen(Border, 1f);
                g.DrawPath(pen, path);
            };
            Controls.Add(paramPanel);

            // Add fields to parameters bar
            paramPanel.Controls.Add(MakeLabel("λ:"));
            _txtLambda = MakeTextBox("20");
            paramPanel.Controls.Add(_txtLambda);

            paramPanel.Controls.Add(MakeLabel("μ:"));
            _txtMu = MakeTextBox("12");
            paramPanel.Controls.Add(_txtMu);

            paramPanel.Controls.Add(MakeLabel("N:"));
            _nudServers = new NumericUpDown
            {
                Value = 3, Minimum = 1, Maximum = 50,
                Size = new Size(60, 26),
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(0, 0, 16, 0)
            };
            paramPanel.Controls.Add(_nudServers);

            paramPanel.Controls.Add(MakeLabel("Sim Time (hrs):"));
            _txtSimTime = MakeTextBox("8");
            paramPanel.Controls.Add(_txtSimTime);

            var btnRun = new Button
            {
                Text      = "▶  RUN ALL MODELS",
                Size      = new Size(170, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235), // Primary Blue
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Bold", 9f),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(12, 0, 0, 0)
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += BtnRun_Click;
            paramPanel.Controls.Add(btnRun);

            // ── Warning Label ──
            _lblWarning = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38), // Warning Red
                AutoSize  = true,
                Location  = new Point(20, paramY + 60),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblWarning);

            // ── Status Label ──
            _lblStatus = new Label
            {
                Text      = "Ready to run comparisons.",
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = TextLight,
                AutoSize  = true,
                Location  = new Point(20, _lblWarning.Bottom + 6)
            };
            Controls.Add(_lblStatus);

            // ── Explanation Note ──
            var lblExplanation = new Label
            {
                Text      = "Analytical results represent steady-state queueing-theory predictions and require a stable system (ρ < 1). Simulation results are finite-horizon observations and may still be produced for unstable systems.",
                Font      = new Font("Segoe UI Italic", 9f),
                ForeColor = TextMid,
                AutoSize  = false,
                BackColor = Color.Transparent
            };
            Controls.Add(lblExplanation);

            // ── Grid Setup ──
            _grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 48 },
                GridColor = Border,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 52,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ShowCellToolTips = true,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Bold", 9.5f),
                    Padding = new Padding(16, 0, 16, 0)
                },
                DefaultCellStyle =
                {
                    ForeColor = Color.FromArgb(30, 41, 59),
                    SelectionBackColor = Color.FromArgb(238, 245, 255),
                    SelectionForeColor = Color.FromArgb(15, 23, 42),
                    Padding = new Padding(16, 0, 16, 0)
                },
                AlternatingRowsDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(248, 250, 252),
                    SelectionBackColor = Color.FromArgb(238, 245, 255),
                    SelectionForeColor = Color.FromArgb(15, 23, 42)
                },
                EnableHeadersVisualStyles = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Model", MinimumWidth = 110, Width = 110 },
                new DataGridViewTextBoxColumn { Name = "N", HeaderText = "N", MinimumWidth = 60, Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Evaluation Method", MinimumWidth = 200, Width = 210 },
                new DataGridViewTextBoxColumn { Name = "Stability", HeaderText = "Stability", MinimumWidth = 100, Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Rho", HeaderText = "ρ (Util)", MinimumWidth = 90, Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Lq", HeaderText = "Lq (Queue)", MinimumWidth = 100, Width = 115 },
                new DataGridViewTextBoxColumn { Name = "L", HeaderText = "L (System)", MinimumWidth = 100, Width = 115 },
                new DataGridViewTextBoxColumn { Name = "Wq", HeaderText = "Wq (min)", MinimumWidth = 100, Width = 120 },
                new DataGridViewTextBoxColumn { Name = "W", HeaderText = "W (min)", MinimumWidth = 100, Width = 120 }
            });

            // Set column alignments
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                if (col.Name == "Model" || col.Name == "Type")
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
                else if (col.Name == "N" || col.Name == "Stability")
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                else
                {
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            // Set Tooltips on headers
            _grid.Columns["Rho"].ToolTipText = "ρ (Utilization): Theoretical offered load for analytical rows, or observed cashier utilization for simulation rows.";
            _grid.Columns["Lq"].ToolTipText = "Lq (Queue size): Average number of customers waiting in queue.";
            _grid.Columns["L"].ToolTipText = "L (System size): Average number of customers in checkout system (queue + service).";
            _grid.Columns["Wq"].ToolTipText = "Wq (Wait time): Average time spent waiting in checkout queue.";
            _grid.Columns["W"].ToolTipText = "W (System time): Average total time spent in checkout area.";

            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.CellPainting += Grid_CellPainting;
            Controls.Add(_grid);

            // ── Responsive Layout updates ──
            Resize += (s, e) =>
            {
                int availW = Math.Max(300, Width - 40);
                desc.MaximumSize = new Size(availW, 0);
                paramPanel.Width = availW;
                
                _lblWarning.Location = new Point(20, paramPanel.Bottom + 8);
                _lblStatus.Location = new Point(20, _lblWarning.Bottom + 6);
                
                lblExplanation.Location = new Point(20, Height - 45);
                lblExplanation.Size = new Size(availW, 36);

                _grid.Location = new Point(20, _lblStatus.Bottom + 8);
                _grid.Size = new Size(availW, Math.Max(100, lblExplanation.Top - _lblStatus.Bottom - 18));
            };
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = _grid.Columns[e.ColumnIndex].Name;

            if (colName == "Model" || colName == "N" || colName == "Type" || colName == "Stability")
            {
                e.PaintBackground(e.CellBounds, true);

                if (e.Graphics == null) return;
                string valStr = e.Value?.ToString() ?? "";
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (colName == "Model")
                {
                    // Soft Blue Badge for Model Column
                    int badgeW = 90;
                    int badgeH = 26;
                    int bx = e.CellBounds.Left + 16;
                    int by = e.CellBounds.Top + (e.CellBounds.Height - badgeH) / 2;
                    var badgeR = new Rectangle(bx, by, badgeW, badgeH);

                    using var path = RoundPath(badgeR, 6);
                    using var bg = new SolidBrush(Color.FromArgb(239, 246, 255));
                    using var pen = new Pen(Color.FromArgb(191, 219, 254), 1f);
                    g.FillPath(bg, path);
                    g.DrawPath(pen, path);

                    using var font = new Font("Segoe UI Bold", 9f);
                    using var brush = new SolidBrush(Color.FromArgb(29, 78, 216));
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(valStr, font, brush, badgeR, sf);
                }
                else if (colName == "N")
                {
                    // Circular Badge for N Column
                    int size = 26;
                    int bx = e.CellBounds.Left + (e.CellBounds.Width - size) / 2;
                    int by = e.CellBounds.Top + (e.CellBounds.Height - size) / 2;
                    var circleR = new Rectangle(bx, by, size, size);

                    using var bg = new SolidBrush(Color.FromArgb(241, 245, 249));
                    using var pen = new Pen(Color.FromArgb(203, 213, 225), 1f);
                    g.FillEllipse(bg, circleR);
                    g.DrawEllipse(pen, circleR);

                    using var font = new Font("Segoe UI Bold", 9f);
                    using var brush = new SolidBrush(Color.FromArgb(30, 41, 59));
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(valStr, font, brush, circleR, sf);
                }
                else if (colName == "Type")
                {
                    // Status Badge for Evaluation Method
                    bool isSim = valStr.Contains("SIMULATION");
                    bool isUnstable = valStr.Contains("UNSTABLE");

                    Color bgClr, txtClr, bdrClr;
                    string badgeText;

                    if (isUnstable)
                    {
                        bgClr = Color.FromArgb(254, 242, 242);
                        txtClr = Color.FromArgb(220, 38, 38);
                        bdrClr = Color.FromArgb(254, 202, 202);
                        badgeText = "⚠ Analytical - Unstable";
                    }
                    else if (isSim)
                    {
                        bgClr = Color.FromArgb(220, 252, 231);
                        txtClr = Color.FromArgb(21, 128, 61);
                        bdrClr = Color.FromArgb(134, 239, 172);
                        badgeText = "⚡ Simulation";
                    }
                    else
                    {
                        bgClr = Color.FromArgb(219, 234, 254);
                        txtClr = Color.FromArgb(29, 78, 216);
                        bdrClr = Color.FromArgb(147, 197, 253);
                        badgeText = "📘 Analytical";
                    }

                    int badgeW = 150;
                    int badgeH = 26;
                    int bx = e.CellBounds.Left + 16;
                    int by = e.CellBounds.Top + (e.CellBounds.Height - badgeH) / 2;
                    var badgeR = new Rectangle(bx, by, badgeW, badgeH);

                    using var path = RoundPath(badgeR, 13);
                    using var bg = new SolidBrush(bgClr);
                    using var pen = new Pen(bdrClr, 1f);
                    g.FillPath(bg, path);
                    g.DrawPath(pen, path);

                    using var font = new Font("Segoe UI Semibold", 8.8f);
                    using var brush = new SolidBrush(txtClr);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(badgeText, font, brush, badgeR, sf);
                }
                else if (colName == "Stability")
                {
                    // Status Badge for Stability (Stable vs Unstable)
                    bool isStable = valStr.Contains("Stable") && !valStr.Contains("Unstable");
                    Color bgClr = isStable ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 242, 242);
                    Color txtClr = isStable ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
                    Color bdrClr = isStable ? Color.FromArgb(187, 247, 208) : Color.FromArgb(254, 202, 202);

                    int badgeW = 90;
                    int badgeH = 26;
                    int bx = e.CellBounds.Left + (e.CellBounds.Width - badgeW) / 2;
                    int by = e.CellBounds.Top + (e.CellBounds.Height - badgeH) / 2;
                    var badgeR = new Rectangle(bx, by, badgeW, badgeH);

                    using var path = RoundPath(badgeR, 13);
                    using var bg = new SolidBrush(bgClr);
                    using var pen = new Pen(bdrClr, 1f);
                    g.FillPath(bg, path);
                    g.DrawPath(pen, path);

                    using var font = new Font("Segoe UI Semibold", 8.8f);
                    using var brush = new SolidBrush(txtClr);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(valStr, font, brush, badgeR, sf);
                }

                // Light separator line
                using var penGrid = new Pen(Color.FromArgb(226, 232, 240), 1f);
                g.DrawLine(penGrid, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

                e.Handled = true;
            }
        }

        private static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void BtnRun_Click(object? sender, EventArgs e)
        {
            if (!double.TryParse(_txtLambda.Text, out double lambda) || lambda <= 0 ||
                !double.TryParse(_txtMu.Text, out double mu) || mu <= 0 ||
                !double.TryParse(_txtSimTime.Text, out double simTime) || simTime <= 0)
            {
                MessageBox.Show("Please enter valid positive numeric values.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int n = (int)_nudServers.Value;

            // Immediate Stability/Instability warning checks
            string warningText = "";
            if (lambda >= mu)
            {
                warningText += "⚠ M/M/1, M/G/1, G/G/1 are unstable because λ ≥ μ. ";
            }
            if (lambda >= n * mu)
            {
                warningText += $"⚠ M/M/N, M/G/N, G/G/N are unstable because λ ≥ Nμ (λ ≥ {n * mu}). ";
            }
            _lblWarning.Text = warningText;

            _grid.Rows.Clear();
            _lblStatus.Text = "Running comparisons across all 6 models...";
            Application.DoEvents();

            string[] models = { "M/M/1", "M/M/N", "M/G/1", "M/G/N", "G/G/1", "G/G/N" };

            foreach (var model in models)
            {
                int servers = model.Contains("/N") ? n : 1;

                // Analytical solving
                SimulationResult? analytical = model switch
                {
                    "M/M/1" => AnalyticalSolver.SolveMM1(lambda, mu),
                    "M/M/N" => AnalyticalSolver.SolveMMN(lambda, mu, servers),
                    "M/G/1" => AnalyticalSolver.SolveMG1(lambda, mu, "Exponential"),
                    "G/G/1" => AnalyticalSolver.SolveGG1(lambda, mu, "Exponential", "Exponential"),
                    _ => null
                };

                if (analytical != null)
                {
                    double theoreticalRho = model.Contains("/N") ? (lambda / (servers * mu)) : (lambda / mu);

                    if (theoreticalRho >= 1)
                    {
                        // Unstable row
                        _grid.Rows.Add(
                            model, servers,
                            "ANALYTICAL — UNSTABLE",
                            "⚠ Unstable",
                            FmtPct(theoreticalRho),
                            "Unstable",
                            "Unstable",
                            "Unstable",
                            "Unstable"
                        );
                    }
                    else
                    {
                        // Stable row
                        _grid.Rows.Add(
                            model, servers,
                            model == "G/G/1" ? "ANALYTICAL (Approx)" : "ANALYTICAL",
                            "✓ Stable",
                            FmtPct(analytical.AnalyticalRho),
                            FmtVal(analytical.AnalyticalLq),
                            FmtVal(analytical.AnalyticalL),
                            FmtMin(analytical.AnalyticalWq),
                            FmtMin(analytical.AnalyticalW)
                        );
                    }
                }

                // Simulation run
                var engine = new SimulationEngine
                {
                    Lambda = lambda, Mu = mu, NumServers = servers,
                    SimulationTime = simTime, ModelName = model,
                    ArrivalDistribution = "Exponential",
                    ServiceDistribution = "Exponential"
                };
                var simResult = engine.RunAll();

                double theoreticalSimRho = model.Contains("/N") ? (lambda / (servers * mu)) : (lambda / mu);
                string simStability = theoreticalSimRho >= 1 ? "⚠ Unstable" : "✓ Stable";

                _grid.Rows.Add(
                    model, servers, "SIMULATION",
                    simStability,
                    FmtPct(simResult.SimRho),
                    FmtVal(simResult.SimLq),
                    FmtVal(simResult.SimL),
                    FmtMin(simResult.SimWq),
                    FmtMin(simResult.SimW)
                );

                // Alternate background color per model pair for high-contrast zebra layout
                int lastIdx = _grid.Rows.Count - 1;
                if (lastIdx > 0 && _grid.Rows[lastIdx - 1].DefaultCellStyle.BackColor == Color.White)
                {
                    _grid.Rows[lastIdx - 1].DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                    _grid.Rows[lastIdx].DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                }
            }

            _lblStatus.Text = "Analytical vs Simulation Results  |  Total Models Compared: 6";
        }

        private static string FmtPct(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            return $"{v * 100:F2}%";
        }

        private static string FmtVal(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            return $"{v:F4}";
        }

        private static string FmtMin(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            return $"{v * 60:F2} min";
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize  = true,
            Margin    = new Padding(0, 5, 8, 0)
        };

        private static TextBox MakeTextBox(string def) => new TextBox
        {
            Text      = def,
            Size      = new Size(55, 26),
            Font      = new Font("Segoe UI", 9.5f),
            Margin    = new Padding(0, 0, 16, 0),
            BackColor = Color.FromArgb(248, 250, 252)
        };
    }
}
