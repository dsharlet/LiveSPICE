using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Circuit;
using ComputerAlgebra;
using Util;
using ButtonWrapper = LiveSPICEVst.ComponentWrapper<Circuit.IButtonControl>;

namespace LiveSPICEVst
{

    /// <summary>
    /// Manages single-channel audio circuit simulation
    /// </summary>
    public class SimulationProcessor
    {
        public ObservableCollection<IComponentWrapper> InteractiveComponents { get; private set; }

        public Schematic Schematic { get; private set; }
        public string SchematicPath { get; private set; }
        public string SchematicName { get { return System.IO.Path.GetFileNameWithoutExtension(SchematicPath); } }

        public double SampleRate
        {
            get { return sampleRate; }
            set
            {
                if (sampleRate != value)
                {
                    sampleRate = value;

                    needRebuild = true;

                    delayUpdateSamples = (int)(sampleRate * .1);
                }
            }
        }

        public int Oversample
        {
            get { return oversample; }
            set
            {
                if (oversample != value)
                {
                    oversample = value;

                    needRebuild = true;
                }
            }
        }

        public int Iterations
        {
            get { return iterations; }
            set
            {
                if (iterations != value)
                {
                    iterations = value;

                    needRebuild = true;
                }
            }
        }

        double sampleRate;
        int oversample = 2;
        int iterations = 8;

        Circuit.Circuit circuit = null;
        Simulation simulation = null;
        bool needUpdate = false;
        bool needRebuild = false;
        int updateSamplesElapsed = 0;
        int delayUpdateSamples = 0;
        Exception simulationUpdateException = null;

        public SimulationProcessor()
        {
            InteractiveComponents = new ObservableCollection<IComponentWrapper>();

            SampleRate = 44100;
        }

        public void LoadSchematic(string path)
        {
            Schematic newSchematic = Circuit.Schematic.Load(path);

            Circuit.Circuit circuit = newSchematic.Build();

            SetCircuit(circuit);

            Schematic = newSchematic;

            SchematicPath = path;
        }

        public void ClearSchematic()
        {
            Schematic = null;
            SchematicPath = "";
            circuit = null;
            InteractiveComponents.Clear();
        }

        void SetCircuit(Circuit.Circuit circuit)
        {
            this.circuit = circuit;

            InteractiveComponents.Clear();

            Dictionary<string, ButtonWrapper> buttonGroups = new Dictionary<string, ButtonWrapper>();
            Dictionary<string, PotWrapper> potGroups = new Dictionary<string, PotWrapper>();

            foreach (Circuit.Component i in circuit.Components)
            {
                if (i is IPotControl pot)
                {
                    if (string.IsNullOrEmpty(pot.Group))
                    {
                        InteractiveComponents.Add(new PotWrapper(pot, i.Name));
                    }
                    else if (potGroups.TryGetValue(pot.Group, out var wrapper))
                    {
                        wrapper.AddSection(pot);
                    }
                    else
                    {
                        wrapper = new PotWrapper(pot, pot.Group);
                        potGroups.Add(pot.Group, wrapper);
                        InteractiveComponents.Add(wrapper);
                    }
                }
                else if (i is IButtonControl button)
                {
                    ButtonWrapper wrapper;
                    if (string.IsNullOrEmpty(button.Group))
                    {
                        if (button.NumPositions == 2)
                        {
                            wrapper = new DoubleThrowWrapper(button, i.Name);
                            InteractiveComponents.Add(wrapper);
                        }
                        else
                        {
                            wrapper = new MultiThrowWrapper(button, i.Name);
                            InteractiveComponents.Add(wrapper);
                        }
                    }
                    else if (buttonGroups.ContainsKey(button.Group))
                    {
                        wrapper = buttonGroups[button.Group];
                        wrapper.AddSection(button);
                    }
                    else
                    {
                        if (button.NumPositions == 2)
                        {
                            wrapper = new DoubleThrowWrapper(button, button.Group);
                        }
                        else
                        {
                            wrapper = new MultiThrowWrapper(button, i.Name);
                        }

                        buttonGroups[button.Group] = wrapper;

                        InteractiveComponents.Add(wrapper);
                    }
                }
            }

            needRebuild = true;
        }

