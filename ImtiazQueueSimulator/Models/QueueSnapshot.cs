namespace ImtiazQueueSimulator.Models
{
    /// <summary>
    /// Records the state of the queue system at a point in time.
    /// Used for queue history and timeline display.
    /// </summary>
    public class QueueSnapshot
    {
        public double Time { get; set; }
        public string EventDescription { get; set; } = "";
        public string CustomerInfo { get; set; } = "";
        public int QueueLength { get; set; }
        public int CustomersInSystem { get; set; }
        public int BusyServers { get; set; }
        public string EventIcon { get; set; } = "⚪";

        public QueueSnapshot(double time, string eventDesc, string customerInfo,
            int queueLen, int inSystem, int busyServers, string icon = "⚪")
        {
            Time = time;
            EventDescription = eventDesc;
            CustomerInfo = customerInfo;
            QueueLength = queueLen;
            CustomersInSystem = inSystem;
            BusyServers = busyServers;
            EventIcon = icon;
        }

        public string FormattedTime => Customer.FormatTime(Time);
    }
}
