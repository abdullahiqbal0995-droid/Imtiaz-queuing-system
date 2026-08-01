namespace ImtiazQueueSimulator.Models
{
    /// <summary>
    /// Represents a checkout cashier/server in the supermarket.
    /// Tracks utilization and current service state.
    /// </summary>
    public class Server
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsIdle { get; set; } = true;
        public Customer? CurrentCustomer { get; set; }

        public double BusyTime { get; set; }
        public double IdleTime { get; set; }
        public double LastEventTime { get; set; }

        public double ServiceStartTime { get; set; }
        public double ServiceEndTime { get; set; }

        public int CustomersServed { get; set; }

        public Server(int id)
        {
            Id = id;
            Name = $"Cashier {id:D2}";
        }

        /// <summary>
        /// Calculate utilization as fraction of total time spent busy
        /// </summary>
        public double GetUtilization(double totalTime)
        {
            if (totalTime <= 0) return 0;
            return BusyTime / totalTime;
        }

        /// <summary>
        /// Start serving a customer
        /// </summary>
        public void StartService(Customer customer, double currentTime)
        {
            if (IsIdle)
            {
                IdleTime += currentTime - LastEventTime;
            }
            IsIdle = false;
            CurrentCustomer = customer;
            ServiceStartTime = currentTime;
            LastEventTime = currentTime;
        }

        /// <summary>
        /// End service for current customer
        /// </summary>
        public void EndService(double currentTime)
        {
            if (!IsIdle)
            {
                BusyTime += currentTime - LastEventTime;
            }
            IsIdle = true;
            CurrentCustomer = null;
            ServiceEndTime = currentTime;
            CustomersServed++;
            LastEventTime = currentTime;
        }

        /// <summary>
        /// Finalize time tracking at end of simulation
        /// </summary>
        public void Finalize(double endTime)
        {
            if (IsIdle)
                IdleTime += endTime - LastEventTime;
            else
                BusyTime += endTime - LastEventTime;
            LastEventTime = endTime;
        }
    }
}