        /// <summary>
        /// Run the circuit simulation on a buffer of audio samples
        /// </summary>
        /// <param name="audioInputs">Arrays of input samples</param>
        /// <param name="audioOutputs">Arrays of output samples</param>
        /// <param name="numSamples">Number of samples to process</param>
        public void RunSimulation(double[][] audioInputs, double[][] audioOutputs, int numSamples)
        {
            // Throw any exception generated by asynchronous simulation updates
            if (simulationUpdateException != null)
            {
                Exception toThrow = simulationUpdateException;

                simulationUpdateException = null;

                throw toThrow;
            }

            if (simulation == null)
            {
                if (needRebuild)
                {
                    if (circuit != null)
                    {
                        UpdateSimulation(needRebuild);

                        needRebuild = false;
                    }
                }
            }

            if ((circuit == null) || (simulation == null))
            {
                audioInputs[0].CopyTo(audioOutputs[0], 0);
            }
            else
            {
                lock (sync)
                {
                    foreach (var component in InteractiveComponents)
                    {
                        if (component.NeedUpdate)
                        {
                            needUpdate = true;

                            component.NeedUpdate = false;

                            updateSamplesElapsed = 0;
                        }

                        if (component.NeedRebuild)
                        {
                            needRebuild = true;

                            component.NeedRebuild = false;
                        }
                    }

                    if (needUpdate || needRebuild)
                    {
                        // Delay updates until user input settles
                        if (needRebuild || (updateSamplesElapsed > delayUpdateSamples))
                        {
                            UpdateSimulation(needRebuild);

                            needRebuild = false;
                            needUpdate = false;
                        }
                        else
                        {
                            updateSamplesElapsed += numSamples;
                        }
                    }

                    simulation.Run(numSamples, audioInputs, audioOutputs);
                }
            }
        }

        int clock = -1;
        int update = 0;
        TaskScheduler scheduler = new RedundantTaskScheduler(1);
        object sync = new object();

        /// <summary>
        /// Update the simulation asynchronously
        /// </summary>
        /// <param name="rebuild">Whether a full simulation rebuild is required</param>
        void UpdateSimulation(bool rebuild)
        {
            int id = Interlocked.Increment(ref update);
            new Task(() =>
            {
                try
                {
                    Analysis analysis = circuit.Analyze();
                    TransientSolution ts = TransientSolution.Solve(analysis, (Real)1 / (sampleRate * oversample));

                    lock (sync)
                    {
                        if (id > clock)
                        {
                            if (rebuild)
                            {
                                Expression inputExpression = circuit.Components.OfType<Input>().Select(i => i.In).SingleOrDefault();

                                if (inputExpression == null)
                                {
                                    simulationUpdateException = new NotSupportedException("Circuit has no inputs.");
                                }
                                else
                                {
                                    IEnumerable<Speaker> speakers = circuit.Components.OfType<Speaker>();

                                    Expression outputExpression = 0;

                                    // Output is voltage drop across the speakers
                                    foreach (Speaker speaker in speakers)
                                    {
                                        outputExpression += speaker.Out;
                                    }

                                    if (outputExpression.EqualsZero())
                                    {
                                        simulationUpdateException = new NotSupportedException("Circuit has no speaker outputs.");
                                    }
                                    else
                                    {
                                        simulation = new Simulation(ts)
                                        {
                                            Oversample = oversample,
                                            Iterations = iterations,
                                            Input = new[] { inputExpression },
                                            Output = new[] { outputExpression }
                                        };
                                    }
                                }
                            }
                            else
                            {
                                simulation.Solution = ts;
                                clock = id;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    simulationUpdateException = ex;
                }

            }).Start(scheduler);
        }
    }
}
