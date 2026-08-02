using System.Text;
using ImtiazQueueSimulator.Models;
using ImtiazQueueSimulator.Statistics;

namespace ImtiazQueueSimulator.Reports
{
    /// <summary>
    /// Generates comprehensive TXT reports from simulation results.
    /// Uses StreamWriter for file output.
    /// </summary>
    public static class ReportGenerator
    {
        /// <summary>
        /// Generate a complete TXT report
        /// </summary>
        public static string GenerateReport(SimulationResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       IMTIAZ SUPERMARKET - QUEUE SIMULATION REPORT            ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"  Report Generated: {result.CreatedAt:dd MMM yyyy HH:mm:ss}");
            sb.AppendLine($"  Queueing Model:   {result.ModelName}");
            sb.AppendLine();

            // ── Parameters ──
            sb.AppendLine("─── INPUT PARAMETERS ────────────────────────────────────────────");
            sb.AppendLine($"  Arrival Rate (λ):       {result.Lambda:F4} customers/hour");
            sb.AppendLine($"  Service Rate (μ):       {result.Mu:F4} customers/hour");
            sb.AppendLine($"  Number of Servers (N):  {result.NumServers}");
            sb.AppendLine($"  Simulation Time:        {result.SimulationTime:F2} hours");
            sb.AppendLine($"  Arrival Distribution:   {result.ArrivalDistribution}");
            sb.AppendLine($"  Service Distribution:   {result.ServiceDistribution}");
            if (result.RandomSeed.HasValue)
                sb.AppendLine($"  Random Seed:            {result.RandomSeed}");
            sb.AppendLine();

            // ── Stability ──
            double rho = result.Lambda / (result.NumServers * result.Mu);
            sb.AppendLine("─── STABILITY CHECK ─────────────────────────────────────────────");
            sb.AppendLine($"  ρ = λ/(Nμ) = {result.Lambda:F2}/({result.NumServers}×{result.Mu:F2}) = {rho:F4}");
            if (rho >= 1)
                sb.AppendLine("  ⚠ WARNING: System is UNSTABLE (ρ ≥ 1)");
            else
                sb.AppendLine("  ✓ System is STABLE (ρ < 1)");
            sb.AppendLine();

            // ── Analytical Results ──
            if (result.HasAnalyticalResults)
            {
                sb.AppendLine("─── ANALYTICAL RESULTS ──────────────────────────────────────────");
                string label = result.ModelName == "G/G/1" ? "(Kingman Approximation)" : "";
                if (!string.IsNullOrEmpty(label)) sb.AppendLine($"  {label}");
                sb.AppendLine($"  Lq  = {FormatVal(result.AnalyticalLq)} customers");
                sb.AppendLine($"  L   = {FormatVal(result.AnalyticalL)} customers");
                sb.AppendLine($"  Wq  = {FormatVal(result.AnalyticalWq)} hours ({FormatMin(result.AnalyticalWq)})");
                sb.AppendLine($"  W   = {FormatVal(result.AnalyticalW)} hours ({FormatMin(result.AnalyticalW)})");
                sb.AppendLine($"  ρ   = {FormatVal(result.AnalyticalRho)}");
                if (!double.IsNaN(result.AnalyticalP0))
                    sb.AppendLine($"  P0  = {FormatVal(result.AnalyticalP0)}");
                sb.AppendLine();
            }

            // ── Simulation Results ──
            sb.AppendLine("─── SIMULATION RESULTS ──────────────────────────────────────────");
            sb.AppendLine($"  Lq (time-avg)     = {FormatVal(result.SimLq)} customers");
            sb.AppendLine($"  L  (time-avg)     = {FormatVal(result.SimL)} customers");
            sb.AppendLine($"  Wq (customer-avg) = {FormatVal(result.SimWq)} hours ({FormatMin(result.SimWq)})");
            sb.AppendLine($"  W  (customer-avg) = {FormatVal(result.SimW)} hours ({FormatMin(result.SimW)})");
            sb.AppendLine($"  ρ  (avg util)     = {FormatVal(result.SimRho)} ({FormatPct(result.SimRho)})");
            sb.AppendLine($"  Effective λ       = {result.EffectiveLambda:F4} customers/hour");
            sb.AppendLine();
            sb.AppendLine($"  Total Customers:      {result.TotalCustomers}");
            sb.AppendLine($"  Customers Served:     {result.CustomersServed}");
            sb.AppendLine($"  Customers Who Waited: {result.CustomersWhoWaited}");
            sb.AppendLine($"  Max Queue Length:     {result.MaxQueueLength}");
            sb.AppendLine($"  Prob. of Waiting:     {result.ProbabilityOfWaiting:F4} ({result.ProbabilityOfWaiting * 100:F1}%)");
            sb.AppendLine();

            // ── Error Analysis ──
            if (result.HasAnalyticalResults)
            {
                sb.AppendLine("─── ERROR ANALYSIS (Analytical vs Simulation) ───────────────────");
                sb.AppendLine($"  Lq Error = {FormatErr(result.LqError)}");
                sb.AppendLine($"  L  Error = {FormatErr(result.LError)}");
                sb.AppendLine($"  Wq Error = {FormatErr(result.WqError)}");
                sb.AppendLine($"  W  Error = {FormatErr(result.WError)}");
                sb.AppendLine();
            }

            // ── Little's Law ──
            sb.AppendLine("─── LITTLE'S LAW VALIDATION ─────────────────────────────────────");
            sb.AppendLine(QueueStatistics.ValidateLittlesLaw(result));
            sb.AppendLine();

