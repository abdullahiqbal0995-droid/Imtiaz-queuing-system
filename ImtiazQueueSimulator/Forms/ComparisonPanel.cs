using System;
using System.Drawing;
using System.Windows.Forms;
using ImtiazQueueSimulator.Simulation;
using ImtiazQueueSimulator.Models;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Model comparison panel — runs all queueing models with identical parameters side-by-side.
    /// Features:
    ///   - Professional table formatting with perfect column header & cell alignments
    ///   - High contrast styling: Dark Navy header bar, soft zebra striping, soft blue row selection
    ///   - Badges for ANALYTICAL vs SIMULATION evaluation methods
    ///   - Number formatting (ρ = 55.6%, Lq/L = 4 decimals, Wq/W = 2 decimals in min)
    /// </summary>
    public class ComparisonPanel : UserControl
    {
        private TextBox _txtLambda = null!;
        private TextBox _txtMu = null!;
        private NumericUpDown _nudServers = null!;
        private TextBox _txtSimTime = null!;
        private DataGridView _grid = null!;
        private Label _lblStatus = null!;

        public ComparisonPanel()
        {
            BackColor = Color.FromArgb(244, 246, 250);
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            var title = new Label
            {
                Text      = "▤ MODEL COMPARISON",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(15, 12)
            };
            Controls.Add(title);

            var desc = new Label
            {
                Text      = "Compare all 6 queueing models side-by-side. Computes both analytical formulas and discrete-event simulation results.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(15, title.Bottom + 4)
            };
            Controls.Add(desc);

            // Parameter input bar
            int paramY = desc.Bottom + 10;
            var paramPanel = new FlowLayoutPanel
            {
                Location     = new Point(15, paramY),
                Size         = new Size(950, 48),
                WrapContents = false,
                BackColor    = Color.White,
                Padding      = new Padding(12, 10, 12, 8),
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            paramPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, paramPanel.Width - 1, paramPanel.Height - 1);
            };
            Controls.Add(paramPanel);

            int statusY = paramY + 56;

            _lblStatus = new Label
            {
                Text      = "Enter parameters and click RUN ALL MODELS to compare.",
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(15, statusY)
            };
            Controls.Add(_lblStatus);

            int gridY = statusY + 24;

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
                Size      = new Size(180, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9f),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(12, 0, 0, 0)
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += BtnRun_Click;
            paramPanel.Controls.Add(btnRun);

            // Results grid - High Contrast & Modern Row Selection
            _grid = new DataGridView
            {
                Location = new Point(15, gridY),
                Size = new Size(950, 420),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 38 },
                GridColor = Color.FromArgb(226, 232, 240),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(15, 23, 42),       // Dark Navy Slate 900
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Bold", 9.5f),
                    Padding = new Padding(8, 0, 8, 0)
                },
                DefaultCellStyle =
                {
                    ForeColor = Color.FromArgb(30, 41, 59),
                    SelectionBackColor = Color.FromArgb(239, 246, 255), // Soft Sky Blue Selection
                    SelectionForeColor = Color.FromArgb(15, 23, 42),
                    Padding = new Padding(8, 0, 8, 0)
                },
                AlternatingRowsDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(248, 250, 252),
                    SelectionBackColor = Color.FromArgb(239, 246, 255),
                    SelectionForeColor = Color.FromArgb(15, 23, 42)
                },
                EnableHeadersVisualStyles = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Setup columns with matched Header and Cell Alignments
            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "Model", HeaderText = "Model", Width = 90,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft, Font = new Font("Segoe UI Bold", 9.5f) }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "N", HeaderText = "N", Width = 50,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Type", HeaderText = "Evaluation Method", Width = 160,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleLeft } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Rho", HeaderText = "ρ (Util)", Width = 90,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Lq", HeaderText = "Lq (Queue)", Width = 110,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "L", HeaderText = "L (System)", Width = 110,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Wq", HeaderText = "Wq (min)", Width = 120,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "W", HeaderText = "W (min)", Width = 120,
                    HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleRight } },
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
                }
            });

            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Controls.Add(_grid);
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

            _grid.Rows.Clear();
            _lblStatus.Text = "Running comparisons across all 6 models...";
            Application.DoEvents();

            string[] models = { "M/M/1", "M/M/N", "M/G/1", "M/G/N", "G/G/1", "G/G/N" };

            foreach (var model in models)
            {
                int servers = model.Contains("/N") ? n : 1;

                // Analytical
                SimulationResult? analytical = model switch
                {
                    "M/M/1" => AnalyticalSolver.SolveMM1(lambda, mu),
                    "M/M/N" => AnalyticalSolver.SolveMMN(lambda, mu, servers),
                    "M/G/1" => AnalyticalSolver.SolveMG1(lambda, mu, "Exponential"),
                    "G/G/1" => AnalyticalSolver.SolveGG1(lambda, mu, "Exponential", "Exponential"),
                    _ => null
                };

                if (analytical != null && !double.IsNaN(analytical.AnalyticalLq))
                {
                    int idx = _grid.Rows.Add(
                        model, servers,
                        model == "G/G/1" ? "🏷 ANALYTICAL (Approx)" : "🏷 ANALYTICAL",
                        FmtPct(analytical.AnalyticalRho),
                        FmtVal(analytical.AnalyticalLq),
                        FmtVal(analytical.AnalyticalL),
                        FmtMin(analytical.AnalyticalWq),
                        FmtMin(analytical.AnalyticalW)
                    );
                    _grid.Rows[idx].Cells[2].Style.ForeColor = Color.FromArgb(29, 78, 216);
                    _grid.Rows[idx].Cells[2].Style.Font = new Font("Segoe UI Semibold", 8.5f);
                }

                // Simulation
                var engine = new SimulationEngine
                {
                    Lambda = lambda, Mu = mu, NumServers = servers,
                    SimulationTime = simTime, ModelName = model,
                    ArrivalDistribution = "Exponential",
                    ServiceDistribution = "Exponential"
                };
                var simResult = engine.RunAll();
                int simIdx = _grid.Rows.Add(
                    model, servers, "⚡ SIMULATION",
                    FmtPct(simResult.SimRho),
                    FmtVal(simResult.SimLq),
                    FmtVal(simResult.SimL),
                    FmtMin(simResult.SimWq),
                    FmtMin(simResult.SimW)
                );
                _grid.Rows[simIdx].Cells[2].Style.ForeColor = Color.FromArgb(4, 120, 87);
                _grid.Rows[simIdx].Cells[2].Style.Font = new Font("Segoe UI Semibold", 8.5f);

                // Alternate background color per model pair for high-contrast zebra layout
                int lastIdx = _grid.Rows.Count - 1;
                if (lastIdx > 0 && _grid.Rows[lastIdx - 1].DefaultCellStyle.BackColor == Color.White)
                {
                    _grid.Rows[lastIdx - 1].DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                    _grid.Rows[lastIdx].DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                }
            }

            _lblStatus.Text = $"Comparison complete — 6 models evaluation (analytical vs simulation).";
        }

        private static string FmtPct(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "—";
            return $"{v * 100:F1}%";
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
