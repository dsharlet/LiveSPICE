using ComputerAlgebra;
using System;
using System.Linq;

namespace Circuit
{
    public static class AudioSimulationFactory
    {
        public static TransientSolution Solve(Circuit circuit, double sampleRate, int oversample)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (oversample <= 0)
                throw new ArgumentOutOfRangeException(nameof(oversample));

            return TransientSolution.Solve(circuit.Analyze(), (Real)1 / (sampleRate * oversample));
        }

        public static Simulation Create(Circuit circuit, double sampleRate, int oversample, int iterations)
        {
            return Create(circuit, Solve(circuit, sampleRate, oversample), oversample, iterations);
        }

        public static Simulation Create(Circuit circuit, TransientSolution solution, int oversample, int iterations)
        {
            if (circuit == null)
                throw new ArgumentNullException(nameof(circuit));
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));
            if (oversample <= 0)
                throw new ArgumentOutOfRangeException(nameof(oversample));
            if (iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(iterations));

            Expression inputExpression = circuit.Components.OfType<Input>().Select(i => i.In).SingleOrDefault();
            if (inputExpression == null)
                throw new NotSupportedException("Circuit has no inputs.");

            Expression outputExpression = 0;
            foreach (Speaker speaker in circuit.Components.OfType<Speaker>())
                outputExpression += speaker.Out;

            if (outputExpression.EqualsZero())
                throw new NotSupportedException("Circuit has no speaker outputs.");

            return new Simulation(solution)
            {
                Oversample = oversample,
                Iterations = iterations,
                Input = new[] { inputExpression },
                Output = new[] { outputExpression }
            };
        }
    }
}