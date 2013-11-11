using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
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

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for LiveSimulation.xaml
    /// </summary>
    partial class LiveSimulation : Window, INotifyPropertyChanged
    {
        public Log Log { get { return (Log)log.Content; } }
        public Parameters Parameters { get { return (Parameters)parameters.Content; } }
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

        protected Audio.Stream stream = null;

        protected class Channel : INotifyPropertyChanged
        {
            private SyMath.Expression signal;
            public SyMath.Expression Signal { get { return signal; } set { signal = value; NotifyChanged("Signal"); } }

            public TextBlock Level;

            public double gain = 1.0;
            public double Gain { get { return (int)Math.Round(20 * Math.Log(gain, 10)); } set { gain = Math.Pow(10, value / 20.0); NotifyChanged("Gain"); } }
            
            // INotifyPropertyChanged.
            private void NotifyChanged(string p)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        protected Channel[] inputChannels, outputChannels;

        private Channel[] InitChannels(Panel Target, Audio.Channel[] Channels, IEnumerable<ComboBoxItem> Signals)
        {
            Channel[] channels = new Channel[Channels.Length];
            for (int i = 0; i < Channels.Length; ++i)
            {
                channels[i] = new Channel();

                channels[i].Level = new TextBlock() 
                {
                    Width = 45,
                    Margin = new Thickness(1),
                    TextAlignment = TextAlignment.Center, 
                    FontWeight = FontWeights.Bold, 
                };
                channels[i].Level.SetBinding(TextBlock.TextProperty, new Binding("Gain") { Source = channels[i], StringFormat = "{0:+#;-#;+0} dB" });
                
                TextBlock name = new TextBlock() 
                { 
                    Width = 50,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = Channels[i].Name, 
                    ToolTip = Channels[i].Name,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };

                ComboBox signal = new ComboBox()
                {
                    Width = 36,
                    ItemsSource = Signals,
                    SelectedValuePath = "Tag",
                };
                signal.SetBinding(ComboBox.SelectedValueProperty, new Binding("Signal") { Source = channels[i] });
                signal.SetBinding(ComboBox.ToolTipProperty, new Binding("Signal") { Source = channels[i] });
                if (Signals.Any())
                    channels[i].Signal = (SyMath.Expression)Signals.First().Tag;

                Slider gain = new Slider() { Minimum = -20, Maximum = 20 };
                gain.SetBinding(Slider.ValueProperty, new Binding("Gain") { Source = channels[i] });

                UIElement level = channels[i].Level;

                Panel panel = new DockPanel() { LastChildFill = true, Tag = channels[i] };
                panel.Children.Add(name);
                panel.Children.Add(signal);
                panel.Children.Add(level);
                panel.Children.Add(gain);

                DockPanel.SetDock(name, Dock.Left);
                DockPanel.SetDock(signal, Dock.Left);
                DockPanel.SetDock(level, Dock.Right);
                DockPanel.SetDock(gain, Dock.Right);

                Target.Children.Add(panel);
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
                
                // Build the circuit from the schematic.
                circuit = schematic.Schematic.Schematic.Build(Log);
                IEnumerable<Circuit.Component> components = circuit.Components;

                inputChannels = InitChannels(inputs, Inputs, components.OfType<Circuit.Input>().Select(j => new ComboBoxItem() { Content = j.Name, Tag = Circuit.Component.DependentVariable(j.Name, Circuit.Component.t) }));
                outputChannels = InitChannels(outputs, Outputs, components.OfType<Circuit.Speaker>().Select(j => new ComboBoxItem() { Content = j.Name, Tag = j.V }));

                Parameters.ParameterChanged += (o, e) => arguments[e.Changed.Name] = e.Value;

                // Begin audio processing.
                stream = Device.Open(ProcessSamples, Inputs, Outputs);
                Closed += (s, e) => stream.Stop();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                lock (probes) probes.Add(probe);
            }
        }

        private void OnElementRemoved(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;
                Scope.Signals.Remove(probe.Signal);
                lock(probes) probes.Remove(probe);
            }
        }

        private void OnProbeSelected(object sender, EventArgs e)
        {
            IEnumerable<Circuit.Symbol> selected = SimulationSchematic.ProbesOf(schematic.Schematic.Selected);
            if (selected.Any())
                Scope.SelectedSignal = ((Probe)selected.First().Component).Signal;
        }

        private bool canclose = false;
        private bool closing = false;
        protected override void OnClosing(CancelEventArgs e)
        {
            if (stream != null)
            {
                closing = true;
                e.Cancel = !canclose;
            }
        }

        private bool rebuild = true;
        private void ProcessSamples(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            if (closing)
            {
                canclose = true;
                Dispatcher.InvokeAsync(() => Close());
                return;
            }

            RebuildSimulation(Rate);

            // Apply input gain.
            for (int i = 0; i < In.Length; ++i)
            {
                Channel ch = inputChannels[i];
                double peak = In[i].Amplify(ch.gain * inputGain);
                Dispatcher.InvokeAsync(() => ch.Level.Background = MapSignalToBrush(peak));
            }

            RunSimulation(Count, In, Out, Rate);

            // Apply input gain.
            for (int i = 0; i < Out.Length; ++i)
            {
                Channel ch = outputChannels[i];
                double peak = Out[i].Amplify(ch.gain * outputGain);
                Dispatcher.InvokeAsync(() => ch.Level.Background = MapSignalToBrush(peak));
            }

            // Tick oscilloscope.
            Scope.Signals.TickClock(Count, Rate);
        }

        private void RunSimulation(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            if (simulation != null)
            {
                try
                {
                    lock (probes)
                    {
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
                }
                catch (Circuit.SimulationDiverged Ex)
                {
                    // If the simulation diverged more than one second ago, reset it and hope it doesn't happen again.
                    Log.WriteLine(Circuit.MessageType.Error, "Error: " + Ex.Message);
                    if ((double)Ex.At > Rate)
                        simulation.Reset();
                    else
                        simulation = null;
                    foreach (Audio.SampleBuffer i in Out)
                        i.Clear();
                }
                catch (Exception ex)
                {
                    // If there was a more serious error, kill the simulation so the user can fix it.
                    Log.WriteLine(Circuit.MessageType.Error, "Error: " + ex.Message);
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
            else
            {
                // If there is no simulation, just zero the output.
                foreach (Audio.SampleBuffer i in Out)
                    i.Clear();
            }
        }

        private void RebuildSimulation(double SampleRate)
        {
            if (rebuild || (simulation != null && (Oversample != simulation.Oversample || SampleRate != (double)simulation.SampleRate)))
            {
                try
                {
                    Circuit.Quantity h = new Circuit.Quantity(1 / (SampleRate * Oversample), Circuit.Units.s);
                    solution = Circuit.TransientSolution.SolveCircuit(circuit, h, Log);
                    arguments = solution.Parameters.ToDictionary(i => i.Name, i => 0.5);
                    Dispatcher.Invoke(() => Parameters.UpdateControls(solution.Parameters));

                    simulation = new Circuit.LinqCompiledSimulation(solution, Oversample, Log);
                }
                catch (System.Exception ex)
                {
                    Log.WriteLine(Circuit.MessageType.Error, "Error: " + ex.Message);
                    simulation = null;
                }
                rebuild = false;
            }
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

        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e) { rebuild = true; }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void ViewScope_Click(object sender, RoutedEventArgs e) { ToggleVisible(scope); }
        private void ViewAudio_Click(object sender, RoutedEventArgs e) { ToggleVisible(audio); }
        private void ViewLog_Click(object sender, RoutedEventArgs e) { ToggleVisible(log); }
        private void ViewParameters_Click(object sender, RoutedEventArgs e) { ToggleVisible(parameters); }

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
