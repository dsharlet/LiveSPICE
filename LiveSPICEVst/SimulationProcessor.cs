using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Circuit;
using CommunityToolkit.Mvvm.ComponentModel;
using ComputerAlgebra;
using LiveSPICE.Common;
using Util;
using ButtonWrapper = LiveSPICEVst.ComponentWrapper<Circuit.IButtonControl>;

namespace LiveSPICEVst
{

    /// <summary>
    /// Manages single-channel audio circuit simulation
    /// </summary>
    public class SimulationProcessor : ObservableObject
    {
        public ObservableCollection<IComponentWrapper> InteractiveComponents { get; private set; }

        public ISimulationBuildPipeline<NewtonSimulationSettings> SimulationPipeline { get; }

        private IDisposable simulationStream;

        public Schematic Schematic { get; private set; }
        public string SchematicPath { get; private set; }
        public string SchematicName { get { return System.IO.Path.GetFileNameWithoutExtension(SchematicPath); } }

        public double SampleRate
        {
            get { return SimulationPipeline.Settings.SampleRate; }
            set { SimulationPipeline.UpdateSimulationSettings(s => s with { SampleRate = (int)value }); OnPropertyChanged(); }
        }

        public int Oversample
        {
            get { return SimulationPipeline.Settings.Oversample; }
            set { SimulationPipeline.UpdateSimulationSettings(s => s with { Oversample = value }); OnPropertyChanged(); }
        }

        public int Iterations
        {
            get { return SimulationPipeline.Settings.Iterations; }
            set { SimulationPipeline.UpdateSimulationSettings(s => s with { Iterations = value }); OnPropertyChanged(); }
        }

        private double cpuLoad;

        public double CpuLoad { get => cpuLoad; set => SetProperty(ref cpuLoad, value); }

        Circuit.Circuit circuit;
        Simulation simulation;
        Exception simulationUpdateException;

        public SimulationProcessor()
        {
            InteractiveComponents = new ObservableCollection<IComponentWrapper>();

            var initialSettings = new NewtonSimulationSettings(SampleRate: 44100, Oversample: 2, Iterations: 8, Optimize: true);

            var log = new NullLog(); // TODO: logging

            var builder = new NewtonSimulationBuilder(log);

            SimulationPipeline = SimulationBuildPipeline.Create(builder, initialSettings, log);
        }

        public void LoadSchematic(string path)
        {
            Schematic newSchematic = Schematic.Load(path);

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

            simulationStream?.Dispose();

            InteractiveComponents.Clear();

            Dictionary<string, ButtonWrapper> buttonGroups = new Dictionary<string, ButtonWrapper>();
            Dictionary<string, PotWrapper> potGroups = new Dictionary<string, PotWrapper>();

            foreach (Component component in circuit.Components)
            {
                if (component is IPotControl pot)
                {
                    if (string.IsNullOrEmpty(pot.Group))
                    {
                        InteractiveComponents.Add(new PotWrapper(pot, component.Name));
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
                else if (component is IButtonControl button)
                {
                    ButtonWrapper wrapper;
                    if (string.IsNullOrEmpty(button.Group))
                    {
                        if (button.NumPositions == 2)
                        {
                            wrapper = new DoubleThrowWrapper(button, component.Name);
                            InteractiveComponents.Add(wrapper);
                        }
                        else
                        {
                            wrapper = new MultiThrowWrapper(button, component.Name);
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
                            wrapper = new MultiThrowWrapper(button, component.Name);
                        }

                        buttonGroups[button.Group] = wrapper;

                        InteractiveComponents.Add(wrapper);
                    }
                }
            }

            var inputExpression = circuit.Components.OfType<Input>().Select(i => i.In).SingleOrDefault();

            if (inputExpression == null)
                throw new NotSupportedException("Circuit has no inputs.");

            var speakers = circuit.Components.OfType<Speaker>();

            Expression outputExpression = 0;

            // Output is voltage drop across the speakers
            foreach (var speaker in speakers)
            {
                outputExpression += speaker.Out;
            }

            if (outputExpression.EqualsZero())
                throw new NotSupportedException("Circuit has no speaker outputs.");


            SimulationPipeline.UpdateInputs(new[] { inputExpression });
            SimulationPipeline.UpdateOutputs(new[] { outputExpression });

            SimulationPipeline.UpdateAnalysis(circuit.Analyze());

            simulationStream = SimulationPipeline.Simulation.Subscribe(simulation =>
            {
                lock (sync)
                {
                    this.simulation = simulation;
                }
            });
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
            var sw = Stopwatch.StartNew();
            if ((circuit == null) || (simulation == null))
            {
                audioInputs[0].CopyTo(audioOutputs[0], 0);
                return;
            }
            lock (sync)
            {
                simulation.Run(numSamples, audioInputs, audioOutputs);
            }

            var windowTime = TimeSpan.FromSeconds(1d / SampleRate * numSamples);

            var a = DecayRate(windowTime.TotalMilliseconds, 300);

            CpuLoad = CpuLoad * a + sw.Elapsed / windowTime * (1 - a);
        }

        private static double DecayRate(double timestep, double halflife)
        {
            return Math.Exp(timestep / halflife * Math.Log(0.5));
        }

        object sync = new object();
    }
}
