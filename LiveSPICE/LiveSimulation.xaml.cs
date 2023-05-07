using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AgileObjects.ReadableExpressions.Extensions;
using AvalonDock.Layout;
using Circuit;
using LiveSPICE.Common;
using SchematicControls;
using Util;
using static System.Reactive.Linq.Observable;
using Expression = ComputerAlgebra.Expression;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for LiveSimulation.xaml
    /// </summary>
    partial class LiveSimulation : Window, INotifyPropertyChanged
    {
        public Log Log { get { return (Log)log.Content; } }
        public Scope Scope { get { return (Scope)scope.Content; } }


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
        private Simulation simulation = null;

        private List<Probe> probes = new List<Probe>();

        private Probe[] simulatedProbes;

        protected Dictionary<Expression, double> arguments = new Dictionary<Expression, double>();

        private object sync = new object();

        private Audio.Stream stream = null;

        public IReadOnlyCollection<InputChannel> InputChannels { get; }
        public IReadOnlyCollection<OutputChannel> OutputChannels { get; }

        private Dictionary<Expression, Channel> inputs = new Dictionary<Expression, Channel>();

        // A timer for continuously refreshing controls.
        protected DispatcherTimer timer;


        public double CpuLoad { get => simulation != null ? cpuLoad : 0d; set => cpuLoad = value; }
        public Simulation Simulation
        {
            get => simulation;
            set
            {
                simulation = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Max iterations for numerical algorithms.
        /// </summary>
        public int Iterations
        {
            get { return simulationPipeline.Settings.Iterations; }
            set { simulationPipeline.UpdateSimulationSettings(s => s with { Iterations = value }); NotifyChanged(); }
        }

        public int Oversample
        {
            get { return simulationPipeline.Settings.Oversample; }
            set { simulationPipeline.UpdateSimulationSettings(s => s with { Oversample = value }); NotifyChanged(); }
        }

        public ISimulationBuildPipeline<NewtonSimulationSettings> SimulationPipeline => simulationPipeline;

        private readonly ISimulationBuildPipeline<NewtonSimulationSettings> simulationPipeline;

        public LiveSimulation(SchematicEditor editor, Audio.Device device, Audio.Channel[] inputs, Audio.Channel[] outputs)
        {
            try
            {
                InitializeComponent();
                DataContext = this;

                // Make a clone of the schematic so we can mess with it.
                _editor = editor;
                var clone = Circuit.Schematic.Deserialize(editor.Schematic.Serialize(), Log); // slow
                clone.Elements.ItemAdded += OnElementAdded;
                clone.Elements.ItemRemoved += OnElementRemoved;
                Schematic = new SimulationSchematic(clone);
                Schematic.SelectionChanged += OnProbeSelected;

                // Build the circuit from the schematic.
                circuit = Schematic.Schematic.Build(Log);

                var builder = new NewtonSimulationBuilder(Log);

                // Some defaults
                var settings = new NewtonSimulationSettings(44100, Oversample: 1, Iterations: 8, Optimize: true);

                simulationPipeline = SimulationBuildPipeline.Create(builder: builder, settings: settings, log: Log);

                simulationPipeline.UpdateAnalysis(circuit.Analyze());

                // Create the input and output controls.                
                IEnumerable<Circuit.Component> components = circuit.Components;

                // Create audio input channels.
                InputChannels = inputs.Select((ch, idx) => new InputChannel(idx) { Name = ch.Name }).ToArray();

                Expression speakers = 0;

                foreach (Circuit.Component i in components)
                {
                    Symbol S = i.Tag as Symbol;
                    if (S == null)
                        continue;

                    SymbolControl tag = (SymbolControl)S.Tag;
                    if (tag == null)
                        continue;

                    // Create potentiometers.
                    if (i is IPotControl potentiometer)
                    {
                        _pots.Add(potentiometer);
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

                        var binding = new Binding
                        {
                            Source = potentiometer,
                            Path = new PropertyPath("(0)", typeof(IPotControl).GetProperty(nameof(IPotControl.Position))),
                            Mode = BindingMode.TwoWay,
                            NotifyOnSourceUpdated = true
                        };

                        potControl.SetBinding(PotControl.ValueProperty, binding);

                        potControl.AddHandler(Binding.SourceUpdatedEvent, new RoutedEventHandler((o, args) =>
                        {
                            var update = !potentiometer.Dynamic;

                            if (!string.IsNullOrEmpty(potentiometer.Group))
                            {
                                foreach (var p in components.OfType<IPotControl>().Where(p => p != potentiometer && p.Group == potentiometer.Group))
                                {
                                    p.Position = (o as PotControl).Value;
                                    update |= !p.Dynamic;
                                }
                            }
                            if (update)
                            {
                                RebuildSolution();
                            }
                        }));

                        potControl.MouseEnter += (o, e) => potControl.Opacity = 0.95;
                        potControl.MouseLeave += (o, e) => potControl.Opacity = 0.5;
                    }

                    // Create Buttons.
                    if (i is IButtonControl b)
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
                            b.Click();
                            // Click all the buttons in the group.
                            if (!string.IsNullOrEmpty(b.Group))
                            {
                                foreach (var j in components.OfType<IButtonControl>().Where(x => x != b && x.Group == b.Group))
                                    j.Click();
                            }
                            RebuildSolution();
                        };

                        button.MouseEnter += (o, e) => button.Opacity = 0.95;
                        button.MouseLeave += (o, e) => button.Opacity = 0.5;
                    }

                    if (i is Speaker output)
                        speakers += output.Out;

                    // Create input controls.
                    if (i is Input input)
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

                        Expression In = input.In;
                        this.inputs[In] = new SignalChannel(0);

                        combo.SelectionChanged += (o, e) =>
                        {
                            if (combo.SelectedItem != null)
                            {
                                ComboBoxItem it = (ComboBoxItem)combo.SelectedItem;
                                this.inputs[In] = new InputChannel(((InputChannel)it.Tag).Index);
                            }
                        };

                        combo.AddHandler(KeyDownEvent, new KeyEventHandler((o, e) =>
                        {
                            try
                            {
                                this.inputs[In] = new SignalChannel(Expression.Parse(combo.Text));
                            }
                            catch (Exception)
                            {
                                // If there is an error in the expression, zero out the signal.
                                this.inputs[In] = new SignalChannel(0);
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

                simulationPipeline.UpdateInputs(this.inputs.Keys);

                // Create audio output channels.
                OutputChannels = outputs.Select((o, idx) => new OutputChannel(idx) { Name = o.Name, Signal = speakers }).ToList();
                foreach (var channel in OutputChannels)
                    channel.SignalChanged += (o, e) => UpdateSimulationOutputs();

                UpdateSimulationOutputs();

                // Begin audio processing.
                if (inputs.Any() || outputs.Any())
                    stream = device.Open(ProcessSamples, inputs, outputs);
                else
                    stream = new NullStream(ProcessSamples);

                simulationPipeline.UpdateSimulationSettings(settings => settings with { SampleRate = (int)stream.SampleRate });

                Closed += (s, e) => stream.Stop();

                simulationSubscription = simulationPipeline.Simulation
                    .SubscribeOn(Scheduler.Default)
                    .Subscribe(
                        simulation =>
                        {
                            var simProbes = probes.Where(p => simulation.OutputExpressions.Contains(p.V)).ToArray(); // maybe not needed?

                            lock (sync)
                            {
                                Simulation = simulation;
                                simulatedProbes = simProbes;
                            }
                        });

                timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                timer.Tick += RefreshControls;
                timer.Start();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task RebuildSolution(bool shuffle = false) => Task.Run(() =>
            {
                SimulationPipeline.UpdateAnalysis(circuit.Analyze(shuffle));
            });

        private void UpdateSimulationOutputs()
        {
            SimulationPipeline.UpdateOutputs(probes.Select(p => p.V).Concat(OutputChannels.Select(o => o.Signal)));
        }

        private SchematicEditor _editor;
        private List<IPotControl> _pots = new List<IPotControl>();
        private double cpuLoad;
        private IDisposable simulationSubscription;

        private void ProcessSamples(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            // The time covered by these samples.
            double timespan = Count / Rate;

            // Apply input gain.
            for (int i = 0; i < In.Length; ++i)
            {
                Channel ch = InputChannels.ElementAt(i);
                double peak = In[i].Amplify(inputGain);
                ch.SampleSignalLevel(peak, timespan);
            }

            // Run the simulation.
            lock (sync)
            {
                if (Simulation != null)
                    RunSimulation(Count, In, Out, Rate);
                else
                    foreach (Audio.SampleBuffer i in Out)
                        i.Clear();
            }

            // Apply output gain.
            for (int i = 0; i < Out.Length; ++i)
            {
                Channel ch = OutputChannels.ElementAt(i);
                double peak = Out[i].Amplify(outputGain);
                ch.SampleSignalLevel(peak, timespan);
            }

            // Tick oscilloscope.
            Scope.Signals.TickClock(Count, Rate);
        }

        // These lists only ever grow, but they should never contain more than 10s of items.
        readonly List<double[]> inputBuffers = new List<double[]>();
        readonly List<double[]> outputBuffers = new List<double[]>();
        private void RunSimulation(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double sampleRate)
        {
            try
            {
                // If the sample rate changed, we need to kill the simulation and let the foreground rebuild it.
                if (sampleRate != (double)Simulation.SampleRate)
                {
                    Simulation = null;
                    simulationPipeline.UpdateSimulationSettings(settings => settings with { SampleRate = (int)stream.SampleRate });
                    return;
                }

                var sw = Stopwatch.StartNew();

                inputBuffers.Clear();
                foreach (Channel i in inputs.Values)
                {
                    if (i is InputChannel input)
                        inputBuffers.Add(In[input.Index].Samples);
                    else if (i is SignalChannel channel)
                        inputBuffers.Add(channel.Buffer(Count, Simulation.Time, Simulation.TimeStep));
                }

                outputBuffers.Clear();
                foreach (Probe i in simulatedProbes)
                    outputBuffers.Add(i.AllocBuffer(Count));
                for (int i = 0; i < Out.Length; ++i)
                    outputBuffers.Add(Out[i].Samples);

                // Process the samples!
                Simulation?.Run(Count, inputBuffers, outputBuffers);

                // Show the samples on the oscilloscope.
                long clock = Scope.Signals.Clock;
                foreach (Probe i in simulatedProbes)
                    i.Signal.AddSamples(clock, i.Buffer);

                var calcuationTime = sw.Elapsed;
                var availableTime = TimeSpan.FromSeconds(1d / sampleRate * Count);
                CpuLoad = calcuationTime / availableTime;

            }
            catch (SimulationDivergedException Ex)
            {
                // If the simulation diverged more than one second ago, reset it and hope it doesn't happen again.
                Log.WriteLine(MessageType.Error, "Error: " + Ex.Message);
                Simulation = null;
                //Status = SimulationStatus.Error;
                CpuLoad = 0d;
                if ((double)Ex.At > sampleRate)
                    RebuildSolution();
                foreach (Audio.SampleBuffer i in Out)
                    i.Clear();
            }
            catch (Exception Ex)
            {
                // If there was a more serious error, kill the simulation so the user can fix it.
                Log.WriteException(Ex);
                Simulation = null;
                foreach (Audio.SampleBuffer i in Out)
                    i.Clear();
            }
        }

        private void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol s && s.Component is Probe probe)
            {
                probe.Signal = new Signal()
                {
                    Name = probe.V.ToString(),
                    Pen = MapToSignalPen(probe.Color)
                };
                Scope.Signals.Add(probe.Signal);
                Scope.SelectedSignal = probe.Signal;
                probes.Add(probe);
                UpdateSimulationOutputs();
            }
        }

        private void OnElementRemoved(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol s && s.Component is Probe probe)
            {
                Scope.Signals.Remove(probe.Signal);
                probes.Remove(probe);
                UpdateSimulationOutputs();
            }
        }

        private void OnProbeSelected(object sender, EventArgs e)
        {
            IEnumerable<Symbol> selected = SimulationSchematic.ProbesOf(Schematic.Selected);
            if (selected.Any())
                Scope.SelectedSignal = ((Probe)selected.First().Component).Signal;
        }

        private async void Simulate_Executed(object sender, ExecutedRoutedEventArgs e) => await RebuildSolution(true);

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            //if (Simulation != null)
            //{
            //    var elements = Schematic.Schematic.Elements;
            //    var symbols = elements.OfType<Symbol>();
            //    var probes = symbols.Where(s => s.Component is Probe).ToArray();
            //    Schematic.Schematic.Elements.RemoveRange(probes);
            //}
        }

        private void ViewScope_Click(object sender, RoutedEventArgs e) { ToggleVisible(scope); }
        private void ViewAudio_Click(object sender, RoutedEventArgs e) { ToggleVisible(audio); }
        private void ViewLog_Click(object sender, RoutedEventArgs e) { ToggleVisible(log); }

        private void BindSignal_Click(object sender, RoutedEventArgs e)
        {
            OutputChannel ch = (OutputChannel)((FrameworkElement)sender).Tag;

            SchematicTool tool = Schematic.Tool;

            Schematic.Tool = new FindRelevantTool(Schematic)
            {
                Relevant = (x) => x is Symbol symbol && symbol.Component is TwoTerminal,
                Clicked = (x) =>
                {
                    if (x.Any())
                    {
                        Expression init = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? ch.Signal : 0;
                        ch.Signal = x.OfType<Symbol>()
                            .Select(i => i.Component)
                            .OfType<TwoTerminal>()
                            .Aggregate(init, (sum, c) => sum + c.V);
                    }
                    Schematic.Tool = tool;
                }
            };
        }

        private void RefreshControls(object sender, EventArgs e)
        {
            NotifyChanged(nameof(CpuLoad));

            foreach (Channel i in InputChannels)
                i.SignalStatus = MapSignalToBrush(i.SignalLevel);
            foreach (Channel i in OutputChannels)
                i.SignalStatus = MapSignalToBrush(i.SignalLevel);
        }

        private static void ToggleVisible(LayoutAnchorable Anchorable)
        {
            if (Anchorable.IsVisible)
                Anchorable.Hide();
            else
                Anchorable.Show();
        }

        private static Pen MapToSignalPen(EdgeType Color)
        {
            var color = Color switch
            {
                // These two need to be brighter than the normal colors.
                EdgeType.Red => new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 80, 80)), 1.0).GetAsFrozen(),
                EdgeType.Blue => new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 180, 255)), 1.0).GetAsFrozen(),
                _ => ElementControl.MapToPen(Color)
            };

            return (Pen)color.GetCurrentValueAsFrozen();
        }

        private static Brush MapSignalToBrush(double Peak) => Peak switch
        {
            < 1e-3 => Brushes.LightGray,
            < 0.5 => Brushes.YellowGreen,
            < 0.75 => Brushes.Yellow,
            < 0.95 => Brushes.Orange,
            _ => Brushes.Red
        };


        // INotifyPropertyChanged.
        private void NotifyChanged([CallerMemberName] string p = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        public void Dispose()
        {
            simulationSubscription.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
