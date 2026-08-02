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

        // ── Display properties enforcing 100% mathematical consistency ───────
        public string DisplayArrival => FormatTime(ArrivalTime);
        public string DisplaySvcStart => FormatTime(ServiceStartTime);
        public string DisplayDeparture => FormatTime(DepartureTime);

        public string DisplayWq
        {
            get
            {
                if (double.IsNaN(ArrivalTime) || double.IsNaN(ServiceStartTime)) return "--";
                long arrSec = (long)Math.Round(ArrivalTime * 3600);
                long svcSec = (long)Math.Round(ServiceStartTime * 3600);
                return FormatDurationFromSeconds(svcSec - arrSec);
            }
        }

        public string DisplayService
        {
            get
            {
                if (double.IsNaN(ServiceStartTime) || double.IsNaN(DepartureTime)) return "--";
                long svcSec = (long)Math.Round(ServiceStartTime * 3600);
                long depSec = (long)Math.Round(DepartureTime * 3600);
                return FormatDurationFromSeconds(depSec - svcSec);
            }
        }

        public string DisplayW
        {
            get
            {
                if (double.IsNaN(ArrivalTime) || double.IsNaN(DepartureTime)) return "--";
                long arrSec = (long)Math.Round(ArrivalTime * 3600);
                long depSec = (long)Math.Round(DepartureTime * 3600);
                return FormatDurationFromSeconds(depSec - arrSec);
            }
        }

        /// <summary>
        /// Format time in hours as HH:MM:SS string using rounded seconds.
        /// </summary>
        public static string FormatTime(double hours)
        {
            if (double.IsNaN(hours) || double.IsInfinity(hours)) return "--:--:--";
            long totalSeconds = (long)Math.Round(hours * 3600);
            long secs = totalSeconds % 60;
            long totalMins = totalSeconds / 60;
            long mins = totalMins % 60;
            long hrs = totalMins / 60;
            return $"{hrs:D2}:{mins:D2}:{secs:D2}";
        }

        /// <summary>
        /// Format duration in hours as human-readable string using rounded seconds.
        /// </summary>
        public static string FormatDuration(double hours)
        {
            if (double.IsNaN(hours) || double.IsInfinity(hours)) return "--";
            return FormatDurationFromSeconds(hours * 3600);
        }

        /// <summary>
        /// Format duration in seconds as human-readable string.
        /// </summary>
        public static string FormatDurationFromSeconds(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return "--";
            long totalSecs = (long)Math.Round(seconds);
            long secs = totalSecs % 60;
            long totalMins = totalSecs / 60;
            long mins = totalMins % 60;
            long hrs = totalMins / 60;

            if (totalMins < 1)
                return $"{secs} sec";
            if (hrs < 1)
                return $"{mins} min {secs} sec";
            return $"{hrs} hr {mins} min {secs} sec";
        }
    }
}
