using System.Collections.Generic;
using ComputerAlgebra;
using Util.Cancellation;

namespace Circuit
{
    public abstract record SimulationSettings(int SampleRate, int Oversample)
    {
        public Expression TimeStep => (Real)1 / SampleRate;
    }

    public interface ISimulationBuilder<in TSettings>
        where TSettings : SimulationSettings
    {
        Simulation Build(
            Analysis mna,
            TSettings settings,
            IEnumerable<Expression> inputs,
            IEnumerable<Expression> outputs,
            ICancellationStrategy cancellationStrategy);

        Simulation Build(TransientSolution solution,
            TSettings settings,
            IEnumerable<Expression> inputs,
            IEnumerable<Expression> outputs,
            ICancellationStrategy cancellationStrategy);
    }
}
