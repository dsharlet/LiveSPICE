using System;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.AvalonDock.Layout;
using SyMath;
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
        public int Oversample
        {
            get { return oversample; }
            set { oversample = value; NotifyChanged("Oversample"); }
        }

        protected int iterations = 8;
        public int Iterations
        {
            get { return iterations; }
            set { iterations = value; NotifyChanged("Iterations"); }
        }

        private double inputGain = 1.0;
        public double InputGain
        {
            get { return (int)Math.Round(20 * Math.Log(inputGain, 10)); }
            set { inputGain = Math.Pow(10, value / 20); NotifyChanged("InputGain"); }
        }

        private double outputGain = 1.0;
        public double OutputGain
        {
            get { return (int)Math.Round(20 * Math.Log(outputGain, 10)); }
            set { outputGain = Math.Pow(10, value / 20); NotifyChanged("OutputGain"); }
        }

        protected Circuit.Circuit circuit = null;
        protected Circuit.Simulation simulation = null;
        protected Circuit.TransientSolution solution = null;

        private List<Probe> probes = new List<Probe>();
        protected Dictionary<SyMath.Expression, double> arguments = new Dictionary<SyMath.Expression, double>();

        private object sync = new object();

        protected Audio.Stream stream = null;
        
        protected Channel[] inputChannels, outputChannels;

        private Channel[] InitChannels(Panel Target, Audio.Channel[] Channels, IEnumerable<ComboBoxItem> Signals)
        {
            Channel[] channels = new Channel[Channels.Length];
            for (int i = 0; i < Channels.Length; ++i)
            {
                channels[i] = new Channel(Channels[i], Signals);
                Target.Children.Add(channels[i]);
            }
            return channels;
        }
        
        public LiveSimulation(Circuit.Schematic Simulate, Audio.Device Device, Audio.Channel[] Inputs, Audio.Channel[] Outputs)
        {
            try
            {
                InitializeComponent();

                // Make a clone of the schematic so we can mess with it.
                Circuit.Schematic clone = Circuit.Schematic.Deserialize(Simulate.Serialize(), Log);
                clone.Elements.ItemAdded += OnElementAdded;
                clone.Elements.ItemRemoved += OnElementRemoved;
                schematic.Schematic = new SimulationSchematic(clone);
                schematic.Schematic.SelectionChanged += OnProbeSelected;

                // Add Pot controls to all the IControl symbols.
                foreach (Circuit.Symbol i in schematic.Schematic.Symbols)
                {
                    SymbolControl tag = (SymbolControl)i.Tag;
                    Circuit.IControl control = i.Component as Circuit.IControl;
                    if (control != null)
                    {
                        PotControl pot = new PotControl() 
                        { 
                            Width = 80, Height = 80, Opacity = 0.25, 
                            FontSize = 15, FontWeight = FontWeights.Bold 
                        };
                        schematic.Schematic.overlays.Children.Add(pot);
                        Canvas.SetLeft(pot, Canvas.GetLeft(tag) - pot.Width / 2 + i.Width / 2);
                        Canvas.SetTop(pot, Canvas.GetTop(tag) - pot.Height / 2 + i.Height / 2);

                        pot.Value = control.Value;
                        pot.ValueChanged += x => { control.Value = x; UpdateSimulation(); };

                        pot.MouseEnter += (o, e) => pot.Opacity = 0.95;
                        pot.MouseLeave += (o, e) => pot.Opacity = 0.4;
                    }
                }

                // Build the circuit from the schematic.
                circuit = schematic.Schematic.Schematic.Build(Log);

                // Create the input and output controls.                
                IEnumerable<Circuit.Component> components = circuit.Components;

                inputChannels = InitChannels(inputs, Inputs, components.OfType<Circuit.Input>()
                    .Select(j => new ComboBoxItem() { Content = j.Name, Tag = Circuit.Component.DependentVariable(j.Name, Circuit.Component.t) })
                    .DefaultIfEmpty(new ComboBoxItem() { Content = "-", Tag = (SyMath.Expression)0 }));
                outputChannels = InitChannels(outputs, Outputs, components.OfType<Circuit.Speaker>()
                    .Select(j => new ComboBoxItem() { Content = j.Name, Tag = j.V })
                    .DefaultIfEmpty(new ComboBoxItem() { Content = "-", Tag = (SyMath.Expression)0 }));
               
                // Begin audio processing.
                if (Inputs.Any() || Outputs.Any())
                    stream = Device.Open(ProcessSamples, Inputs, Outputs);
                else
                    stream = new NullStream(ProcessSamples);

                ContentRendered += (o, e) => RebuildSimulation();

                Closed += (s, e) => stream.Stop();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RebuildSimulation()
        {
            lock (sync)
            {
                simulation = null;
                ProgressDialog.RunAsync(this, "Building simulation...", () =>
                {
                    try
                    {
                        Circuit.Quantity h = new Circuit.Quantity((SyMath.Expression)1 / (stream.SampleRate * Oversample), Circuit.Units.s);
                        solution = Circuit.TransientSolution.Solve(circuit.Analyze(), h, Log);

                        simulation = new Circuit.LinqCompiledSimulation(solution, Oversample, Log);
                    }
                    catch (Exception Ex)
                    {
                        Log.WriteException(Ex);
                    }
                });
            }
        }

        private RedundantTaskScheduler updateScheduler = new RedundantTaskScheduler(Math.Max(Environment.ProcessorCount - 2, 1));
        private void UpdateSimulation()
        {
            int ov = Oversample;
            Circuit.Quantity h = new Circuit.Quantity((SyMath.Expression)1 / (stream.SampleRate * ov), Circuit.Units.s);
            Circuit.Analysis analysis = circuit.Analyze();
            new Task(() => 
            {
                Circuit.TransientSolution s = Circuit.TransientSolution.Solve(analysis, h, solution.InitialConditions, new NullLog());
                lock (sync) simulation.Update(s, ov);
            }).Start(updateScheduler);
        }

        private void ProcessSamples(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            // Apply input gain.
            for (int i = 0; i < In.Length; ++i)
            {
                Channel ch = inputChannels[i];
                double peak = In[i].Amplify(ch.gain * inputGain);
                Dispatcher.InvokeAsync(() => ch.SignalStatus = MapSignalToBrush(peak));
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

            // Apply input gain.
            for (int i = 0; i < Out.Length; ++i)
            {
                Channel ch = outputChannels[i];
                double peak = Out[i].Amplify(ch.gain * outputGain);
                Dispatcher.InvokeAsync(() => ch.SignalStatus = MapSignalToBrush(peak));
            }

            // Tick oscilloscope.
            Scope.Signals.TickClock(Count, Rate);
        }

        private void RunSimulation(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            try
            {
                // If the sample rate changed, we need to kill the simulation and let the foreground rebuild it.
                if (Rate != (double)simulation.SampleRate)
                {
                    simulation = null;
                    Dispatcher.InvokeAsync(() => RebuildSimulation());
                    return;
                }

                KeyValuePair<SyMath.Expression, double[]>[] inputs = new KeyValuePair<SyMath.Expression, double[]>[In.Length];
                for (int i = 0; i < In.Length; ++i)
                    inputs[i] = new KeyValuePair<SyMath.Expression, double[]>(inputChannels[i].Signal, In[i].LockSamples(true, false));

                List<KeyValuePair<SyMath.Expression, double[]>> signals = new List<KeyValuePair<SyMath.Expression, double[]>>(probes.Count);
                foreach (Probe i in probes)
                    signals.Add(new KeyValuePair<SyMath.Expression, double[]>(i.V, i.AllocBuffer(Count)));

                KeyValuePair<SyMath.Expression, double[]>[] outputs = new KeyValuePair<SyMath.Expression, double[]>[Out.Length];
                for (int i = 0; i < Out.Length; ++i)
                    outputs[i] = new KeyValuePair<SyMath.Expression, double[]>(outputChannels[i].Signal, Out[i].LockSamples(false, true));

                // Process the samples!
                simulation.Run(Count, inputs, signals.Concat(outputs), Iterations);

                // Show the samples on the oscilloscope.
                long clock = Scope.Signals.Clock;
                foreach (Probe i in probes)
                    i.Signal.AddSamples(clock, i.Buffer);
            }
            catch (Circuit.SimulationDiverged Ex)
            {
                // If the simulation diverged more than one second ago, reset it and hope it doesn't happen again.
                Log.WriteLine(MessageType.Error, "Error: " + Ex.Message);
                if ((double)Ex.At > Rate)
                    simulation.Reset();
                else
                    simulation = null;
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

            // Unlock sample buffers.
            foreach (Audio.SampleBuffer i in Out)
                i.Unlock();
            foreach (Audio.SampleBuffer i in In)
                i.Unlock();
        }

        private static double AmplifySignal(Audio.SampleBuffer Signal, double Gain)
        {
            double peak = 0.0;
            using (Audio.SamplesLock samples = new Audio.SamplesLock(Signal, true, true))
            {
                for (int i = 0; i < samples.Count; ++i)
                {
                    double v = samples[i];
                    v *= Gain;
                    peak = Math.Max(peak, Math.Abs(v));
                    samples[i] = v;
                }
            }
            return peak;
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
                lock (sync) probes.Add(probe);
            }
        }

        private void OnElementRemoved(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;
                Scope.Signals.Remove(probe.Signal);
                lock (sync) probes.Remove(probe);
            }
        }

        private void OnProbeSelected(object sender, EventArgs e)
        {
            IEnumerable<Circuit.Symbol> selected = SimulationSchematic.ProbesOf(schematic.Schematic.Selected);
            if (selected.Any())
                Scope.SelectedSignal = ((Probe)selected.First().Component).Signal;
        }

        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e) { RebuildSimulation(); }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void ViewScope_Click(object sender, RoutedEventArgs e) { ToggleVisible(scope); }
        private void ViewAudio_Click(object sender, RoutedEventArgs e) { ToggleVisible(audio); }
        private void ViewLog_Click(object sender, RoutedEventArgs e) { ToggleVisible(log); }

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
                case Circuit.EdgeType.Red: return new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 50, 50)), 1.0);
                case Circuit.EdgeType.Blue: return new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 180, 255)), 1.0);
                default: return ElementControl.MapToPen(Color);
            }
        }

        private static Brush MapSignalToBrush(double Peak)
        {
            if (Peak < 0.5) return Brushes.Green;
            if (Peak < 0.75) return Brushes.Yellow;
            if (Peak < 0.95) return Brushes.Orange;
            return Brushes.Red;
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
