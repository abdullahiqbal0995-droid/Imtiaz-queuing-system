namespace ImtiazQueueSimulator.Simulation
{
    using ImtiazQueueSimulator.Models;

    /// <summary>
    /// Computes closed-form analytical results for queueing models.
    /// Supports M/M/1, M/M/N, M/G/1, and G/G/1 (Kingman approximation).
    /// </summary>
    public static class AnalyticalSolver
    {
        /// <summary>
        /// Solve M/M/1 model
        /// </summary>
        public static SimulationResult SolveMM1(double lambda, double mu)
        {
            var result = new SimulationResult
            {
                ModelName = "M/M/1",
                Lambda = lambda,
                Mu = mu,
                NumServers = 1
            };

            double rho = lambda / mu;
            result.AnalyticalRho = rho;

            if (rho >= 1)
            {
                // Unstable — set to NaN
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                result.AnalyticalP0 = double.NaN;
                return result;
            }

            // P0 = 1 - ρ
            result.AnalyticalP0 = 1 - rho;

            // Lq = λ² / (μ(μ - λ)) = ρ² / (1 - ρ)
            result.AnalyticalLq = (rho * rho) / (1 - rho);

            // L = λ / (μ - λ) = ρ / (1 - ρ)
            result.AnalyticalL = rho / (1 - rho);

            // Wq = λ / (μ(μ - λ)) = ρ / (μ(1 - ρ))
            result.AnalyticalWq = rho / (mu * (1 - rho));

            // W = 1 / (μ - λ)
            result.AnalyticalW = 1.0 / (mu - lambda);

            return result;
        }

        /// <summary>
        /// Solve M/M/N model using Erlang-C formulas
        /// </summary>
        public static SimulationResult SolveMMN(double lambda, double mu, int n)
        {
            var result = new SimulationResult
            {
                ModelName = "M/M/N",
                Lambda = lambda,
                Mu = mu,
                NumServers = n
            };

            double rho = lambda / (n * mu);
            result.AnalyticalRho = rho;

            if (rho >= 1)
            {
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                result.AnalyticalP0 = double.NaN;
                return result;
            }

            // Calculate P0
            double sumPart = 0;
            double a = lambda / mu; // offered load
            for (int k = 0; k < n; k++)
            {
                sumPart += Math.Pow(a, k) / Factorial(k);
            }
            double lastTerm = Math.Pow(a, n) / (Factorial(n) * (1 - rho));
            double p0 = 1.0 / (sumPart + lastTerm);
            result.AnalyticalP0 = p0;

            // Erlang C = P(wait) = C(N, a)
            double erlangC = (Math.Pow(a, n) / Factorial(n)) * (1.0 / (1 - rho)) * p0;

            // Lq = C(N,a) * ρ / (1 - ρ)
            result.AnalyticalLq = erlangC * rho / (1 - rho);

            // L = Lq + a
            result.AnalyticalL = result.AnalyticalLq + a;

            // Wq = Lq / λ
            result.AnalyticalWq = result.AnalyticalLq / lambda;

            // W = Wq + 1/μ
            result.AnalyticalW = result.AnalyticalWq + 1.0 / mu;

            return result;
        }

        /// <summary>
        /// Solve M/G/1 model using Pollaczek-Khinchine formula
        /// </summary>
        public static SimulationResult SolveMG1(double lambda, double mu,
            string serviceDistribution, double param1 = 0, double param2 = 0)
        {
            var result = new SimulationResult
            {
                ModelName = "M/G/1",
                Lambda = lambda,
                Mu = mu,
                NumServers = 1,
                ServiceDistribution = serviceDistribution
            };

            double es = 1.0 / mu; // E[S]
            double rho = lambda * es;
            result.AnalyticalRho = rho;

            if (rho >= 1)
            {
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                return result;
            }

            // E[S²] depends on service distribution
            double es2 = DistributionGenerator.GetSecondMoment(serviceDistribution, mu, param1, param2);

            // Pollaczek-Khinchine: Wq = λE[S²] / (2(1-ρ))
            result.AnalyticalWq = (lambda * es2) / (2.0 * (1.0 - rho));

            // Lq = λ × Wq
            result.AnalyticalLq = lambda * result.AnalyticalWq;

            // W = Wq + E[S]
            result.AnalyticalW = result.AnalyticalWq + es;

            // L = λ × W
            result.AnalyticalL = lambda * result.AnalyticalW;

            return result;
        }

        /// <summary>
        /// Solve G/G/1 model using Kingman's approximation
        /// </summary>
        public static SimulationResult SolveGG1(double lambda, double mu,
            string arrivalDistribution, string serviceDistribution,
            double arrParam1 = 0, double arrParam2 = 0,
            double svcParam1 = 0, double svcParam2 = 0)
        {
            var result = new SimulationResult
            {
                ModelName = "G/G/1",
                Lambda = lambda,
                Mu = mu,
                NumServers = 1,
                ArrivalDistribution = arrivalDistribution,
                ServiceDistribution = serviceDistribution
            };

            double ea = 1.0 / lambda; // E[A] = mean interarrival time
            double es = 1.0 / mu;     // E[S] = mean service time
            double rho = lambda / mu;
            result.AnalyticalRho = rho;

            if (rho >= 1)
            {
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                return result;
            }

            // Variance of interarrival and service
            double varA = DistributionGenerator.GetVariance(arrivalDistribution, lambda, arrParam1, arrParam2);
            double varS = DistributionGenerator.GetVariance(serviceDistribution, mu, svcParam1, svcParam2);

            // Coefficients of variation
            double ca = Math.Sqrt(varA) / ea;
            double cs = Math.Sqrt(varS) / es;

            // Kingman: Wq ≈ (ρ / (1-ρ)) × ((Ca² + Cs²) / 2) × E[S]
            result.AnalyticalWq = (rho / (1 - rho)) * ((ca * ca + cs * cs) / 2.0) * es;

            result.AnalyticalLq = lambda * result.AnalyticalWq;
            result.AnalyticalW = result.AnalyticalWq + es;
            result.AnalyticalL = lambda * result.AnalyticalW;

            return result;
        }

        /// <summary>
        /// Solve G/G/N model using Allen-Cunneen Approximation.
        /// </summary>
        public static SimulationResult SolveGGN(double lambda, double mu, int n,
            string arrivalDistribution, string serviceDistribution,
            double arrParam1 = 0, double arrParam2 = 0,
            double svcParam1 = 0, double svcParam2 = 0)
        {
            var result = new SimulationResult
            {
                ModelName = "G/G/N",
                Lambda = lambda,
                Mu = mu,
                NumServers = n,
                ArrivalDistribution = arrivalDistribution,
                ServiceDistribution = serviceDistribution
            };

            double rho = lambda / (n * mu);
            result.AnalyticalRho = rho;

            if (rho >= 1)
            {
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                result.AnalyticalP0 = double.NaN;
                return result;
            }

            // Coefficients of variation
            double ca = GetCV(arrivalDistribution, lambda, arrParam1, arrParam2);
            double cs = GetCV(serviceDistribution, mu, svcParam1, svcParam2);

            // Obtain standard M/M/N waiting time
            var mmnResult = SolveMMN(lambda, mu, n);
            double wq_MMN = mmnResult.AnalyticalWq;

            if (double.IsNaN(wq_MMN) || double.IsInfinity(wq_MMN))
            {
                result.AnalyticalLq = double.NaN;
                result.AnalyticalL = double.NaN;
                result.AnalyticalWq = double.NaN;
                result.AnalyticalW = double.NaN;
                return result;
            }

            // Allen-Cunneen waiting time: Wq_GGN = ((Ca² + Cs²)/2) * Wq_MMN
            double wq = ((ca * ca + cs * cs) / 2.0) * wq_MMN;

            result.AnalyticalWq = wq;
            result.AnalyticalLq = lambda * wq;
            result.AnalyticalW = wq + (1.0 / mu);
            result.AnalyticalL = lambda * result.AnalyticalW;
            result.AnalyticalP0 = mmnResult.AnalyticalP0; // approx P0 same as M/M/N

            return result;
        }

        private static double GetCV(string distribution, double rate, double param1 = 0, double param2 = 0)
        {
            if (distribution == "Exponential") return 1.0;
            if (distribution == "Deterministic") return 0.0;
            
            double mean = 1.0 / rate;
            if (mean <= 0) return 0.0;
            
            double variance = DistributionGenerator.GetVariance(distribution, rate, param1, param2);
            double stdDev = Math.Sqrt(variance);
            return stdDev / mean;
        }

        /// <summary>
        /// Calculate factorial (with overflow protection)
        /// </summary>
        private static double Factorial(int n)
        {
            if (n <= 1) return 1;
            double f = 1;
            for (int i = 2; i <= n; i++)
                f *= i;
            return f;
        }

        /// <summary>
        /// Get probability of n customers in M/M/1 system
        /// </summary>
        public static double MM1ProbN(double rho, int n)
        {
            if (rho >= 1) return double.NaN;
            return (1 - rho) * Math.Pow(rho, n);
        }
    }
}
