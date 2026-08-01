namespace ImtiazQueueSimulator.Simulation
{
    using ImtiazQueueSimulator.Models;

    /// <summary>
    /// Core discrete-event simulation engine for all queueing models.
    /// Processes ARRIVAL and DEPARTURE events using a priority queue.
    /// Supports step-by-step execution for real-time visualization.
    /// </summary>
    public class SimulationEngine
    {
        // Configuration
        public double Lambda { get; set; }
        public double Mu { get; set; }
        public int NumServers { get; set; }
        public double SimulationTime { get; set; }
        public int MaxCustomers { get; set; }
        public string ModelName { get; set; } = "M/M/1";
        public string ArrivalDistribution { get; set; } = "Exponential";
        public string ServiceDistribution { get; set; } = "Exponential";
        public double ArrivalParam1 { get; set; }
        public double ArrivalParam2 { get; set; }
        public double ServiceParam1 { get; set; }
        public double ServiceParam2 { get; set; }
        public int? RandomSeed { get; set; }

        // State
        private PriorityEventQueue _eventQueue = new();
        private Queue<Customer> _waitingQueue = new();
        private List<Customer> _allCustomers = new();
        private List<QueueSnapshot> _snapshots = new();
        private Server[] _servers = Array.Empty<Server>();
        private DistributionGenerator _distGen = new();

        private double _currentTime = 0;
        private int _nextCustomerId = 1;
        private int _customersServed = 0;
        private int _totalArrivals = 0;
        private int _totalDepartures = 0;
        private bool _isRunning = false;
        private bool _isCompleted = false;

        // Area-under-curve for time-average calculations
        private double _areaQueueLength = 0;
        private double _areaSystemSize = 0;
        private double _lastEventTime = 0;
        private int _currentQueueLength = 0;
        private int _currentSystemSize = 0;
        private int _maxQueueLength = 0;
        private int _customersWhoWaited = 0;

        // Chart data
        private List<(double Time, int QueueLength)> _queueLengthData = new();
        private List<(double Time, int SystemSize)> _systemSizeData = new();
        private List<(double Time, int Arrivals, int Departures)> _arrDepData = new();

        // Events
        public event Action<Customer>? OnCustomerArrived;
        public event Action<Customer, int>? OnServiceStarted;
        public event Action<Customer>? OnCustomerDeparted;
        public event Action? OnSimulationComplete;
        public event Action<QueueSnapshot>? OnSnapshotRecorded;

        // Read-only accessors
        public IReadOnlyList<Customer> AllCustomers => _allCustomers;
        public IReadOnlyCollection<Customer> WaitingQueue => _waitingQueue;
        public Server[] Servers => _servers;
        public double CurrentTime => _currentTime;
        public bool IsRunning => _isRunning;
        public bool IsCompleted => _isCompleted;
        public int CurrentQueueLength => _currentQueueLength;
        public int CurrentSystemSize => _currentSystemSize;
        public int TotalArrivals => _totalArrivals;
        public int TotalDepartures => _totalDepartures;
        public int CustomersServed => _customersServed;
        public List<QueueSnapshot> Snapshots => _snapshots;

        /// <summary>
        /// Initialize the simulation
        /// </summary>
        public void Initialize()
        {
            SimEvent.ResetSequence();
            _eventQueue = new PriorityEventQueue();
            _waitingQueue = new Queue<Customer>();
            _allCustomers = new List<Customer>();
            _snapshots = new List<QueueSnapshot>();
            _queueLengthData = new List<(double, int)>();
            _systemSizeData = new List<(double, int)>();
            _arrDepData = new List<(double, int, int)>();

            _distGen = new DistributionGenerator(RandomSeed);

            _servers = new Server[NumServers];
            for (int i = 0; i < NumServers; i++)
                _servers[i] = new Server(i + 1);

            _currentTime = 0;
            _nextCustomerId = 1;
            _customersServed = 0;
            _totalArrivals = 0;
            _totalDepartures = 0;
            _areaQueueLength = 0;
            _areaSystemSize = 0;
            _lastEventTime = 0;
            _currentQueueLength = 0;
            _currentSystemSize = 0;
            _maxQueueLength = 0;
            _customersWhoWaited = 0;
            _isRunning = true;
            _isCompleted = false;

            // Record initial state
            RecordSnapshot("Simulation Start", "");
            _queueLengthData.Add((0, 0));
            _systemSizeData.Add((0, 0));
            _arrDepData.Add((0, 0, 0));

            // Schedule first arrival
            double firstArrival = GenerateInterarrivalTime();
            _eventQueue.Enqueue(new SimEvent(EventType.Arrival, firstArrival, _nextCustomerId));
        }

        /// <summary>
        /// Process the next event in the simulation.
        /// Returns true if simulation should continue.
        /// </summary>
        public bool ProcessNextEvent()
        {
            if (!_isRunning || (_eventQueue.IsEmpty && _currentSystemSize == 0))
            {
                CompleteSimulation();
                return false;
            }

            if (_eventQueue.IsEmpty)
            {
                CompleteSimulation();
                return false;
            }

            var evt = _eventQueue.Dequeue();

            // If an arrival occurs after simulation time cutoff, skip processing it
            if (evt.Type == EventType.Arrival && SimulationTime > 0 && evt.Time > SimulationTime)
            {
                if (_eventQueue.IsEmpty && _currentSystemSize == 0)
                {
                    CompleteSimulation();
                    return false;
                }
                return true;
            }

            // Update area-under-curve before changing state
            double timeDelta = evt.Time - _lastEventTime;
            _areaQueueLength += _currentQueueLength * timeDelta;
            _areaSystemSize += _currentSystemSize * timeDelta;
            _lastEventTime = evt.Time;
            _currentTime = evt.Time;

            if (evt.Type == EventType.Arrival)
                ProcessArrival(evt);
            else
                ProcessDeparture(evt);

            if (_eventQueue.IsEmpty && _currentSystemSize == 0)
            {
                CompleteSimulation();
                return false;
            }

            return true;
        }

        public List<string> GetWaitingCustomerIds()
        {
            return _waitingQueue.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// Process multiple events (batch mode)
        /// </summary>
        public int ProcessEvents(int count)
        {
            int processed = 0;
            for (int i = 0; i < count; i++)
            {
                if (!ProcessNextEvent()) break;
                processed++;
            }
            return processed;
        }

        /// <summary>
        /// Run entire simulation to completion
        /// </summary>
        public SimulationResult RunAll()
        {
            Initialize();
            while (ProcessNextEvent()) { }
            return GetResults();
        }

        private void ProcessArrival(SimEvent evt)
        {
            _totalArrivals++;
            var customer = new Customer(_nextCustomerId++, evt.Time);
            customer.QueueLengthOnArrival = _currentQueueLength;
            customer.SystemSizeOnArrival = _currentSystemSize;
            customer.Status = "Arrived";

            _currentSystemSize++;
            _allCustomers.Add(customer);

            // Find an idle server
            int idleServer = FindIdleServer();
            if (idleServer >= 0)
            {
                // Start service immediately
                StartService(customer, idleServer);
            }
            else
            {
                // Add to queue
                customer.Status = "Waiting";
                _waitingQueue.Enqueue(customer);
                _currentQueueLength++;
                if (_currentQueueLength > _maxQueueLength)
                    _maxQueueLength = _currentQueueLength;
                _customersWhoWaited++;

                RecordSnapshot("Joined Queue", customer.Name, "🟡");
            }

            _queueLengthData.Add((_currentTime, _currentQueueLength));
            _systemSizeData.Add((_currentTime, _currentSystemSize));
            _arrDepData.Add((_currentTime, _totalArrivals, _totalDepartures));

            OnCustomerArrived?.Invoke(customer);

            // Schedule next arrival (only if within simulation constraints)
            bool shouldScheduleNext = true;
            if (MaxCustomers > 0 && _totalArrivals >= MaxCustomers)
                shouldScheduleNext = false;

            if (shouldScheduleNext)
            {
                double interarrival = GenerateInterarrivalTime();
                double nextArrivalTime = _currentTime + interarrival;
                if (SimulationTime <= 0 || nextArrivalTime <= SimulationTime)
                {
                    _eventQueue.Enqueue(new SimEvent(EventType.Arrival, nextArrivalTime, _nextCustomerId));
                }
            }
        }

        private void ProcessDeparture(SimEvent evt)
        {
            var server = _servers[evt.ServerId - 1];
            var customer = FindCustomer(evt.CustomerId);
            if (customer == null) return;

            _totalDepartures++;
            _customersServed++;
            _currentSystemSize--;

            customer.DepartureTime = _currentTime;
            customer.TimeInSystem = _currentTime - customer.ArrivalTime;
            customer.Status = "Completed";

            server.EndService(_currentTime);

            RecordSnapshot("Departed", customer.Name, "🔴");

            OnCustomerDeparted?.Invoke(customer);

            // Serve next customer in queue
            if (_waitingQueue.Count > 0)
            {
                var next = _waitingQueue.Dequeue();
                _currentQueueLength--;
                StartService(next, evt.ServerId - 1);
            }

            _queueLengthData.Add((_currentTime, _currentQueueLength));
            _systemSizeData.Add((_currentTime, _currentSystemSize));
            _arrDepData.Add((_currentTime, _totalArrivals, _totalDepartures));
        }

        private void StartService(Customer customer, int serverIndex)
        {
            var server = _servers[serverIndex];

            customer.QueueLengthOnServiceStart = _currentQueueLength;
            customer.SystemSizeOnServiceStart = _currentSystemSize;
            customer.ServiceStartTime = _currentTime;
            customer.WaitingTime = _currentTime - customer.ArrivalTime;
            customer.AssignedServer = server.Id;
            customer.Status = "InService";

            double serviceTime = GenerateServiceTime();
            customer.ServiceTime = serviceTime;

            server.StartService(customer, _currentTime);

            double departureTime = _currentTime + serviceTime;
            _eventQueue.Enqueue(new SimEvent(EventType.Departure, departureTime, customer.Id, server.Id));

            RecordSnapshot("Service Started", $"{customer.Name} → {server.Name}", "🔵");

            OnServiceStarted?.Invoke(customer, server.Id);
        }

        private int FindIdleServer()
        {
            for (int i = 0; i < _servers.Length; i++)
            {
                if (_servers[i].IsIdle) return i;
            }
            return -1;
        }

        private Customer? FindCustomer(int customerId)
        {
            for (int i = _allCustomers.Count - 1; i >= 0; i--)
            {
                if (_allCustomers[i].Id == customerId) return _allCustomers[i];
            }
            return null;
        }

        private double GenerateInterarrivalTime()
        {
            return _distGen.Generate(ArrivalDistribution, Lambda, ArrivalParam1, ArrivalParam2);
        }

        private double GenerateServiceTime()
        {
            return _distGen.Generate(ServiceDistribution, Mu, ServiceParam1, ServiceParam2);
        }

        private void RecordSnapshot(string eventDesc, string customerInfo, string icon = "⚪")
        {
            int busy = 0;
            foreach (var s in _servers)
                if (!s.IsIdle) busy++;

            var snapshot = new QueueSnapshot(
                _currentTime, eventDesc, customerInfo,
                _currentQueueLength, _currentSystemSize, busy, icon);

            _snapshots.Add(snapshot);
            OnSnapshotRecorded?.Invoke(snapshot);
        }

        private void CompleteSimulation()
        {
            if (_isCompleted) return;
            _isRunning = false;
            _isCompleted = true;

            // Update final area-under-curve
            double finalTime = _currentTime > 0 ? _currentTime : SimulationTime;
            double timeDelta = finalTime - _lastEventTime;
            _areaQueueLength += _currentQueueLength * timeDelta;
            _areaSystemSize += _currentSystemSize * timeDelta;

            // Finalize server utilization
            foreach (var server in _servers)
                server.Finalize(finalTime);

            RecordSnapshot("Simulation Complete", "", "✅");

            OnSimulationComplete?.Invoke();
        }

        /// <summary>
        /// Get comprehensive simulation results
        /// </summary>
        public SimulationResult GetResults()
        {
            double totalTime = _currentTime > 0 ? _currentTime : SimulationTime;
            double effectiveLambda = _totalArrivals > 0 ? _totalArrivals / totalTime : 0;

            var result = new SimulationResult
            {
                ModelName = ModelName,
                Lambda = Lambda,
                Mu = Mu,
                NumServers = NumServers,
                SimulationTime = totalTime,
                ArrivalDistribution = ArrivalDistribution,
                ServiceDistribution = ServiceDistribution,
                RandomSeed = RandomSeed,
                TotalCustomers = _totalArrivals,
                CustomersServed = _customersServed,
                CustomersWhoWaited = _customersWhoWaited,
                MaxQueueLength = _maxQueueLength,
                EffectiveLambda = effectiveLambda,
                AllCustomers = new List<Customer>(_allCustomers),
                Snapshots = new List<QueueSnapshot>(_snapshots),
                QueueLengthOverTime = new List<(double, int)>(_queueLengthData),
                SystemSizeOverTime = new List<(double, int)>(_systemSizeData),
                ArrivalDepartureOverTime = new List<(double, int, int)>(_arrDepData)
            };

            // Time-average metrics (area-under-curve method)
            if (totalTime > 0)
            {
                result.SimLq = _areaQueueLength / totalTime;
                result.SimL = _areaSystemSize / totalTime;
            }

            // Customer-average metrics
            if (_customersServed > 0)
            {
                double totalWait = 0, totalSystem = 0;
                foreach (var c in _allCustomers)
                {
                    if (c.Status == "Completed")
                    {
                        totalWait += c.WaitingTime;
                        totalSystem += c.TimeInSystem;
                    }
                }
                result.SimWq = totalWait / _customersServed;
                result.SimW = totalSystem / _customersServed;
            }

            // Server utilization
            if (_servers.Length > 0)
            {
                result.ServerUtilizations = new double[_servers.Length];
                double totalUtil = 0;
                for (int i = 0; i < _servers.Length; i++)
                {
                    result.ServerUtilizations[i] = _servers[i].GetUtilization(totalTime);
                    totalUtil += result.ServerUtilizations[i];
                }
                result.SimRho = totalUtil / _servers.Length;
            }

            // Probability of waiting
            if (_totalArrivals > 0)
                result.ProbabilityOfWaiting = (double)_customersWhoWaited / _totalArrivals;

            // Little's Law validation
            if (effectiveLambda > 0)
            {
                result.LittleLawLq = effectiveLambda * result.SimWq;
                result.LittleLawL = effectiveLambda * result.SimW;
            }

            // Get analytical results based on model
            AddAnalyticalResults(result);

            return result;
        }

        private void AddAnalyticalResults(SimulationResult result)
        {
            SimulationResult? analytical = null;

            switch (ModelName)
            {
                case "M/M/1":
                    analytical = AnalyticalSolver.SolveMM1(Lambda, Mu);
                    break;
                case "M/M/N":
                    analytical = AnalyticalSolver.SolveMMN(Lambda, Mu, NumServers);
                    break;
                case "M/G/1":
                    analytical = AnalyticalSolver.SolveMG1(Lambda, Mu, ServiceDistribution, ServiceParam1, ServiceParam2);
                    break;
                case "G/G/1":
                    analytical = AnalyticalSolver.SolveGG1(Lambda, Mu, ArrivalDistribution, ServiceDistribution,
                        ArrivalParam1, ArrivalParam2, ServiceParam1, ServiceParam2);
                    break;
                // M/G/N and G/G/N: no closed-form; simulation-only
            }

            if (analytical != null)
            {
                result.AnalyticalLq = analytical.AnalyticalLq;
                result.AnalyticalL = analytical.AnalyticalL;
                result.AnalyticalWq = analytical.AnalyticalWq;
                result.AnalyticalW = analytical.AnalyticalW;
                result.AnalyticalRho = analytical.AnalyticalRho;
                result.AnalyticalP0 = analytical.AnalyticalP0;
            }
        }

        /// <summary>
        /// Stop the simulation
        /// </summary>
        public void Stop()
        {
            CompleteSimulation();
        }
    }
}
