namespace ImtiazQueueSimulator.Simulation
{
    /// <summary>
    /// Generates random variates from various probability distributions.
    /// Supports Exponential, Uniform, Normal, and Deterministic distributions.
    /// </summary>
    public class DistributionGenerator
    {
        private readonly Random _random;
        private bool _hasSpareNormal = false;
        private double _spareNormal = 0;

        public DistributionGenerator(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Exponential distribution: X = -ln(1-U) / rate
        /// </summary>
        public double Exponential(double rate)
        {
            if (rate <= 0) throw new ArgumentException("Rate must be positive.");
            double u = _random.NextDouble();
            while (u == 0) u = _random.NextDouble(); // Avoid log(0)
            return -Math.Log(u) / rate;
        }

        /// <summary>
        /// Uniform distribution: X = a + U*(b-a)
        /// </summary>
        public double Uniform(double a, double b)
        {
            if (a >= b) throw new ArgumentException("a must be less than b.");
            return a + _random.NextDouble() * (b - a);
        }

        /// <summary>
        /// Deterministic distribution: X = constant
        /// </summary>
        public double Deterministic(double value)
        {
            return value;
        }

        /// <summary>
        /// Normal distribution using Box-Muller transform.
        /// Result is clamped to prevent negative values for service times.
        /// </summary>
        public double Normal(double mean, double stddev, bool clampPositive = true)
        {
            double result;
            if (_hasSpareNormal)
            {
                _hasSpareNormal = false;
                result = mean + stddev * _spareNormal;
            }
            else
            {
                double u, v, s;
                do
                {
                    u = 2.0 * _random.NextDouble() - 1.0;
                    v = 2.0 * _random.NextDouble() - 1.0;
                    s = u * u + v * v;
                } while (s >= 1.0 || s == 0);

                s = Math.Sqrt(-2.0 * Math.Log(s) / s);
                _spareNormal = v * s;
                _hasSpareNormal = true;
                result = mean + stddev * u * s;
            }

            if (clampPositive && result < 0.0001)
                result = 0.0001;

            return result;
        }

        /// <summary>
        /// Generate a variate based on distribution name and parameters.
        /// </summary>
        /// <param name="distribution">Distribution name: Exponential, Uniform, Normal, Deterministic</param>
        /// <param name="rate">Rate parameter (for Exponential: λ or μ)</param>
        /// <param name="param1">Additional parameter 1 (Uniform: min, Normal: stddev)</param>
        /// <param name="param2">Additional parameter 2 (Uniform: max)</param>
        public double Generate(string distribution, double rate, double param1 = 0, double param2 = 0)
        {
            double result;
            switch (distribution)
            {
                case "Exponential":
                    result = Exponential(rate);
                    break;
                case "Uniform":
                    double mean = 1.0 / rate;
                    double a, b;
                    if (param2 > param1 && param2 > 0)
                    {
                        a = param1;
                        b = param2;
                    }
                    else
                    {
                        a = param1 > 0 ? param1 : mean * 0.5;
                        b = param2 > 0 ? param2 : mean * 1.5;
                        if (a >= b) b = a + 0.001;
                    }
                    result = Uniform(a, b);
                    break;
                case "Normal":
                    double mu = 1.0 / rate;
                    double sigma = param1 > 0 ? param1 : mu * 0.2;
                    result = Normal(mu, sigma);
                    break;
                case "Deterministic":
                    result = 1.0 / rate;
                    break;
                default:
                    result = Exponential(rate);
                    break;
            }

            // Never return negative times
            if (result < 0.0001) result = 0.0001;
            return result;
        }

        /// <summary>
        /// Calculate E[S²] for a given distribution
        /// </summary>
        public static double GetSecondMoment(string distribution, double rate, double param1 = 0, double param2 = 0)
        {
            double mean = 1.0 / rate;

            switch (distribution)
            {
                case "Exponential":
                    // E[S²] = 2/μ² for exponential
                    return 2.0 / (rate * rate);

                case "Uniform":
                    double a = (param2 > param1 && param2 > 0) ? param1 : (param1 > 0 ? param1 : mean * 0.5);
                    double b = (param2 > param1 && param2 > 0) ? param2 : (param2 > 0 ? param2 : mean * 1.5);
                    if (a >= b) b = a + 0.001;
                    // E[S²] = (a² + ab + b²) / 3
                    return (a * a + a * b + b * b) / 3.0;

                case "Normal":
                    double sigma = param1 > 0 ? param1 : mean * 0.2;
                    // E[S²] = μ² + σ²
                    return mean * mean + sigma * sigma;

                case "Deterministic":
                    // E[S²] = (1/μ)² = mean²  (no variance)
                    return mean * mean;

                default:
                    return 2.0 / (rate * rate);
            }
        }

        /// <summary>
        /// Calculate variance for a given distribution
        /// </summary>
        public static double GetVariance(string distribution, double rate, double param1 = 0, double param2 = 0)
        {
            double mean = 1.0 / rate;

            switch (distribution)
            {
                case "Exponential":
                    return 1.0 / (rate * rate);

                case "Uniform":
                    double a = (param2 > param1 && param2 > 0) ? param1 : (param1 > 0 ? param1 : mean * 0.5);
                    double b = (param2 > param1 && param2 > 0) ? param2 : (param2 > 0 ? param2 : mean * 1.5);
                    if (a >= b) b = a + 0.001;
                    return (b - a) * (b - a) / 12.0;

                case "Normal":
                    double sigma = param1 > 0 ? param1 : mean * 0.2;
                    return sigma * sigma;

                case "Deterministic":
                    return 0;

                default:
                    return 1.0 / (rate * rate);
            }
        }
    }
}
