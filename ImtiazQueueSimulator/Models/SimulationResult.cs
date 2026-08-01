namespace ImtiazQueueSimulator.Models
{
    /// <summary>
    /// Container for all simulation and analytical results.
    /// </summary>
    public class SimulationResult
    {
        // Model info
        public string ModelName { get; set; } = "";
        public double Lambda { get; set; }
        public double Mu { get; set; }
        public int NumServers { get; set; }
        public double SimulationTime { get; set; }

        // Analytical results
        public double AnalyticalLq { get; set; } = double.NaN;
        public double AnalyticalL { get; set; } = double.NaN;
        public double AnalyticalWq { get; set; } = double.NaN;
        public double AnalyticalW { get; set; } = double.NaN;
        public double AnalyticalRho { get; set; } = double.NaN;
        public double AnalyticalP0 { get; set; } = double.NaN;

        // Simulation results
        public double SimLq { get; set; } = double.NaN;
        public double SimL { get; set; } = double.NaN;
        public double SimWq { get; set; } = double.NaN;
        public double SimW { get; set; } = double.NaN;
        public double SimRho { get; set; } = double.NaN;

        // Additional stats
        public int TotalCustomers { get; set; }
        public int CustomersServed { get; set; }
        public int CustomersWhoWaited { get; set; }
        public int MaxQueueLength { get; set; }
        public double ProbabilityOfWaiting { get; set; }
        public double EffectiveLambda { get; set; }

        // Little's Law validation
        public double LittleLawLq { get; set; } = double.NaN;   // λ_eff × Wq
        public double LittleLawL { get; set; } = double.NaN;    // λ_eff × W

        // Error percentages
        public double LqError => CalculateError(AnalyticalLq, SimLq);
        public double LError => CalculateError(AnalyticalL, SimL);
        public double WqError => CalculateError(AnalyticalWq, SimWq);
        public double WError => CalculateError(AnalyticalW, SimW);

        // Per-server utilization
        public double[] ServerUtilizations { get; set; } = Array.Empty<double>();

        // Data collections
        public List<Customer> AllCustomers { get; set; } = new();
        public List<QueueSnapshot> Snapshots { get; set; } = new();

        // Chart data
        public List<(double Time, int QueueLength)> QueueLengthOverTime { get; set; } = new();
        public List<(double Time, int SystemSize)> SystemSizeOverTime { get; set; } = new();
        public List<(double Time, int Arrivals, int Departures)> ArrivalDepartureOverTime { get; set; } = new();

        // Distribution info
        public string ArrivalDistribution { get; set; } = "Exponential";
        public string ServiceDistribution { get; set; } = "Exponential";
        public int? RandomSeed { get; set; }

        // Timestamp
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        private double CalculateError(double analytical, double simulation)
        {
            if (double.IsNaN(analytical) || double.IsNaN(simulation)) return double.NaN;
            if (analytical == 0) return simulation == 0 ? 0 : 100;
            return Math.Abs((simulation - analytical) / analytical) * 100;
        }

        public bool HasAnalyticalResults =>
            !double.IsNaN(AnalyticalLq) && !double.IsNaN(AnalyticalL);
    }
}
