using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Circuit;
using ComputerAlgebra;

namespace LiveSPICEVst
{
    public class ComponentWrapper
    {
        public string Name { get; set; }
        public bool NeedUpdate { get; set; }
        public bool NeedRebuild { get; set; }
    }

    /// <summary>
    /// Simple wrapper around IPotControl to make UI integration easier
    /// </summary>
    public class PotWrapper : ComponentWrapper
    {
        IPotControl pot = null;

        public PotWrapper(IPotControl pot, string name)
        {
            this.pot = pot;
            this.Name = name;
        }

        public double PotValue
        {
            get
            {
                return pot.PotValue;
            }

            set
            {
                pot.PotValue = value;

                NeedUpdate = true;
            }
        }
    }

    /// <summary>
    /// Simple wrapper around IButtonControl to make UI integration easier
    /// </summary>
    public class ButtonWrapper : ComponentWrapper
    {
        List<IButtonControl> buttons = new List<IButtonControl>();
        bool engaged = false;

        public ButtonWrapper(string name)
        {
            this.Name = name;
        }

        public bool Engaged
        {
            get
            {
                return engaged;
            }

            set
            {
                if (value != engaged)
                {
                    engaged = !engaged;

                    foreach (IButtonControl button in buttons)
                    {
                        button.Click();
                    }

                    NeedRebuild = true;
                }
            }
        }

        public void AddButton(IButtonControl button)
        {
            buttons.Add(button);
        }
    }

    /// <summary>
    /// Manages single-channel audio circuit simulation
    /// </summary>
    public class SimulationProcessor
    {
        public ObservableCollection<ComponentWrapper> InteractiveComponents { get; private set; }

        public Schematic Schematic { get; private set; }
        public string SchematicPath { get; private set; }
        public string SchematicName { get; private set; }

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
            InteractiveComponents = new ObservableCollection<ComponentWrapper>();
        }

        public void LoadSchematic(string path)
        {
            Schematic newSchematic = Circuit.Schematic.Load(path);

            Circuit.Circuit circuit = newSchematic.Build();

            SetCircuit(circuit);

            Schematic = newSchematic;

            SchematicName = System.IO.Path.GetFileNameWithoutExtension(path);

            SchematicPath = path;
        }

        public void ClearSchematic()
        {
            Schematic = null;
            circuit = null;
            InteractiveComponents.Clear();
        }

        void SetCircuit(Circuit.Circuit circuit)
        {
            this.circuit = circuit;

            InteractiveComponents.Clear();

            Dictionary<string, ButtonWrapper> buttonGroups = new Dictionary<string, ButtonWrapper>();

            foreach (Circuit.Component i in circuit.Components)
            {
                if (i is IPotControl)
                {
                    InteractiveComponents.Add(new PotWrapper((i as IPotControl), i.Name));
                }
                else if (i is IButtonControl)
                {
                    IButtonControl button = (i as IButtonControl);

                    ButtonWrapper wrapper = null;

                    if (string.IsNullOrEmpty(button.Group))
                    {
                        wrapper = new ButtonWrapper(i.Name);
                        InteractiveComponents.Add(wrapper);
                    }
                    else if (buttonGroups.ContainsKey(button.Group))
                    {
                        wrapper = buttonGroups[button.Group];
                    }
                    else
                    {
                        wrapper = new ButtonWrapper(button.Group);

                        buttonGroups[button.Group] = wrapper;

                        InteractiveComponents.Add(wrapper);
                    }

                    wrapper.AddButton(button);
                }
            }

            needRebuild = true;
        }

        /// <summary>
        /// Run the cicuite simulation on a buffer of audio samples
        /// </summary>
        /// <param name="audioInput">Array of input samples</param>
        /// <param name="audioOutput">Array of output samples</param>
        public void RunSimulation(double[] audioInput, double[] audioOutput)
        {
            if (circuit == null)
            {
                audioInput.CopyTo(audioOutput, 0);
            }
            else
            {
                bool needUpdate = false;

                foreach (ComponentWrapper component in InteractiveComponents)
                {
                    if (component.NeedUpdate)
                    {
                        needUpdate = true;

                        component.NeedUpdate = false;
                    }

                    if (component.NeedRebuild)
                    {
                        needRebuild = true;

                        component.NeedRebuild = false;
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
