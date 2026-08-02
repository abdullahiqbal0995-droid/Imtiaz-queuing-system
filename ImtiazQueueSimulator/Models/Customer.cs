namespace ImtiazQueueSimulator.Models
{
    /// <summary>
    /// Represents a customer in the supermarket checkout simulation.
    /// Tracks complete journey from arrival through service to departure.
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        // Timing
        public double ArrivalTime { get; set; }
        public double QueueEntryTime { get; set; }
        public double ServiceStartTime { get; set; }
        public double ServiceTime { get; set; }
        public double DepartureTime { get; set; }

        // Calculated metrics
        public double WaitingTime { get; set; }      // Wq = ServiceStartTime - ArrivalTime
        public double TimeInSystem { get; set; }      // W  = DepartureTime - ArrivalTime

        // Server assignment
        public int AssignedServer { get; set; } = -1;

        // Status: "Arrived", "Waiting", "InService", "Completed"
        public string Status { get; set; } = "Arrived";

        // System state when customer arrived
        public int QueueLengthOnArrival { get; set; }
        public int SystemSizeOnArrival { get; set; }

        // System state when service started
        public int QueueLengthOnServiceStart { get; set; }
        public int SystemSizeOnServiceStart { get; set; }

        public Customer(int id, double arrivalTime)
        {
            Id = id;
            Name = $"Customer {id:D3}";
            ArrivalTime = arrivalTime;
            QueueEntryTime = arrivalTime;
            ServiceStartTime = double.NaN;
            ServiceTime = double.NaN;
            DepartureTime = double.NaN;
            WaitingTime = double.NaN;
            TimeInSystem = double.NaN;
        }

        /// <summary>
        /// Format time in hours as HH:MM:SS string
        /// </summary>
        public static string FormatTime(double hours)
        {
            if (double.IsNaN(hours) || double.IsInfinity(hours)) return "--:--:--";
            TimeSpan ts = TimeSpan.FromHours(hours);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// Format duration in hours as human-readable string
        /// </summary>
        public static string FormatDuration(double hours)
        {
            if (double.IsNaN(hours) || double.IsInfinity(hours)) return "--";
            TimeSpan ts = TimeSpan.FromHours(hours);
            if (ts.TotalMinutes < 1)
                return $"{ts.Seconds} sec";
            if (ts.TotalHours < 1)
                return $"{(int)ts.TotalMinutes} min {ts.Seconds} sec";
            return $"{(int)ts.TotalHours} hr {ts.Minutes} min {ts.Seconds} sec";
        }
    }
}
