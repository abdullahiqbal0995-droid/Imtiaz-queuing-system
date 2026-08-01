namespace ImtiazQueueSimulator.Models
{
    /// <summary>
    /// Type of simulation event
    /// </summary>
    public enum EventType
    {
        Arrival,
        Departure
    }

    /// <summary>
    /// Represents a discrete event in the simulation.
    /// Implements IComparable for priority queue ordering by time.
    /// </summary>
    public class SimEvent : IComparable<SimEvent>
    {
        public EventType Type { get; set; }
        public double Time { get; set; }
        public int CustomerId { get; set; }
        public int ServerId { get; set; }

        private static int _sequenceCounter = 0;
        private int _sequence;

        public SimEvent(EventType type, double time, int customerId, int serverId = -1)
        {
            Type = type;
            Time = time;
            CustomerId = customerId;
            ServerId = serverId;
            _sequence = _sequenceCounter++;
        }

        /// <summary>
        /// Compare events by time, breaking ties by sequence number (FIFO)
        /// </summary>
        public int CompareTo(SimEvent? other)
        {
            if (other == null) return -1;
            int cmp = Time.CompareTo(other.Time);
            if (cmp != 0) return cmp;
            // Departures before arrivals at the same time
            if (Type != other.Type)
                return Type == EventType.Departure ? -1 : 1;
            return _sequence.CompareTo(other._sequence);
        }

        public static void ResetSequence()
        {
            _sequenceCounter = 0;
        }
    }
}
