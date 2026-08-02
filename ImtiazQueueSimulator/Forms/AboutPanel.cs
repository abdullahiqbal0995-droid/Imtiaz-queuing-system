using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ImtiazQueueSimulator.Reports;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Professional educational and reference dashboard for Queueing Theory.
    /// Redesigned to support a robust vertical flow, zero clipping/overlapping,
    /// auto-expanding cards, and responsive formatting.
    /// </summary>
    public class AboutPanel : UserControl
    {
        private FlowLayoutPanel _mainFlow = null!;
        private List<(Panel Card, FlowLayoutPanel ContentFlow)> _cards = new();

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg    = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg    = Color.White;
        private static readonly Color CodeBg    = Color.FromArgb(248, 250, 252);
        private static readonly Color TextDark  = Color.FromArgb(30, 41, 59);
        private static readonly Color TextMid   = Color.FromArgb(71, 85, 105);
        private static readonly Color TextLight = Color.FromArgb(100, 116, 139);
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);
        private static readonly Color AccentBlue = Color.FromArgb(29, 78, 216);

        public AboutPanel()
        {
            BackColor  = PageBg;
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            Controls.Clear();
            _cards.Clear();

            _mainFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Padding       = new Padding(20),
                Location      = new Point(0, 0)
            };
            Controls.Add(_mainFlow);

            // ── 1. Page Header ──
            var headerFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 0, 0, 16)
            };
            _mainFlow.Controls.Add(headerFlow);

            var titleLbl = new Label
            {
                Text      = "ℹ  ABOUT & EDUCATIONAL CONTENT",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 4)
            };
            headerFlow.Controls.Add(titleLbl);

            var subTitleLbl = new Label
            {
                Text      = "Queueing Theory, Simulation Models, Performance Metrics & How to Use the Simulator",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextLight,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 12)
            };
            headerFlow.Controls.Add(subTitleLbl);

            var introLbl = new Label
            {
                Text      = "Imtiaz Queue Analyzer is a supermarket checkout queue simulation system designed to demonstrate real-world queueing behavior using stochastic simulation and classical queueing models.",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = TextMid,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 8)
            };
            headerFlow.Controls.Add(introLbl);

            // ── 2. How to Use ──
            var usageCard = AddCard("📖  HOW TO USE THE SIMULATOR");
            usageCard.AddText("Follow these numbered steps to run the simulation and analyze its results:");
            usageCard.AddBullet("1. Open the 'Simulation' panel from the sidebar.");
            usageCard.AddBullet("2. Select the Queueing Model you want to simulate (e.g. M/M/1, M/M/N).");
            usageCard.AddBullet("3. Select the number of customers to generate.");
            usageCard.AddBullet("4. Set the arrival rate (λ) in customers per hour.");
            usageCard.AddBullet("5. Set the cashier service rate (μ) in customers per hour.");
            usageCard.AddBullet("6. Set the number of servers/cashiers (N) active in the supermarket.");
            usageCard.AddBullet("7. Configure the arrival and service distributions (e.g., Exponential, Uniform, Normal).");
            usageCard.AddBullet("8. Click the 'Start Simulation' button.");
            usageCard.AddBullet("9. Watch customers arrive and enter checkout queues in real time.");
            usageCard.AddBullet("10. Observe customer state flows from Queue → Server → Departure.");
            usageCard.AddBullet("11. Click any row in the 'Customer Records' panel to view that customer's exact journey.");
            usageCard.AddBullet("12. Switch to 'Queue History', 'Analytics', 'Model Comparison', or 'Reports' to view advanced outputs.");
            usageCard.AddText("Note: The simulator dynamically calculates all metrics in real time from simulated customer events. You do not need to manually calculate the queue.");

            // ── 3. Key Terminology ──
            var termsCard = AddCard("📐  KEY QUEUEING TERMS");
            termsCard.AddSubHeading("λ (Lambda) — Arrival Rate");
            termsCard.AddText("The average number of customers arriving at the checkout area per unit of time.");
            termsCard.AddSubHeading("μ (Mu) — Service Rate");
            termsCard.AddText("The average number of customers a single server/cashier can serve per unit of time.");
            termsCard.AddSubHeading("N — Parallel Servers");
            termsCard.AddText("The number of cashiers operating checkout lanes in parallel.");
            termsCard.AddSubHeading("ρ (Rho) — Server Utilization");
            termsCard.AddText("Traffic intensity or cashier workload. Represents the fraction of time servers are busy.");
            termsCard.AddFormula("For M/M/1:  ρ = λ / μ\nFor M/M/N: ρ = λ / (Nμ)");
            termsCard.AddSubHeading("Lq — Average Queue Length");
            termsCard.AddText("The average number of customers waiting in the checkout queue (excluding those being served).");
            termsCard.AddSubHeading("L — Average System Size");
            termsCard.AddText("The average number of customers in the entire checkout system (waiting in line + currently being served).");
            termsCard.AddSubHeading("Wq — Average Waiting Time");
            termsCard.AddText("The average time a customer spends waiting in queue before a cashier starts their checkout service.");
            termsCard.AddSubHeading("W — Average System Time");
            termsCard.AddText("The average total time a customer spends in the checkout area (waiting time + service duration).");
            termsCard.AddSubHeading("Little's Law");
            termsCard.AddText("A fundamental queueing relation that holds for almost all queueing systems in steady state:");
            termsCard.AddFormula("L  = λ × W\nLq = λ × Wq");
            termsCard.AddText("\"System\" means customers currently waiting in the queue PLUS customers currently receiving service.");

            // ── 4. Queueing Models (6 cards) ──
            var mm1Card = AddCard("1️⃣  M/M/1 — Markovian Arrivals, Markovian Service, 1 Server");
            mm1Card.AddText("Poisson arrival process, exponential service times, and a single cashier checkout counter.");
            mm1Card.AddFormula(
                "ρ  = λ / μ\n" +
                "P0 = 1 - ρ\n" +
                "Lq = ρ² / (1 - ρ) = λ² / [μ(μ - λ)]\n" +
                "L  = ρ / (1 - ρ) = λ / (μ - λ)\n" +
                "Wq = Lq / λ      = λ / [μ(μ - λ)]\n" +
                "W  = L / λ       = 1 / (μ - λ)"
            );
            mm1Card.AddText("Stability Condition: λ < μ (or ρ < 1). If ρ >= 1, the queue grows infinitely.");

            var mmnCard = AddCard("2️⃣  M/M/N — Markovian Arrivals, Markovian Service, N Servers");
            mmnCard.AddText("Poisson arrival process, exponential service times, and N parallel cashier counters.");
            mmnCard.AddFormula(
                "a = λ / μ\n" +
                "ρ = λ / (Nμ)\n" +
                "P0 = [ Σ_{k=0}^{N-1} (a^k / k!) + (a^N / N!) * 1/(1-ρ) ]⁻¹\n" +
                "P(wait) = (a^N / N!) * P0 / (1-ρ)    (Erlang-C Formula)\n" +
                "Lq = P(wait) * ρ / (1-ρ)\n" +
                "Wq = Lq / λ\n" +
                "W  = Wq + 1/μ\n" +
                "L  = λ * W"
            );
            mmnCard.AddText("Stability Condition: ρ < 1 (or λ < Nμ).");

            var mg1Card = AddCard("3️⃣  M/G/1 — Markovian Arrivals, General Service, 1 Server");
            mg1Card.AddText("Poisson arrival process, general service-time distribution (e.g. constant, normal), and one cashier. Uses the Pollaczek-Khinchine (P-K) formula.");
            mg1Card.AddFormula(
                "ρ  = λE[S]\n" +
                "Lq = λ²E[S²] / [2(1-ρ)]\n" +
                "Wq = Lq / λ\n" +
                "W  = Wq + E[S]\n" +
                "L  = λ * W"
            );
            mg1Card.AddText("Where E[S] = mean service time, and E[S²] = second moment of service time.\nThis model is useful when service time is NOT necessarily exponential (e.g. uniform or constant scan times).");

            var mgnCard = AddCard("4️⃣  M/G/N — Markovian Arrivals, General Service, N Servers");
            mgnCard.AddText("Poisson arrival process, general service-time distribution, and N parallel cashiers.");
            mgnCard.AddText("There is no single simple closed-form formula for the general M/G/N model.");
            mgnCard.AddText("Therefore, the simulator estimates Lq, L, Wq, W, and ρ using discrete-event simulation.");
            mgnCard.AddText("Simulation is especially useful for general multi-server systems where analytical formulas become difficult or mathematically intractable.");

            var gg1Card = AddCard("5️⃣  G/G/1 — General Arrivals, General Service, 1 Server");
            gg1Card.AddText("General inter-arrival distribution, general service-time distribution, and one cashier. Uses Kingman's heavy-traffic approximation.");
            gg1Card.AddFormula(
                "Wq ≈ [ρ / (1-ρ)] * [(Ca² + Cs²)/2] * E[S]\n" +
                "Lq ≈ λ * Wq\n" +
                "W  = Wq + E[S]\n" +
                "L  = λ * W"
            );
            gg1Card.AddText("Where Ca = coefficient of variation of inter-arrival times, Cs = coefficient of variation of service times, and ρ = λE[S].\nNote: This is an approximation rather than an exact closed-form result.");

            var ggnCard = AddCard("6️⃣  G/G/N — General Arrivals, General Service, N Servers");
            ggnCard.AddText("General arrival process, general service-time distribution, and N parallel cashier counters.");
            ggnCard.AddText("There is no simple universal closed-form formula. The simulator uses discrete-event simulation to estimate Lq, L, Wq, W, and ρ.");
            ggnCard.AddText("This is the most general model among the six models supported by this project.");

            // ── 5. Simulation Formulas ──
            var simFormulasCard = AddCard("📝  SIMULATION FORMULAS");
            simFormulasCard.AddSubHeading("ARRIVAL GENERATION");
            simFormulasCard.AddText("For exponential/Poisson arrivals, the inter-arrival time (IA) is generated using inverse transform sampling:");
            simFormulasCard.AddFormula("IA = -ln(U) / λ");
            simFormulasCard.AddText("where U is a uniformly distributed random number between 0 and 1.\nCustomer arrival time is calculated as:");
            simFormulasCard.AddFormula("ArrivalTime(i) = ArrivalTime(i-1) + IA(i)");
            simFormulasCard.AddText("A smaller inter-arrival time means customers arrive more frequently.");
            simFormulasCard.AddSubHeading("SERVICE TIME GENERATION");
            simFormulasCard.AddText("For exponential service, service duration is generated as:");
            simFormulasCard.AddFormula("ServiceTime = -ln(U) / μ");
            simFormulasCard.AddText("where U is a uniform random number in (0,1).\nFor general service distributions, service time is generated according to the selected distribution (e.g. Uniform, Normal) using its corresponding generation mechanism.");

            // ── 6. Individual Customer Metrics ──
            var customerMetricsCard = AddCard("👥  HOW CUSTOMER METRICS ARE CALCULATED");
            customerMetricsCard.AddText("Every customer event has its own timing details calculated using rounded-second precision:");
            customerMetricsCard.AddBullet("Arrival Time = time customer enters the supermarket checkout system");
            customerMetricsCard.AddBullet("Service Start Time = time customer actually begins checkout service with cashier");
            customerMetricsCard.AddBullet("Departure Time = time customer finishes checkout service and leaves");
            customerMetricsCard.AddSubHeading("Formulas");
            customerMetricsCard.AddFormula(
                "WaitingTime (Wq)  = Service Start Time - Arrival Time\n" +
                "Service Time      = Departure Time - Service Start Time\n" +
                "Time in System (W) = Departure Time - Arrival Time = Wq + Service Time"
            );
            customerMetricsCard.AddSubHeading("Numerical Example");
            customerMetricsCard.AddText("• Customer arrives at 00:10:00\n• Service starts at 00:12:30\n• Service ends at 00:16:00\n\nCalculations:\n  - Waiting Time (Wq) = 00:12:30 - 00:10:00 = 2 min 30 sec\n  - Service Time = 00:16:00 - 00:12:30 = 3 min 30 sec\n  - Total Time in System (W) = 00:16:00 - 00:10:00 = 6 min 00 sec (or W = 2m30s + 3m30s = 6m00s)");

            // ── 7. System State Metrics ──
            var stateMetricsCard = AddCard("📊  SYSTEM STATE ON ARRIVAL & SERVICE");
            stateMetricsCard.AddSubHeading("Queue on Arrival");
            stateMetricsCard.AddText("Number of customers already waiting in line immediately BEFORE the customer arrives.");
            stateMetricsCard.AddSubHeading("System on Arrival");
            stateMetricsCard.AddText("Number of customers in the system immediately BEFORE the customer arrives (System = Queue + Customers currently in service).");
            stateMetricsCard.AddSubHeading("Queue on Service Start");
            stateMetricsCard.AddText("Number of customers still waiting in line immediately BEFORE the customer's service begins.");
            stateMetricsCard.AddSubHeading("System on Service Start");
            stateMetricsCard.AddText("Number of customers in the system immediately BEFORE the customer's service begins.");
            stateMetricsCard.AddText("Note: The simulator's logs represent the system state immediately before the event occurs.");

            // ── 8. Aggregate Performance Metrics ──
            var aggMetricsCard = AddCard("📈  SIMULATION PERFORMANCE METRICS");
            aggMetricsCard.AddBullet("Average Queue Length (Lq): Average number of customers waiting in line.");
            aggMetricsCard.AddBullet("Average System Size (L): Average number of customers in the complete system.");
            aggMetricsCard.AddBullet("Average Waiting Time (Wq): Average time customers spend waiting in line.");
            aggMetricsCard.AddBullet("Average System Time (W): Average total time from arrival to departure.");
            aggMetricsCard.AddBullet("Server Utilization (ρ): Percentage/fraction of time cashiers are busy.");
            aggMetricsCard.AddBullet("Served Customers: Number of customers whose service has completed.");
            aggMetricsCard.AddText("These parameters are related via Little's Law:");
            aggMetricsCard.AddFormula("Lq = λ × Wq\nL = λ × W");
            aggMetricsCard.AddText("Note: Simulation results may differ from theoretical formulas because simulation uses random samples and finite simulation runs.");

            // ── 9. Theoretical vs Simulation Results ──
            var comparisonTheoryCard = AddCard("🔬  THEORETICAL vs SIMULATION RESULTS");
            comparisonTheoryCard.AddText("Theoretical values are calculated using asymptotic mathematical queueing formulas.");
            comparisonTheoryCard.AddText("Simulation values are computed from actual simulated customer events.");
            comparisonTheoryCard.AddText("They may differ due to initial transient effects, finite sample sizes, and random variations. As the simulation duration and customer count increase, simulation values converge to theoretical values.");

            // ── 10. Real-world Imtiaz Checkout Example ──
            var realWorldCard = AddCard("🛒  REAL-WORLD IMTIAZ CHECKOUT EXAMPLE");
            realWorldCard.AddText("Here is how supermarket checkout maps to queueing elements:");
            realWorldCard.AddBullet("Customer = shoppers queueing for checkout");
            realWorldCard.AddBullet("Checkout line = queue");
            realWorldCard.AddBullet("Cashier = server");
            realWorldCard.AddBullet("Arrival rate λ = shoppers arriving per unit time");
            realWorldCard.AddBullet("Service rate μ = shoppers served per cashier per unit time");
            realWorldCard.AddBullet("N = number of cashiers");
            realWorldCard.AddBullet("Wq = time shoppers wait in checkout line");
            realWorldCard.AddBullet("W = total time shoppers spend in checkout system");
            realWorldCard.AddBullet("ρ = cashier utilization");
            realWorldCard.AddSubHeading("Visual Flow");
            realWorldCard.AddControl(CreateVisualFlow());

            // ── 11. Model Comparison Table ──
            var tableCard = AddCard("▤  MODEL COMPARISON TABLE");
            tableCard.AddText("Overview of arrival/service distributions and analytical availability for the 6 models:");
            tableCard.AddControl(CreateModelTable());

            // ── 12. Important Assumptions ──
            var assumptionsCard = AddCard("⚠️  MODEL ASSUMPTIONS");
            assumptionsCard.AddBullet("Queue Discipline: First-Come, First-Served (FCFS/FIFO).");
            assumptionsCard.AddBullet("Parallel Cashiers: Multiple counters operate independently and in parallel.");
            assumptionsCard.AddBullet("Infinite Buffer: Customers do not balk (refuse to join) or reneg (leave queue).");
            assumptionsCard.AddBullet("Units consistency: All rates (λ, μ) must use the same time unit (hours).");
            assumptionsCard.AddBullet("System Stability: Analytical calculations require ρ < 1.");

            // ── 13. Glossary ──
            var glossaryCard = AddCard("📖  GLOSSARY OF SYMBOLS");
            glossaryCard.AddBullet("λ — Arrival rate");
            glossaryCard.AddBullet("μ — Service rate");
            glossaryCard.AddBullet("N — Number of servers");
            glossaryCard.AddBullet("ρ — Server utilization");
            glossaryCard.AddBullet("Lq — Average queue length");
            glossaryCard.AddBullet("L — Average system size");
            glossaryCard.AddBullet("Wq — Average waiting time in queue");
            glossaryCard.AddBullet("W — Average total system time");
            glossaryCard.AddBullet("S — Service time");
            glossaryCard.AddBullet("IA — Inter-arrival time");
            glossaryCard.AddBullet("U — Uniform random number (0, 1)");

            // ── Layout Update ──
            Resize += (s, e) => PerformLayoutUpdates();
            PerformLayoutUpdates();
        }

        private CardContainer AddCard(string title)
        {
            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Location      = new Point(20, 20),
                BackColor     = Color.Transparent
            };

            var card = new Panel
            {
                BackColor = CardBg,
                Margin    = new Padding(0, 0, 0, 16)
            };

            card.Paint += (s, e) =>
            {
                DrawRoundedBorder(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 12)
            };
            contentFlow.Controls.Add(titleLbl);
            card.Controls.Add(contentFlow);

            _mainFlow.Controls.Add(card);
            _cards.Add((card, contentFlow));

            return new CardContainer(card, contentFlow, this);
        }

        private void PerformLayoutUpdates()
        {
            int cardW = Math.Max(320, Width - 40);
            _mainFlow.Width = Width;

            foreach (var item in _cards)
            {
                item.Card.Width = cardW;
                item.ContentFlow.Width = cardW - 40;

                foreach (Control child in item.ContentFlow.Controls)
                {
                    if (child is Label lbl)
                    {
                        lbl.MaximumSize = new Size(cardW - 40, 0);
                    }
                    else if (child is Panel p)
                    {
                        p.Width = cardW - 40;
                        p.PerformLayout(); // Trigger formula box resizing internally
                    }
                    else if (child is TableLayoutPanel tbl)
                    {
                        tbl.Width = cardW - 40;
                    }
                    else if (child is FlowLayoutPanel fl)
                    {
                        fl.Width = cardW - 40;
                    }
                }

                item.ContentFlow.PerformLayout();
                item.Card.Height = item.ContentFlow.Height + 40;
            }

            _mainFlow.PerformLayout();
        }

        // ── Helper Subclasses / Factories ─────────────────────────────────────

        public class CardContainer
        {
            private readonly Panel _card;
            private readonly FlowLayoutPanel _flow;
            private readonly AboutPanel _parent;

            public CardContainer(Panel card, FlowLayoutPanel flow, AboutPanel parent)
            {
                _card = card;
                _flow = flow;
                _parent = parent;
            }

            public void AddControl(Control c) => _flow.Controls.Add(c);

            public void AddText(string text)
            {
                _flow.Controls.Add(new Label
                {
                    Text      = text,
                    Font      = new Font("Segoe UI", 9.5f),
                    ForeColor = TextMid,
                    AutoSize  = true,
                    Margin    = new Padding(0, 0, 0, 8),
                    BackColor = Color.Transparent
                });
            }

            public void AddBullet(string text)
            {
                _flow.Controls.Add(new Label
                {
                    Text      = "  •  " + text,
                    Font      = new Font("Segoe UI", 9.5f),
                    ForeColor = TextMid,
                    AutoSize  = true,
                    Margin    = new Padding(0, 0, 0, 6),
                    BackColor = Color.Transparent
                });
            }

            public void AddSubHeading(string text)
            {
                _flow.Controls.Add(new Label
                {
                    Text      = text,
                    Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                    ForeColor = TextDark,
                    AutoSize  = true,
                    Margin    = new Padding(0, 12, 0, 6),
                    BackColor = Color.Transparent
                });
            }

            public void AddFormula(string formulaText)
            {
                var p = new Panel
                {
                    BackColor = CodeBg,
                    Margin    = new Padding(0, 8, 0, 8)
                };
                p.Paint += (s, e) =>
                {
                    using var pen = new Pen(Border, 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                };

                var lbl = new Label
                {
                    Text      = formulaText,
                    Font      = new Font("Consolas", 10f),
                    ForeColor = TextDark,
                    Location  = new Point(12, 10),
                    AutoSize  = true,
                    BackColor = Color.Transparent
                };
                p.Controls.Add(lbl);

                p.Resize += (s, e) =>
                {
                    lbl.MaximumSize = new Size(p.Width - 24, 0);
                    p.Height = lbl.Height + 20;
                };

                _flow.Controls.Add(p);
            }
        }

        private TableLayoutPanel CreateModelTable()
        {
            var tbl = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount    = 7,
                AutoSize    = true,
                Margin      = new Padding(0, 12, 0, 12),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor   = Color.White
            };

            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));

            string[] headers = { "Model", "Arrival", "Service", "Servers", "Analytical Result" };
            for (int i = 0; i < headers.Length; i++)
            {
                var lbl = new Label
                {
                    Text      = headers[i],
                    Font      = new Font("Segoe UI Bold", 9.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(15, 23, 42),
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding   = new Padding(8),
                    Margin    = new Padding(0)
                };
                tbl.Controls.Add(lbl, i, 0);
            }

            string[][] data = new string[][]
            {
                new string[] { "M/M/1", "Markovian (Poisson)", "Exponential", "1", "Exact formulas" },
                new string[] { "M/M/N", "Markovian (Poisson)", "Exponential", "N", "Exact formulas" },
                new string[] { "M/G/1", "Markovian (Poisson)", "General", "1", "P-K formula" },
                new string[] { "M/G/N", "Markovian (Poisson)", "General", "N", "Simulation-based" },
                new string[] { "G/G/1", "General", "General", "1", "Kingman approx." },
                new string[] { "G/G/N", "General", "General", "N", "Simulation-based" }
            };

            for (int r = 0; r < data.Length; r++)
            {
                for (int c = 0; c < 5; c++)
                {
                    var lbl = new Label
                    {
                        Text      = data[r][c],
                        Font      = new Font("Segoe UI", 9f),
                        ForeColor = TextMid,
                        Dock      = DockStyle.Fill,
                        TextAlign = c == 0 ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter,
                        Padding   = new Padding(6),
                        Margin    = new Padding(0),
                        BackColor = r % 2 == 0 ? Color.FromArgb(248, 250, 252) : Color.White
                    };
                    tbl.Controls.Add(lbl, c, r + 1);
                }
            }

            return tbl;
        }

        private FlowLayoutPanel CreateVisualFlow()
        {
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Margin        = new Padding(0, 12, 0, 12),
                BackColor     = Color.Transparent
            };

            string[] steps = { "CUSTOMER ARRIVAL", "QUEUE", "AVAILABLE CASHIER", "SERVICE", "DEPARTURE" };
            for (int i = 0; i < steps.Length; i++)
            {
                var lbl = new Label
                {
                    Text      = steps[i],
                    Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(29, 78, 216),
                    BackColor = Color.FromArgb(239, 246, 255),
                    Padding   = new Padding(10, 6, 10, 6),
                    AutoSize  = true,
                    Margin    = new Padding(0, 0, 8, 8)
                };
                flow.Controls.Add(lbl);

                if (i < steps.Length - 1)
                {
                    var arrow = new Label
                    {
                        Text      = "➔",
                        Font      = new Font("Segoe UI", 12f),
                        ForeColor = TextLight,
                        AutoSize  = true,
                        Margin    = new Padding(0, 4, 8, 8)
                    };
                    flow.Controls.Add(arrow);
                }
            }

            return flow;
        }

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
