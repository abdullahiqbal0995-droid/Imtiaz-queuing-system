# 🛒 Imtiaz Queue Analyzer

A professional, university-level supermarket checkout simulation and analysis tool designed for Queueing Theory & Operations Research applications. It bridges stochastic discrete-event simulation with classical mathematical queueing models.

---

## 📐 Table of Contents
1. [Overview](#-overview)
2. [Key Features](#-key-features)
3. [Supported Queueing Models](#-supported-queueing-models)
4. [Queueing Terminology](#-queueing-terminology)
5. [Mathematical & Simulation Logic](#-mathematical--simulation-logic)
6. [Precision & Consistency Controls](#-precision--consistency-controls)
7. [How to Use the Simulator](#-how-to-use-the-simulator)
8. [Technologies & Architecture](#-technologies--architecture)

---

## 🌐 Overview

The **Imtiaz Queue Analyzer** simulates real-world supermarket checkout dynamics. Instead of relying purely on static, asymptotic calculations, the system generates individual customer entities and models their arrivals, waiting queues, server allocations, and departures using stochastic distributions. It then gathers simulation-based metrics and directly compares them with exact analytical solvers or approximations.

---

## ✨ Key Features

- **Live Discrete-Event Simulation**: Watch customers move through a visual checkout process (Arrival ➔ Queue ➔ Server Allocation ➔ Service ➔ Departure) in real time.
- **6 Classical Queuing Models**: Built-in support for single and multi-server queueing networks with Poisson or general arrival/service configurations.
- **Interactive Live Dashboard**: Displays real-time metrics including average waiting time ($W_q$), system time ($W$), queue length ($L_q$), system size ($L$), and server utilization ($\rho$).
- **Customer Records Panel**: Comprehensive tabular view with search/filter features showing complete history, cashier assignments, and precise timing logs.
- **Comparison Panel**: Displays side-by-side grids mapping analytical results directly against empirical simulation outcomes.
- **Custom GDI+ Charts**: Responsive, pixel-perfect charts rendering customer arrival patterns, server utilization, queue distributions, and wait time histograms.
- **Detailed Report Generator**: Automatically exports text-based simulation summaries and queue timelines.

---

## 📋 Supported Queueing Models

The application models six standard Kendall's notation queue configurations:

### 1. M/M/1
- **Process**: Poisson Arrivals, Exponential Service, 1 Cashier.
- **Formulas**:
  - Server Utilization: $\rho = \lambda / \mu$
  - Probability of empty system: $P_0 = 1 - \rho$
  - Average Queue Length: $L_q = \frac{\rho^2}{1-\rho} = \frac{\lambda^2}{\mu(\mu - \lambda)}$
  - Average System Size: $L = \frac{\rho}{1-\rho} = \frac{\lambda}{\mu - \lambda}$
  - Average Queue Wait: $W_q = \frac{L_q}{\lambda} = \frac{\lambda}{\mu(\mu - \lambda)}$
  - Average System Time: $W = \frac{L}{\lambda} = \frac{1}{\mu - \lambda}$
  - *Condition*: $\lambda < \mu$ ($\rho < 1$).

### 2. M/M/N
- **Process**: Poisson Arrivals, Exponential Service, N Parallel Cashiers.
- **Formulas**:
  - Offered Load: $a = \lambda / \mu$
  - Server Utilization: $\rho = \lambda / (N\mu)$
  - Empty System Probability: $P_0 = \left[ \sum_{k=0}^{N-1} \frac{a^k}{k!} + \frac{a^N}{N!(1 - \rho)} \right]^{-1}$
  - Probability of Waiting (Erlang-C): $P(wait) = \frac{a^N \cdot P_0}{N!(1 - \rho)}$
  - Average Queue Length: $L_q = P(wait) \cdot \frac{\rho}{1-\rho}$
  - Average Queue Wait: $W_q = L_q / \lambda$
  - Average System Time: $W = W_q + 1/\mu$
  - Average System Size: $L = \lambda W$
  - *Condition*: $\rho < 1$.

### 3. M/G/1
- **Process**: Poisson Arrivals, General Service Distribution, 1 Cashier.
- **Formulas** (Pollaczek-Khinchine Formula):
  - Server Utilization: $\rho = \lambda E[S]$
  - Average Queue Length: $L_q = \frac{\lambda^2 E[S^2]}{2(1-\rho)}$
  - Average Queue Wait: $W_q = L_q / \lambda$
  - Average System Time: $W = W_q + E[S]$
  - Average System Size: $L = \lambda W$
  - *Note*: $E[S]$ is mean service time, and $E[S^2]$ is the second moment of service time. Allows modeling uniform or constant cashier processing times.

### 4. M/G/N
- **Process**: Poisson Arrivals, General Service Distribution, N Parallel Cashiers.
- **Method**: There is no simple closed-form mathematical formula. The system uses discrete-event simulation to approximate $L_q$, $L$, $W_q$, $W$, and $\rho$.

### 5. G/G/1
- **Process**: General Arrivals, General Service Distribution, 1 Cashier.
- **Formulas** (Kingman's Heavy-Traffic Approximation):
  - Average Queue Wait: $W_q \approx \left(\frac{\rho}{1-\rho}\right) \left(\frac{C_a^2 + C_s^2}{2}\right) E[S]$
  - Average Queue Length: $L_q \approx \lambda W_q$
  - Average System Time: $W = W_q + E[S]$
  - Average System Size: $L = \lambda W$
  - *Note*: $C_a$ and $C_s$ represent coefficients of variation of inter-arrival and service times respectively.

### 6. G/G/N
- **Process**: General Arrivals, General Service Distribution, N Parallel Cashiers.
- **Method**: The most complex model; simulated dynamically via discrete-event queues.

---

## 📖 Queueing Terminology

- **$\lambda$ (Arrival Rate)**: Average rate at which shoppers arrive at the checkout counters per unit time.
- **$\mu$ (Service Rate)**: Average rate at which a single cashier processes checkouts.
- **$N$ (Servers)**: Total number of active checkout cashiers.
- **$\rho$ (Utilization)**: Fraction of time cashiers are busy.
- **$L_q$ (Queue Length)**: Average count of shoppers waiting in line.
- **$L$ (System Size)**: Average total count of shoppers in the checkout zone (Queue + In Service).
- **$W_q$ (Wait Time)**: Average duration shoppers spend waiting in the queue line.
- **$W$ (System Time)**: Average total duration shoppers spend in the checkout zone (Wait Time + Checkout Service Duration).
- **Little's Law**: Fundamental queueing equation showing $L = \lambda W$ and $L_q = \lambda W_q$.

---

## 🛠 Mathematical & Simulation Logic

To simulate stochastic behavior, the engine uses **Inverse Transform Sampling** to draw inter-arrival times and service durations.

### Arrival Time Generation
For exponential (Markovian) inter-arrivals with rate $\lambda$:
$$IA = -\frac{\ln(U)}{\lambda}$$
where $U$ is a uniformly distributed random variable $U \sim \text{Uniform}(0, 1)$.
The absolute arrival time for customer $i$ is calculated iteratively:
$$\text{ArrivalTime}_i = \text{ArrivalTime}_{i-1} + IA_i$$

### Service Time Generation
For exponential service times with service rate $\mu$:
$$S = -\frac{\ln(U)}{\mu}$$
For general distributions (Uniform, Normal), the service time is drawn based on the configured boundaries and standard deviation.

---

## ⏱ Precision & Consistency Controls

To resolve fractional-second rounding errors across UI controls and textual logs, the simulator implements **Rounded-Second Precision Logic**:
1. All raw time properties are stored as double values representing hours.
2. Timestamps are rounded to the nearest second for visual formatting:
   - $\text{ArrivalSeconds} = \text{Round}(\text{ArrivalTime} \times 3600)$
   - $\text{SvcStartSeconds} = \text{Round}(\text{ServiceStartTime} \times 3600)$
   - $\text{DepartureSeconds} = \text{Round}(\text{DepartureTime} \times 3600)$
3. Customer durations are computed directly from these rounded integer seconds:
   - $\text{WaitingTime (Wq)} = \text{SvcStartSeconds} - \text{ArrivalSeconds}$
   - $\text{ServiceDuration} = \text{DepartureSeconds} - \text{SvcStartSeconds}$
   - $\text{SystemTime (W)} = \text{DepartureSeconds} - \text{ArrivalSeconds}$

This guarantees that the following identities hold mathematically true for **every single customer row** in the application:
$$\text{W} = \text{Wq} + \text{Service}$$
$$\text{Wq} = \text{Svc Start} - \text{Arrival}$$
$$\text{Service} = \text{Departure} - \text{Svc Start}$$

---

## 🚀 How to Use the Simulator

1. **Configure Parameters**: Navigate to the **Simulation** panel. Choose your Queueing Model, customer count, arrival rate ($\lambda$), service rate ($\mu$), number of active cashiers ($N$), and distributions.
2. **Preset Scenarios**: Open the **Settings & Presets** panel to load standard supermarket traffic scenarios:
   - *Normal Hours* ($\lambda = 10, \mu = 12, N = 2$) — stable low-traffic conditions.
   - *Peak Hours* ($\lambda = 20, \mu = 12, N = 3$) — high-traffic evening rush.
   - *Weekend Rush* ($\lambda = 25, \mu = 12, N = 4$) — maximum shopper activity.
   - *Overloaded* ($\lambda = 30, \mu = 12, N = 2$) — unstable condition where $\rho \ge 1$.
3. **Run**: Click **Start Simulation**. Customers are queued and serviced on the live tracking visualizer.
4. **Compare**: Open the **Comparison** panel to inspect simulation metrics alongside theoretical queue formulas.
5. **View History**: Look through **Queue History** for chronological snapshots of the checkout states.

---

## 💻 Technologies & Architecture

- **Runtime**: .NET 8.0 Windows Forms (C#)
- **GUI Controls**: Custom GDI+ double-buffered controls for responsive rendering and anti-aliased charts.
- **Simulation Engine**: Event-driven queuing architecture powered by a customized min-heap priority queue (`PriorityEventQueue`) managing chronological system state transitions (`ArrivalEvent`, `ServiceStartEvent`, `DepartureEvent`).
