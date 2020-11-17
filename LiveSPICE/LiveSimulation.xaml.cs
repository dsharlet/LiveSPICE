using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AvalonDock.Layout;
using Circuit;
using SchematicControls;
using Util;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for LiveSimulation.xaml
    /// </summary>
    partial class LiveSimulation : Window, INotifyPropertyChanged
    {
        public Log Log { get { return (Log)log.Content; } }
        public Scope Scope { get { return (Scope)scope.Content; } }

        protected int oversample = 8;
        /// <summary>
        /// Simulation oversampling rate.
        /// </summary>
        public int Oversample
        {
            get { return oversample; }
            set { oversample = value; RebuildSolution(); NotifyChanged(nameof(Oversample)); }
        }

        protected int iterations = 8;
        /// <summary>
        /// Max iterations for numerical algorithms.
        /// </summary>
        public int Iterations
        {
            get { return iterations; }
            set { iterations = value; RebuildSolution(); NotifyChanged(nameof(Iterations)); }
        }

        private double inputGain = 1.0;
        /// <summary>
        /// Overall input gain.
        /// </summary>
        public double InputGain { get { return inputGain; } set { inputGain = value; NotifyChanged(nameof(InputGain)); } }

        private double outputGain = 1.0;
        /// <summary>
        /// Overall output gain.
        /// </summary>
        public double OutputGain { get { return outputGain; } set { outputGain = value; NotifyChanged(nameof(OutputGain)); } }

        private SimulationSchematic Schematic { get { return (SimulationSchematic)schematic.Schematic; } set { schematic.Schematic = value; } }

        protected Circuit.Circuit circuit = null;
        protected Circuit.Simulation simulation = null;

        private List<Probe> probes = new List<Probe>();

        private object sync = new object();

        protected Audio.Stream stream = null;

        protected ObservableCollection<InputChannel> _inputChannels = new ObservableCollection<InputChannel>();
        protected ObservableCollection<OutputChannel> _outputChannels = new ObservableCollection<OutputChannel>();
        public ObservableCollection<InputChannel> InputChannels { get { return _inputChannels; } }
        public ObservableCollection<OutputChannel> OutputChannels { get { return _outputChannels; } }

        private Dictionary<ComputerAlgebra.Expression, Channel> inputs = new Dictionary<ComputerAlgebra.Expression, Channel>();

        // A timer for continuously refreshing controls.
        protected System.Timers.Timer timer;

        public LiveSimulation(Circuit.Schematic Simulate, Audio.Device Device, Audio.Channel[] Inputs, Audio.Channel[] Outputs)
        {
            try
            {
                InitializeComponent();

                // Make a clone of the schematic so we can mess with it.
                var clone = Circuit.Schematic.Deserialize(Simulate.Serialize(), Log);
                clone.Elements.ItemAdded += OnElementAdded;
                clone.Elements.ItemRemoved += OnElementRemoved;
                Schematic = new SimulationSchematic(clone);
                Schematic.SelectionChanged += OnProbeSelected;

                // Build the circuit from the schematic.
                circuit = Schematic.Schematic.Build(Log);

                // Create the input and output controls.                
                IEnumerable<Circuit.Component> components = circuit.Components;

                // Create audio input channels.
                for (int i = 0; i < Inputs.Length; ++i)
                    InputChannels.Add(new InputChannel(i) { Name = Inputs[i].Name });

                ComputerAlgebra.Expression speakers = 0;

                foreach (Circuit.Component i in components)
                {
                    Circuit.Symbol S = i.Tag as Circuit.Symbol;
                    if (S == null)
                        continue;

                    SymbolControl tag = (SymbolControl)S.Tag;
                    if (tag == null)
                        continue;

                    // Create potentiometers.
                    if (i is Circuit.IPotControl potentiometer)
                    {
                        var potControl = new PotControl()
                        {
                            Width = 80,
                            Height = 80,
                            Opacity = 0.5,
                            FontSize = 15,
                            FontWeight = FontWeights.Bold,
                        };
                        Schematic.Overlays.Children.Add(potControl);
                        Canvas.SetLeft(potControl, Canvas.GetLeft(tag) - potControl.Width / 2 + tag.Width / 2);
                        Canvas.SetTop(potControl, Canvas.GetTop(tag) - potControl.Height / 2 + tag.Height / 2);

                        potControl.Value = potentiometer.PotValue;
                        potControl.ValueChanged += x =>
                        {
                            foreach (var p in components.OfType<IPotControl>().Where(p => p.Group == potentiometer.Group))
                            {
                                p.PotValue = x;
                            }
                            UpdateSimulation(false);
                        };

                        potControl.MouseEnter += (o, e) => potControl.Opacity = 0.95;
                        potControl.MouseLeave += (o, e) => potControl.Opacity = 0.5;
                    }

                    // Create Buttons.
                    if (i is Circuit.IButtonControl b)
                    {
                        Button button = new Button()
                        {
                            Width = tag.Width,
                            Height = tag.Height,
                            Opacity = 0.5,
                            Background = Brushes.White,
                        };
                        Schematic.Overlays.Children.Add(button);
                        Canvas.SetLeft(button, Canvas.GetLeft(tag));
                        Canvas.SetTop(button, Canvas.GetTop(tag));

                        button.Click += (o, e) =>
                        {
                            // Click all the buttons in the group.
                            foreach (Circuit.IButtonControl j in components.OfType<Circuit.IButtonControl>().Where(x => x.Group == b.Group))
                                j.Click();
                            UpdateSimulation(true);
                        };

                        button.MouseEnter += (o, e) => button.Opacity = 0.95;
                        button.MouseLeave += (o, e) => button.Opacity = 0.5;
                    }

                    if (i is Circuit.Speaker output)
                        speakers += output.Out;

                    // Create input controls.
                    if (i is Circuit.Input input)
                    {
                        tag.ShowText = false;

                        ComboBox combo = new ComboBox()
                        {
                            Width = 80,
                            Height = 24,
                            Opacity = 0.5,
                            IsEditable = true,
                            SelectedValuePath = "Tag",
                        };

                        foreach (InputChannel j in InputChannels)
                        {
                            combo.Items.Add(new ComboBoxItem()
                            {
                                Tag = j,
                                Content = j.Name
                            });
                        }

                        Schematic.Overlays.Children.Add(combo);
                        Canvas.SetLeft(combo, Canvas.GetLeft(tag) - combo.Width / 2 + tag.Width / 2);
                        Canvas.SetTop(combo, Canvas.GetTop(tag) - combo.Height / 2 + tag.Height / 2);

                        ComputerAlgebra.Expression In = input.In;
                        inputs[In] = new SignalChannel(0);

                        combo.SelectionChanged += (o, e) =>
                        {
                            if (combo.SelectedItem != null)
                            {
                                ComboBoxItem it = (ComboBoxItem)combo.SelectedItem;
                                inputs[In] = new InputChannel(((InputChannel)it.Tag).Index);
                            }
                        };

                        combo.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler((o, e) =>
                        {
                            try
                            {
                                inputs[In] = new SignalChannel(ComputerAlgebra.Expression.Parse(combo.Text));
                            }
                            catch (Exception)
                            {
                                // If there is an error in the expression, zero out the signal.
                                inputs[In] = new SignalChannel(0);
                            }
                        }));

                        if (combo.Items.Count > 0)
                            combo.SelectedItem = combo.Items[0];
                        else
                            combo.Text = "0 V";

                        combo.MouseEnter += (o, e) => combo.Opacity = 0.95;
                        combo.MouseLeave += (o, e) => combo.Opacity = 0.5;
                    }
                }

                // Create audio output channels.
                for (int i = 0; i < Outputs.Length; ++i)
                {
                    OutputChannel c = new OutputChannel(i) { Name = Outputs[i].Name, Signal = speakers };
                    c.PropertyChanged += (o, e) => { if (e.PropertyName == "Signal") RebuildSolution(); };
                    OutputChannels.Add(c);
                }


                // Begin audio processing.
                if (Inputs.Any() || Outputs.Any())
                    stream = Device.Open(ProcessSamples, Inputs, Outputs);
                else
                    stream = new NullStream(ProcessSamples);

                ContentRendered += (o, e) => RebuildSolution();

                Closed += (s, e) => stream.Stop();

                timer = new System.Timers.Timer()
                {
                    Interval = 100,
                    AutoReset = true,
                    Enabled = true,
                };
                timer.Elapsed += timer_Elapsed;
                timer.Start();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RebuildSolution()
        {
            lock (sync)
            {
                simulation = null;
                ProgressDialog.RunAsync(this, "Building circuit solution...", () =>
                {
                    try
                    {
                        ComputerAlgebra.Expression h = (ComputerAlgebra.Expression)1 / (stream.SampleRate * Oversample);
                        Circuit.TransientSolution solution = Circuit.TransientSolution.Solve(circuit.Analyze(), h, Log);

                        simulation = new Circuit.Simulation(solution)
                        {
                            Log = Log,
                            Input = inputs.Keys.ToArray(),
                            Output = probes.Select(i => i.V).Concat(OutputChannels.Select(i => i.Signal)).ToArray(),
                            Oversample = Oversample,
                            Iterations = Iterations,
                        };
                    }
                    catch (Exception Ex)
                    {
                        Log.WriteException(Ex);
                    }
                });
            }
        }

        private int clock = -1;
        private int update = 0;
        private TaskScheduler scheduler = new RedundantTaskScheduler(1);
        private void UpdateSimulation(bool Rebuild)
        {
            int id = Interlocked.Increment(ref update);
            new Task(() =>
            {
                ComputerAlgebra.Expression h = (ComputerAlgebra.Expression)1 / (stream.SampleRate * Oversample);
                Circuit.TransientSolution s = Circuit.TransientSolution.Solve(circuit.Analyze(), h, Rebuild ? (ILog)Log : new NullLog());
                lock (sync)
                {
                    if (id > clock)
                    {
                        if (Rebuild)
                        {
                            simulation = new Circuit.Simulation(s)
                            {
                                Log = Log,
                                Input = inputs.Keys.ToArray(),
                                Output = probes.Select(i => i.V).Concat(OutputChannels.Select(i => i.Signal)).ToArray(),
                                Oversample = Oversample,
                                Iterations = Iterations,
                            };
                        }
                        else
                        {
                            simulation.Solution = s;
                            clock = id;
                        }
                    }
                }
            }).Start(scheduler);
        }

        private void ProcessSamples(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            // The time covered by these samples.
            double timespan = Count / Rate;

            // Apply input gain.
            for (int i = 0; i < In.Length; ++i)
            {
                Channel ch = InputChannels[i];
                double peak = In[i].Amplify(inputGain);
                ch.SampleSignalLevel(peak, timespan);
            }

            // Run the simulation.
            lock (sync)
            {
                if (simulation != null)
                    RunSimulation(Count, In, Out, Rate);
                else
                    foreach (Audio.SampleBuffer i in Out)
                        i.Clear();
            }

            // Apply output gain.
            for (int i = 0; i < Out.Length; ++i)
            {
                Channel ch = OutputChannels[i];
                double peak = Out[i].Amplify(outputGain);
                ch.SampleSignalLevel(peak, timespan);
            }

            // Tick oscilloscope.
            Scope.Signals.TickClock(Count, Rate);
        }

        // These lists only ever grow, but they should never contain more than 10s of items.
        readonly List<double[]> inputBuffers = new List<double[]>();
        readonly List<double[]> outputBuffers = new List<double[]>();
        private void RunSimulation(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            try
            {
                // If the sample rate changed, we need to kill the simulation and let the foreground rebuild it.
                if (Rate != (double)simulation.SampleRate)
                {
                    simulation = null;
                    Dispatcher.InvokeAsync(() => RebuildSolution());
                    return;
                }

                inputBuffers.Clear();
                foreach (Channel i in inputs.Values)
                {
                    if (i is InputChannel input)
                        inputBuffers.Add(In[input.Index].Samples);
                    else if (i is SignalChannel channel)
                        inputBuffers.Add(channel.Buffer(Count, simulation.Time, simulation.TimeStep));
                }

                outputBuffers.Clear();
                foreach (Probe i in probes)
                    outputBuffers.Add(i.AllocBuffer(Count));
                for (int i = 0; i < Out.Length; ++i)
                    outputBuffers.Add(Out[i].Samples);

                // Process the samples!
                simulation.Run(Count, inputBuffers, outputBuffers);

                // Show the samples on the oscilloscope.
                long clock = Scope.Signals.Clock;
                foreach (Probe i in probes)
                    i.Signal.AddSamples(clock, i.Buffer);
            }
            catch (Circuit.SimulationDiverged Ex)
            {
                // If the simulation diverged more than one second ago, reset it and hope it doesn't happen again.
                Log.WriteLine(MessageType.Error, "Error: " + Ex.Message);
                simulation = null;
                if ((double)Ex.At > Rate)
                    Dispatcher.InvokeAsync(() => RebuildSolution());
                foreach (Audio.SampleBuffer i in Out)
                    i.Clear();
            }
            catch (Exception Ex)
            {
                // If there was a more serious error, kill the simulation so the user can fix it.
                Log.WriteException(Ex);
                simulation = null;
                foreach (Audio.SampleBuffer i in Out)
                    i.Clear();
            }
        }

        private void OnElementAdded(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;
                probe.Signal = new Signal()
                {
                    Name = probe.V.ToString(),
                    Pen = MapToSignalPen(probe.Color)
                };
                Scope.Signals.Add(probe.Signal);
                Scope.SelectedSignal = probe.Signal;
                lock (sync)
                {
                    probes.Add(probe);
                    if (simulation != null)
                        simulation.Output = probes.Select(i => i.V).Concat(OutputChannels.Select(i => i.Signal)).ToArray();
                }
            }
        }

        private void OnElementRemoved(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;
                Scope.Signals.Remove(probe.Signal);
                lock (sync)
                {
                    probes.Remove(probe);
                    if (simulation != null)
                        simulation.Output = probes.Select(i => i.V).Concat(OutputChannels.Select(i => i.Signal)).ToArray();
                }
            }
        }

        private void OnProbeSelected(object sender, EventArgs e)
        {
            IEnumerable<Circuit.Symbol> selected = SimulationSchematic.ProbesOf(Schematic.Selected);
            if (selected.Any())
                Scope.SelectedSignal = ((Probe)selected.First().Component).Signal;
        }

        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e) { RebuildSolution(); }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void ViewScope_Click(object sender, RoutedEventArgs e) { ToggleVisible(scope); }
        private void ViewAudio_Click(object sender, RoutedEventArgs e) { ToggleVisible(audio); }
        private void ViewLog_Click(object sender, RoutedEventArgs e) { ToggleVisible(log); }

        private void BindSignal_Click(object sender, RoutedEventArgs e)
        {
            OutputChannel ch = (OutputChannel)((FrameworkElement)sender).Tag;

            SchematicTool tool = Schematic.Tool;

            Schematic.Tool = new FindRelevantTool(Schematic)
            {
                Relevant = (x) => x is Circuit.Symbol symbol && symbol.Component is Circuit.TwoTerminal,
                Clicked = (x) =>
                {
                    if (x.Any())
                    {
                        ComputerAlgebra.Expression init = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? ch.Signal : 0;
                        ch.Signal = x.OfType<Circuit.Symbol>()
                            .Select(i => i.Component)
                            .OfType<Circuit.TwoTerminal>()
                            .Aggregate(init, (sum, c) => sum + c.V);
                    }
                    Schematic.Tool = tool;
                }
            };
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // TODO: Figure out how to calculate the processing speed to set statusSampleRate.Text.
                foreach (Channel i in InputChannels)
                    i.SignalStatus = MapSignalToBrush(i.SignalLevel);
                foreach (Channel i in OutputChannels)
                    i.SignalStatus = MapSignalToBrush(i.SignalLevel);
            });
        }

        private static void ToggleVisible(LayoutAnchorable Anchorable)
        {
            if (Anchorable.IsVisible)
                Anchorable.Hide();
            else
                Anchorable.Show();
        }

        private static Pen MapToSignalPen(Circuit.EdgeType Color)
        {
            switch (Color)
            {
                // These two need to be brighter than the normal colors.
                case Circuit.EdgeType.Red: return new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 80, 80)), 1.0);
                case Circuit.EdgeType.Blue: return new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 180, 255)), 1.0);
                default: return ElementControl.MapToPen(Color);
            }
        }

        private static Brush MapSignalToBrush(double Peak)
        {
            if (Peak < 1e-3) return Brushes.Transparent;
            if (Peak < 0.5) return Brushes.Green;
            if (Peak < 0.75) return Brushes.Yellow;
            if (Peak < 0.95) return Brushes.Orange;
            return Brushes.Red;
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
