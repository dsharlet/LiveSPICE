using ComputerAlgebra;

namespace Circuit
{
    /// <summary>
    /// Exception thrown when a simulation does not converge.
    /// </summary>
    public class SimulationDivergedException : FailedToConvergeException
    {
        private long at;
        /// <summary>
        /// Sample number at which the simulation diverged.
        /// </summary>
        public long At { get { return at; } }

        public SimulationDivergedException(string Message, long At) : base(Message) { at = At; }

        public SimulationDivergedException(int At) : base("Simulation diverged.") { at = At; }
    }
}
