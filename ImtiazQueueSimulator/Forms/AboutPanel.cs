using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Professional, modern academic educational dashboard for Queueing Theory.
    /// Rebuilt with absolute zero-clipping vertical grids, responsive dual-column grids,
    /// horizontal-scroll comparison table, and unified branding colors.
    /// </summary>
    public class AboutPanel : UserControl
    {
        private FlowLayoutPanel _mainFlow = null!;
        private FlowLayoutPanel _modelsFlow = null!;
        
        private List<(Panel Card, FlowLayoutPanel ContentFlow)> _cards = new();
        private List<(Panel Card, FlowLayoutPanel ContentFlow)> _modelCards = new();

        // ── Design tokens ──────────────────────────────────────────────────────
        private static readonly Color PageBg      = Color.FromArgb(244, 246, 250);
        private static readonly Color CardBg      = Color.White;
        private static readonly Color CodeBg      = Color.FromArgb(248, 250, 252);
        private static readonly Color TextDark    = Color.FromArgb(15, 23, 42);   // Navy Dark
        private static readonly Color TextMid     = Color.FromArgb(71, 85, 105);   // Slate Mid
        private static readonly Color TextLight   = Color.FromArgb(100, 116, 139); // Muted Slate
        private static readonly Color Border      = Color.FromArgb(226, 232, 240); // Light Border
        private static readonly Color AccentBlue  = Color.FromArgb(29, 78, 216);   // Deep Blue
        private static readonly Color AccentGreen = Color.FromArgb(22, 163, 74);   // Success Green

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
            _modelCards.Clear();

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
                Margin        = new Padding(4, 10, 24, 24)
            };
            _mainFlow.Controls.Add(headerFlow);

            var titleLbl = new Label
            {
                Text      = "ABOUT EDUCATIONAL CONTENT",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 6)
            };
            headerFlow.Controls.Add(titleLbl);

            var subTitleLbl = new Label
            {
                Text      = "Reference material for queueing theory concepts used in this simulation.",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = TextLight,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 14)
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

            // ── 2. Key Metrics Card ──
            var metricsCard = AddCard("📐", "KEY METRICS EXPLAINED");
            metricsCard.AddText("Understand the core mathematical and operational performance metrics calculated by the analyzer:");
            metricsCard.AddMetricRow("Lq", "Average Queue Length", "The average number of customers waiting in the checkout queue (excludes customers currently in service).");
            metricsCard.AddMetricRow("L", "Average System Size", "The average number of customers in the checkout area (waiting in line + currently being served). L = customers waiting + customers currently being served.");
            metricsCard.AddMetricRow("Wq", "Average Waiting Time", "The average time spent waiting in queue before cashier service begins.");
            metricsCard.AddMetricRow("W", "Average System Time", "The average total time spent in the system (W = Wq + service time).");
            metricsCard.AddMetricRow("ρ", "Server Utilization", "The average fraction of time cashiers are busy serving customers. For single-server systems: ρ = λ / μ. For multi-server systems: ρ = λ / (Nμ). Must be < 1 for steady-state stability.");
            metricsCard.AddSubHeading("Little's Law");
            metricsCard.AddText("A foundational law in Operations Research that relates the system size, arrival rate, and times:");
            metricsCard.AddFormula("L = λW\nLq = λWq");
            metricsCard.AddText("\"System\" means customers currently waiting in the queue PLUS customers currently receiving service.");

            // ── 3. Queueing Model Explanations (Grid Header) ──
            var modelsHeaderFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Margin        = new Padding(4, 16, 0, 12),
                BackColor     = Color.Transparent
            };
            _mainFlow.Controls.Add(modelsHeaderFlow);

            var modelsHeaderIcon = new Label
            {
                Text      = "📋",
                Font      = new Font("Segoe UI", 12.5f),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 8, 0)
            };
            modelsHeaderFlow.Controls.Add(modelsHeaderIcon);

            var modelsHeaderTitle = new Label
            {
                Text      = "QUEUEING MODELS",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 1, 0, 0)
            };
            modelsHeaderFlow.Controls.Add(modelsHeaderTitle);

            // Create models grid FlowLayoutPanel
            _modelsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 0, 0, 24)
            };
            _mainFlow.Controls.Add(_modelsFlow);

            // Model 1 Card
            var m1 = AddModelCard("1️⃣", "M/M/1 Model", "Single Server (Markovian)");
            m1.AddText("Poisson arrivals, Exponential service, and a single cashier checkout counter.");
            m1.AddFormula(
                "ρ = λ / μ\n" +
                "P₀ = 1 − ρ\n" +
                "Lq = ρ² / (1 − ρ)  or  Lq = λ² / [μ(μ − λ)]\n" +
                "L = ρ / (1 − ρ)  or  L = λ / (μ − λ)\n" +
                "Wq = Lq / λ  or  Wq = λ / [μ(μ − λ)]\n" +
                "W = L / λ  or  W = 1 / (μ − λ)"
            );
            m1.AddSubHeading("Stability Condition:");
            m1.AddText("λ < μ (or ρ < 1)");

            // Model 2 Card
            var m2 = AddModelCard("2️⃣", "M/M/N Model", "N Parallel Servers (Markovian)");
            m2.AddText("Poisson arrivals, Exponential service, and N parallel cashier checkout counters.");
            m2.AddFormula(
                "a = λ / μ\n" +
                "ρ = λ / (Nμ)\n" +
                "P₀ = [ Σ_{k=0}^{N-1} (a^k / k!) + (a^N / N!) × 1/(1−ρ) ]⁻¹\n" +
                "P(wait) = (a^N / N!) × P₀ / (1−ρ)    (Erlang-C Formula)\n" +
                "Lq = P(wait) × ρ / (1−ρ)\n" +
                "Wq = Lq / λ\n" +
                "W = Wq + 1/μ\n" +
                "L = λW"
            );
            m2.AddSubHeading("Stability Condition:");
            m2.AddText("ρ < 1 (or λ < Nμ)");

            // Model 3 Card
            var m3 = AddModelCard("3️⃣", "M/G/1 Model", "Single Server, General Service");
            m3.AddText("Poisson arrivals, General service distribution (e.g. constant/uniform scan times), and one cashier. Calculates queue size using the Pollaczek-Khinchine formula.");
            m3.AddFormula(
                "ρ = λE[S]\n" +
                "Lq = λ²E[S²] / [2(1−ρ)]\n" +
                "Wq = Lq / λ\n" +
                "W = Wq + E[S]\n" +
                "L = λW"
            );
            m3.AddText("Where E[S] = mean service time, and E[S²] = second moment of service time.\nUseful when service times are constant or follow arbitrary non-exponential distributions.");

            // Model 4 Card
            var m4 = AddModelCard("4️⃣", "M/G/N Model", "N Servers, General Service");
            m4.AddText("Poisson arrivals, General service distribution, and N parallel cashier counters.");
            m4.AddText("There is no single simple closed-form analytical formula for the general M/G/N model.");
            m4.AddNote("The simulator estimates Lq, L, Wq, W, and ρ using discrete-event simulation. Simulation is especially useful for general multi-server systems where analytical formulas become difficult.");

            // Model 5 Card
            var m5 = AddModelCard("5️⃣", "G/G/1 Model", "Single Server, General Arrivals & Service");
            m5.AddText("General inter-arrival distribution, General service-time distribution, and one cashier. Uses Kingman's heavy-traffic approximation.");
            m5.AddFormula(
                "Wq ≈ [ρ / (1−ρ)] × [(Ca² + Cs²)/2] × E[S]\n" +
                "Lq ≈ λWq\n" +
                "W = Wq + E[S]\n" +
                "L = λW"
            );
            m5.AddSubHeading("Definitions:");
            m5.AddText("• Ca = coefficient of variation of inter-arrival times\n• Cs = coefficient of variation of service times\n• ρ = λE[S]\n• E[S] = mean service time");
            m5.AddText("Note: This is an approximation rather than an exact closed-form result.");

            // Model 6 Card
            var m6 = AddModelCard("6️⃣", "G/G/N Model", "N Servers, General Arrivals & Service");
            m6.AddText("General arrival process, General service-time distribution, and N parallel cashiers.");
            m6.AddText("There is no simple universal closed-form formula.");
            m6.AddNote("The simulator uses discrete-event simulation to estimate Lq, L, Wq, W, and ρ. This is the most general model among the six models supported by this project.");

            // ── 4. Model Comparison Table ──
            var tableCard = AddCard("▤", "MODEL COMPARISON TABLE");
            tableCard.AddText("Overview of arrival/service processes and analytical availability for the 6 models:");
            tableCard.AddControl(CreateModelTable());

            // ── 5. Simulation Variables & Formulas ──
            var simFormulasCard = AddCard("📝", "SIMULATION VARIABLES & FORMULAS");
            simFormulasCard.AddText("The simulator models individual customer journeys sequentially. The main timing parameters are generated using inverse transform sampling, and metrics are derived dynamically:");
            simFormulasCard.AddSubHeading("Exponential Inter-Arrival Generation");
            simFormulasCard.AddFormula("IA = −ln(U) / λ");
            simFormulasCard.AddText("where U is a uniformly distributed random number between 0 and 1.\nCustomer arrival time is calculated as:");
            simFormulasCard.AddFormula("ArrivalTime(i) = ArrivalTime(i−1) + IA(i)");
            simFormulasCard.AddText("A smaller inter-arrival time means customers arrive more frequently.");
            simFormulasCard.AddSubHeading("Exponential Service Duration Generation");
            simFormulasCard.AddFormula("ServiceTime = −ln(U) / μ");
            simFormulasCard.AddText("where U is a uniform random number in (0,1). For general distributions (e.g. Uniform, Normal), service time is generated according to standard statistical transform bounds.");
            simFormulasCard.AddSubHeading("Timing Formulas");
            simFormulasCard.AddFormula(
                "Wq = Service Start Time − Arrival Time\n" +
                "Service = Departure Time − Service Start Time\n" +
                "W = Departure Time − Arrival Time = Wq + Service"
            );
            simFormulasCard.AddSubHeading("Supermarket System State Calculations");
            simFormulasCard.AddFormula(
                "System Size = Queue Customers + Customers Currently Being Served"
            );

            // ── 6. How Customer Metrics are Calculated ──
            var customerMetricsCard = AddCard("👥", "HOW CUSTOMER METRICS ARE CALCULATED");
            customerMetricsCard.AddText("Every customer record details their timestamps (formatted as HH:MM:SS) and durations:");
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
            var stateMetricsCard = AddCard("📊", "SYSTEM STATE ON ARRIVAL & SERVICE");
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
            var aggMetricsCard = AddCard("📈", "SIMULATION PERFORMANCE METRICS");
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
            var comparisonTheoryCard = AddCard("🔬", "THEORETICAL vs SIMULATION RESULTS");
            comparisonTheoryCard.AddText("Theoretical values are calculated using asymptotic mathematical queueing formulas.");
            comparisonTheoryCard.AddText("Simulation values are computed from actual simulated customer events.");
            comparisonTheoryCard.AddText("They may differ due to initial transient effects, finite sample sizes, and random variations. As the simulation duration and customer count increase, simulation values converge to theoretical values.");

            // ── 10. Real-world Imtiaz Checkout Example ──
            var realWorldCard = AddCard("🛒", "REAL-WORLD IMTIAZ CHECKOUT EXAMPLE");
            realWorldCard.AddText("Checkout operations are mapped to queueing concepts as follows:");
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

            // ── 11. Model Assumptions ──
            var assumptionsCard = AddCard("⚠️", "MODEL ASSUMPTIONS");
            assumptionsCard.AddBullet("Queue Discipline: First-Come, First-Served (FCFS/FIFO).");
            assumptionsCard.AddBullet("Parallel Cashiers: Multiple counters operate independently and in parallel.");
            assumptionsCard.AddBullet("Infinite Buffer: Customers do not balk (refuse to join) or reneg (leave queue).");
            assumptionsCard.AddBullet("Units consistency: All rates (λ, μ) must use the same time unit (hours).");
            assumptionsCard.AddBullet("System Stability: Analytical calculations require ρ < 1.");

            // ── 12. How to Use Section ──
            var usageCard = AddCard("🧭", "HOW TO USE THE SIMULATOR");
            usageCard.AddBullet("1. Select a queueing model from the model dropdown.");
            usageCard.AddBullet("2. Enter/select λ (arrival rate) for incoming supermarket shoppers.");
            usageCard.AddBullet("3. Enter/select μ (service rate) for checkout scanning speed.");
            usageCard.AddBullet("4. Select the number of cashiers (N) currently active.");
            usageCard.AddBullet("5. Click Run to execute the simulation.");
            usageCard.AddBullet("6. Compare results side-by-side with theoretical formulas.");
            usageCard.AddBullet("7. Use Customer Records, Queue History, Analytics, and Reports tabs to perform analytical diagnostics.");

            // ── 13. Glossary ──
            var glossaryCard = AddCard("📖", "GLOSSARY OF SYMBOLS");
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

            // ── Setup Responsive Resize Listener ──
            Resize += (s, e) => PerformLayoutUpdates();
            PerformLayoutUpdates();
        }

        private CardContainer AddCard(string icon, string title)
        {
            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Location      = new Point(20, 20),
                BackColor     = Color.Transparent,
                Padding       = new Padding(4, 4, 4, 4)
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

            var headingFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Margin        = new Padding(0, 0, 0, 16),
                BackColor     = Color.Transparent
            };

            var iconLbl = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = AccentBlue,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 10, 0),
                BackColor = Color.Transparent
            };
            headingFlow.Controls.Add(iconLbl);

            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 1, 0, 0),
                BackColor = Color.Transparent
            };
            headingFlow.Controls.Add(titleLbl);

            contentFlow.Controls.Add(headingFlow);
            card.Controls.Add(contentFlow);

            _mainFlow.Controls.Add(card);
            _cards.Add((card, contentFlow));

            return new CardContainer(card, contentFlow, this);
        }

        private CardContainer AddModelCard(string icon, string title, string badgeText)
        {
            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Location      = new Point(20, 20),
                BackColor     = Color.Transparent,
                Padding       = new Padding(4, 4, 4, 4)
            };

            var card = new Panel
            {
                BackColor = CardBg,
                Margin    = new Padding(0, 0, 16, 16)
            };

            card.Paint += (s, e) =>
            {
                DrawRoundedBorder(e.Graphics, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            // Badge / Title row
            var topFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Margin        = new Padding(0, 0, 0, 12),
                BackColor     = Color.Transparent
            };
            contentFlow.Controls.Add(topFlow);

            var iconLbl = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 11.5f),
                ForeColor = AccentBlue,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 8, 0),
                BackColor = Color.Transparent
            };
            topFlow.Controls.Add(iconLbl);

            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 8, 0),
                BackColor = Color.Transparent
            };
            topFlow.Controls.Add(titleLbl);

            var badge = new Label
            {
                Text      = badgeText,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = AccentBlue,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding   = new Padding(6, 3, 6, 3),
                AutoSize  = true,
                Margin    = new Padding(0, 1, 0, 0)
            };
            topFlow.Controls.Add(badge);

            card.Controls.Add(contentFlow);
            _modelsFlow.Controls.Add(card);

            _modelCards.Add((card, contentFlow));

            return new CardContainer(card, contentFlow, this);
        }

        private void PerformLayoutUpdates()
        {
            int cardW = Math.Max(320, Width - 40);
            _mainFlow.Width = Width;

            // Layout standard full-width cards
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
                        p.PerformLayout(); // update formula box or grid internally
                    }
                    else if (child is DataGridView dgv)
                    {
                        dgv.Width = cardW - 40;
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

            // Layout model cards inside the models grid FlowLayoutPanel
            int gap = 16;
            int numCols = 1;
            if (cardW >= 780) numCols = 2; // Two columns on desktop
            
            int modelCardW = (cardW - (numCols - 1) * gap) / numCols;
            foreach (var item in _modelCards)
            {
                item.Card.Width = modelCardW;
                item.ContentFlow.Width = modelCardW - 40;

                foreach (Control child in item.ContentFlow.Controls)
                {
                    if (child is Label lbl)
                    {
                        lbl.MaximumSize = new Size(modelCardW - 40, 0);
                    }
                    else if (child is Panel p)
                    {
                        p.Width = modelCardW - 40;
                        p.PerformLayout();
                    }
                    else if (child is FlowLayoutPanel fl)
                    {
                        fl.Width = modelCardW - 40;
                    }
                }

                item.ContentFlow.PerformLayout();
                item.Card.Height = item.ContentFlow.Height + 40;
            }

            _modelsFlow.Width = cardW;
            _modelsFlow.PerformLayout();

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
                _card   = card;
                _flow   = flow;
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
                    Font      = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                    ForeColor = TextDark,
                    AutoSize  = true,
                    Margin    = new Padding(0, 12, 0, 6),
                    BackColor = Color.Transparent
                });
            }

            public void AddNote(string text)
            {
                var p = new Panel
                {
                    BackColor = Color.FromArgb(240, 253, 244), // Subtle Success Green Background
                    Margin    = new Padding(0, 8, 0, 8)
                };
                p.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(187, 247, 208), 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                };

                var lbl = new Label
                {
                    Text      = "ℹ  " + text,
                    Font      = new Font("Segoe UI Semibold", 9.5f),
                    ForeColor = AccentGreen,
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

            public void AddMetricRow(string symbol, string name, string explanation)
            {
                var rowPanel = _parent.CreateMetricRow(symbol, name, explanation);
                _flow.Controls.Add(rowPanel);
            }
        }

        private Panel CreateMetricRow(string symbol, string name, string explanation)
        {
            var p = new Panel
            {
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 0, 0, 12)
            };

            // Symbol badge
            var symLbl = new Label
            {
                Text      = symbol,
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                BackColor = Color.FromArgb(239, 246, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(45, 24),
                Location  = new Point(4, 4) // padded to prevent clipping
            };
            p.Controls.Add(symLbl);

            // Name
            var nameLbl = new Label
            {
                Text      = name,
                Font      = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                ForeColor = TextDark,
                Location  = new Point(64, 6),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            p.Controls.Add(nameLbl);

            // Explanation
            var expLbl = new Label
            {
                Text      = explanation,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextMid,
                Location  = new Point(64, 32),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            p.Controls.Add(expLbl);

            // Divider
            var div = new Panel
            {
                BackColor = Border,
                Height    = 1,
                Location  = new Point(0, 0)
            };
            p.Controls.Add(div);

            p.Resize += (s, e) =>
            {
                int w = p.Width;
                expLbl.MaximumSize = new Size(w - 74, 0);
                div.Location = new Point(0, expLbl.Bottom + 12);
                div.Width    = w;
                p.Height     = div.Bottom + 4;
            };

            return p;
        }

        private DataGridView CreateModelTable()
        {
            var grid = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Border,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Horizontal, // only show horizontal scrollbar if needed
                Height = 246,
                Margin = new Padding(0, 12, 0, 12)
            };

            // Custom modern visual style
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(6)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = TextMid,
                Font = new Font("Segoe UI", 9f),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(6),
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = TextDark
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = TextMid,
                Font = new Font("Segoe UI", 9f),
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = TextDark
            };

            // Columns definition
            var cModel = new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Model", FillWeight = 15, MinimumWidth = 70 };
            var cArrival = new DataGridViewTextBoxColumn { Name = "Arrival", HeaderText = "Arrival Process", FillWeight = 23, MinimumWidth = 140 };
            var cService = new DataGridViewTextBoxColumn { Name = "Service", HeaderText = "Service Process", FillWeight = 23, MinimumWidth = 140 };
            var cServers = new DataGridViewTextBoxColumn { Name = "Servers", HeaderText = "Servers", FillWeight = 14, MinimumWidth = 70 };
            var cMethod = new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "Analytical / Simulation Method", FillWeight = 25, MinimumWidth = 200 };

            grid.Columns.AddRange(new DataGridViewColumn[] { cModel, cArrival, cService, cServers, cMethod });

            // Alignment tweaks
            grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Rows data
            grid.Rows.Add("M/M/1", "Markovian (Poisson)", "Exponential", "1", "Exact formulas");
            grid.Rows.Add("M/M/N", "Markovian (Poisson)", "Exponential", "N", "Exact formulas");
            grid.Rows.Add("M/G/1", "Markovian (Poisson)", "General", "1", "P-K formula");
            grid.Rows.Add("M/G/N", "Markovian (Poisson)", "General", "N", "Simulation-based");
            grid.Rows.Add("G/G/1", "General", "General", "1", "Kingman approximation");
            grid.Rows.Add("G/G/N", "General", "General", "N", "Simulation-based");

            // Disable automatic visual selection selection on load
            grid.DataBindingComplete += (s, e) => grid.ClearSelection();
            
            return grid;
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
