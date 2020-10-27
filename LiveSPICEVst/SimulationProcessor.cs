using Circuit;
using ComputerAlgebra;
using System.Collections.ObjectModel;
using System.Linq;

namespace LiveSPICEVst
{
    /// <summary>
    /// Simple wrapper around IPotControl to make UI integration easier
    /// </summary>
    public class PotWrapper
    {
        public Circuit.Component Pot { get; set; }

        public string Name
        {
            get
            {
                return Pot.Name;
            }
        }

        public double PotValue
        {
            get
            {
                return (Pot as IPotControl).PotValue;
            }

            set
            {
                (Pot as IPotControl).PotValue = value;

                IsDirty = true;
            }
        }

        public bool IsDirty { get; set; }
    }

    /// <summary>
    /// Manages single-channel audio circuit simulation
    /// </summary>
    public class SimulationProcessor
    {
        public ObservableCollection<PotWrapper> Pots { get; private set; }

        public double SampleRate
        {
            get { return sampleRate; }
            set
            {
                sampleRate = value;

                needRebuild = true;
            }
        }

        public int Oversample
        {
            get { return oversample; }
            set
            {
                oversample = value;

                needRebuild = true;
            }
        }

        public int Iterations
        {
            get { return iterations; }
            set
            {
                iterations = value;
                needRebuild = true;
            }
        }

        double sampleRate = 44100;
        int oversample = 2;
        int iterations = 8;

        Circuit.Circuit circuit = null;
        Simulation simulation = null;
        bool needRebuild = false;

        public SimulationProcessor()
        {
            Pots = new ObservableCollection<PotWrapper>();
        }

        public void SetCircuit(Circuit.Circuit circuit)
        {
            this.circuit = circuit;

            Pots.Clear();

            foreach (Circuit.Component i in circuit.Components)
            {
                if (i is IPotControl)
                {
                    Pots.Add(new PotWrapper { Pot = i });
                }
            }

            needRebuild = true;
        }

        public void RunSimulation(double[] audioInput, double[] audioOutput)
        {
            if (circuit == null)
            {
                audioInput.CopyTo(audioOutput, 0);
            }
            else
            {

                bool needUpdate = false;

                foreach (PotWrapper pot in Pots)
                {
                    if (pot.IsDirty)
                    {
                        needUpdate = true;

                        pot.IsDirty = false;
                    }
                }

                if (needUpdate || needRebuild)
                {
                    UpdateSimulation(needRebuild);

                    needRebuild = false;
                }

                simulation.Run(audioInput, audioOutput);
            }
        }

        void UpdateSimulation(bool rebuild)
        {
            Analysis analysis = circuit.Analyze();
            TransientSolution ts = TransientSolution.Solve(analysis, (Real)1 / (sampleRate * oversample));

            if (rebuild)
            {
                Expression outputExpression = 0;

                foreach (Circuit.Component i in circuit.Components)
                {
                    if (i is Speaker)
                    {
                        outputExpression += (i as Speaker).V;  // Add the voltage drop across this speaker to the output expression.
                    }
                }

                simulation = new Simulation(ts)
                {
                    Oversample = oversample,
                    Iterations = iterations,
                    Input = new[] { circuit.Components.OfType<Input>().Select(i => Expression.Parse(i.Name + "[t]")).DefaultIfEmpty("V[t]").SingleOrDefault() },
                    Output = new[] { outputExpression }
                };
            }
            else
            {
                simulation.Solution = ts;
            }
        }
    }
}