            // ── Server Utilization ──
            sb.AppendLine("─── SERVER UTILIZATION ──────────────────────────────────────────");
            for (int i = 0; i < result.ServerUtilizations.Length; i++)
            {
                sb.AppendLine($"  Cashier {i + 1:D2}: {result.ServerUtilizations[i] * 100:F1}%");
            }
            sb.AppendLine($"  Average:   {result.SimRho * 100:F1}%");
            sb.AppendLine();

            // ── Top Waiting Customers ──
            var top = QueueStatistics.GetLongestWaiting(result.AllCustomers, 10);
            if (top.Count > 0)
            {
                sb.AppendLine("─── LONGEST WAITING CUSTOMERS ───────────────────────────────────");
                for (int i = 0; i < top.Count; i++)
                {
                    sb.AppendLine($"  {i + 1,2}. {top[i].Name} — {Customer.FormatDuration(top[i].WaitingTime)}");
                }
                sb.AppendLine();
            }

            // ── Customer History ──
            sb.AppendLine("─── COMPLETE CUSTOMER HISTORY ───────────────────────────────────");
            sb.AppendLine($"{"ID",-6} {"Name",-16} {"Arrival",-10} {"Svc Start",-10} {"Service",-15} {"Departure",-10} {"Wq",-15} {"W",-15} {"Server",-12} {"Status",-10}");
            sb.AppendLine(new string('-', 128));
            foreach (var c in result.AllCustomers)
            {
                sb.AppendLine($"{c.Id,-6} {c.Name,-16} " +
                    $"{Customer.FormatTime(c.ArrivalTime),-10} " +
                    $"{Customer.FormatTime(c.ServiceStartTime),-10} " +
                    $"{Customer.FormatDuration(c.ServiceTime),-15} " +
                    $"{Customer.FormatTime(c.DepartureTime),-10} " +
                    $"{Customer.FormatDuration(c.WaitingTime),-15} " +
                    $"{Customer.FormatDuration(c.TimeInSystem),-15} " +
                    $"{(c.AssignedServer > 0 ? "Cashier " + c.AssignedServer : "—"),-12} " +
                    $"{c.Status,-10}");
            }
            sb.AppendLine();

            // ── Queue Timeline ──
            sb.AppendLine("─── COMPLETE QUEUE TIMELINE ─────────────────────────────────────");
            sb.AppendLine($"{"Time",-10} {"Event",-20} {"Customer",-25} {"Queue",-8} {"System",-8} {"Busy",-6}");
            sb.AppendLine(new string('-', 77));
            foreach (var s in result.Snapshots)
            {
                sb.AppendLine($"{s.FormattedTime,-10} {s.EventIcon + " " + s.EventDescription,-20} " +
                    $"{s.CustomerInfo,-25} {s.QueueLength,-8} {s.CustomersInSystem,-8} {s.BusyServers,-6}");
            }
            sb.AppendLine();

            // ── Model Explanation ──
            sb.AppendLine("─── MODEL EXPLANATION ───────────────────────────────────────────");
            sb.AppendLine(GetModelExplanation(result.ModelName));
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine("  End of Report — Imtiaz Queue Analyzer");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Save report to file using StreamWriter
        /// </summary>
        public static void SaveToFile(string filePath, SimulationResult result)
        {
            string content = GenerateReport(result);
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.Write(content);
        }

        private static string FormatVal(double v) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "N/A" : $"{v:F4}";

        private static string FormatMin(double hours) =>
            double.IsNaN(hours) || double.IsInfinity(hours) ? "N/A" : $"{hours * 60:F2} min";

        private static string FormatPct(double v) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "N/A" : $"{v * 100:F1}%";

        private static string FormatErr(double v) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "N/A" : $"{v:F2}%";

        public static string GetModelExplanation(string model)
        {
            return model switch
            {
                "M/M/1" => "M/M/1: Markovian (Poisson) arrivals, Markovian (Exponential) service times,\n" +
                           "  1 server. This is the simplest queueing model with FCFS discipline.\n" +
                           "  Formulas: ρ=λ/μ, Lq=ρ²/(1-ρ), L=ρ/(1-ρ), Wq=ρ/(μ(1-ρ)), W=1/(μ-λ)",

                "M/M/N" => "M/M/N: Markovian arrivals, Markovian service, N parallel servers.\n" +
                           "  Uses Erlang-C formula for P(wait). Customers join a single queue and are\n" +
                           "  served by the first available cashier. Stable when λ < Nμ.",

                "M/G/1" => "M/G/1: Markovian arrivals, General service distribution, 1 server.\n" +
                           "  Uses the Pollaczek-Khinchine formula: Wq = λE[S²] / (2(1-ρ)).\n" +
                           "  This model captures the effect of service time variability.",

                "M/G/N" => "M/G/N: Markovian arrivals, General service, N servers.\n" +
                           "  No simple closed-form solution exists. Results are obtained from\n" +
                           "  discrete-event simulation.",

                "G/G/1" => "G/G/1: General arrivals, General service, 1 server.\n" +
                           "  Uses Kingman's approximation:\n" +
                           "  Wq ≈ (ρ/(1-ρ)) × ((Ca²+Cs²)/2) × E[S]\n" +
                           "  where Ca and Cs are coefficients of variation.",

                "G/G/N" => "G/G/N: General arrivals, General service, N servers.\n" +
                           "  The most general model. No closed-form solution exists.\n" +
                           "  All results are obtained from discrete-event simulation.",

                _ => "Unknown model."
            };
        }
    }
}
