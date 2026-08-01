namespace ImtiazQueueSimulator.Statistics
{
    using ImtiazQueueSimulator.Models;

    /// <summary>
    /// Computes and formats queue statistics from simulation results.
    /// Provides Little's Law validation and formatted metric strings.
    /// </summary>
    public static class QueueStatistics
    {
        /// <summary>
        /// Format a metric value for display
        /// </summary>
        public static string FormatMetric(double value, string unit = "")
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "N/A";
            if (unit == "min")
                return $"{value * 60:F2} min";
            if (unit == "%")
                return $"{value * 100:F1}%";
            return $"{value:F4}";
        }

        /// <summary>
        /// Format time in hours to minutes for display
        /// </summary>
        public static string FormatMinutes(double hours)
        {
            if (double.IsNaN(hours) || double.IsInfinity(hours)) return "N/A";
            return $"{hours * 60:F2} min";
        }

        /// <summary>
        /// Get top N customers by waiting time
        /// </summary>
        public static List<Customer> GetLongestWaiting(List<Customer> customers, int topN = 5)
        {
            return customers
                .Where(c => c.Status == "Completed")
                .OrderByDescending(c => c.WaitingTime)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// Get the stability status message
        /// </summary>
        public static (bool IsStable, string Message) CheckStability(double lambda, double mu, int servers)
        {
            double rho = lambda / (servers * mu);
            if (rho >= 1)
            {
                return (false,
                    $"⚠ WARNING: System is UNSTABLE (ρ = {rho:F3} ≥ 1).\n" +
                    $"Arrival rate ({lambda:F1}) ≥ Total service capacity ({servers * mu:F1}).\n" +
                    "Queue will grow without bound.");
            }
            return (true, $"✓ System is stable (ρ = {rho:F3} < 1)");
        }

        /// <summary>
        /// Validate Little's Law and return message
        /// </summary>
        public static string ValidateLittlesLaw(SimulationResult result)
        {
            if (double.IsNaN(result.SimLq) || double.IsNaN(result.SimWq))
                return "Insufficient data for Little's Law validation.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("LITTLE'S LAW VALIDATION");
            sb.AppendLine("═══════════════════════");
            sb.AppendLine($"Effective λ = {result.EffectiveLambda:F4} customers/hour");
            sb.AppendLine();
            sb.AppendLine("Queue: Lq ≈ λ_eff × Wq");
            sb.AppendLine($"  Lq (time-average)  = {result.SimLq:F4}");
            sb.AppendLine($"  λ_eff × Wq         = {result.LittleLawLq:F4}");
            double lqErr = result.SimLq > 0 ? Math.Abs(result.SimLq - result.LittleLawLq) / result.SimLq * 100 : 0;
            sb.AppendLine($"  Difference         = {lqErr:F2}%");
            sb.AppendLine();
            sb.AppendLine("System: L ≈ λ_eff × W");
            sb.AppendLine($"  L (time-average)   = {result.SimL:F4}");
            sb.AppendLine($"  λ_eff × W          = {result.LittleLawL:F4}");
            double lErr = result.SimL > 0 ? Math.Abs(result.SimL - result.LittleLawL) / result.SimL * 100 : 0;
            sb.AppendLine($"  Difference         = {lErr:F2}%");

            return sb.ToString();
        }
    }
}
