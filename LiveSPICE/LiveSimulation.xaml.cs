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
    public partial class LiveSimulation : Window, INotifyPropertyChanged
    {
        protected int oversample = 4;
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

        protected SyMath.Expression input;
        public SyMath.Expression Input
        {
            get { return input; }
            set { input = value; NotifyChanged("Input"); }
        }

        protected Circuit.Circuit circuit = null;
        protected Circuit.Simulation simulation = null;
        protected Circuit.TransientSolution solution = null;

        private SyMath.Expression output;
        protected List<Probe> probes = new List<Probe>();
        protected Dictionary<SyMath.Expression, double> arguments = new Dictionary<SyMath.Expression, double>();

        public Log Log { get { return (Log)log.Content; } }
        public Parameters Parameters { get { return (Parameters)parameters.Content; } }
        public AudioStream Audio { get { return (AudioStream)audio.Content; } }
        public Scope Scope { get { return (Scope)scope.Content; } }

        public LiveSimulation(Circuit.Schematic Simulate)
        {
            InitializeComponent();

            Unloaded += (s, e) => Audio.Stop();

            // Make a clone of the schematic so we can mess with it.
            Circuit.Schematic clone = Circuit.Schematic.Deserialize(Simulate.Serialize(), Log);
            clone.Elements.ItemAdded += OnElementAdded;
            clone.Elements.ItemRemoved += OnElementRemoved;
            schematic.Schematic = new SimulationSchematic(clone);
            schematic.Schematic.SelectionChanged += OnProbeSelected;

            // Build the circuit from the schematic.
            try
            {
                circuit = schematic.Schematic.Schematic.Build(Log);
                IEnumerable<Circuit.Component> components = circuit.Components;

                // Find the input expression, the first voltage source that does not evaluate to a constant.
                foreach (Circuit.VoltageSource i in components.OfType<Circuit.VoltageSource>().Where(i => i.IsInput))
                    inputs.Items.Add(new ComboBoxItem()
                    {
                        Content = i.Name,
                        Tag = i.Voltage.Value,
                    });
                inputs.SelectedIndex = 0;

                // Build the output expression from the speakers in the circuit.
                output = SyMath.Add.New(components.OfType<Circuit.Speaker>().Select(i => i.Sound)).Evaluate();
                if (output.IsZero())
                    output = null;

                Parameters.ParameterChanged += (o, e) => arguments[e.Changed.Name] = e.Value;
                Audio.Callback = ProcessSamples;
            }
            catch (System.Exception)
            {
            }
        }

        private void OnElementAdded(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;
                
                Pen p;
                switch (probe.Color)
                {
                    // These two need to be brighter than the normal colors.
                    case Circuit.EdgeType.Red: p = new Pen(new SolidColorBrush(Color.FromRgb(255, 50, 50)), 1.0); break;
                    case Circuit.EdgeType.Blue: p = new Pen(new SolidColorBrush(Color.FromRgb(20, 180, 255)), 1.0); break;
                    default: p = ElementControl.MapToPen(probe.Color); break;
                }
                probe.Signal = new Signal() { Name = probe.V.ToString(), Pen = p };
                Scope.Display.Signals.Add(probe.Signal);

                e.Element.LayoutChanged += (x, y) => ConnectProbes();
            }
            ConnectProbes();
        }

        private void OnElementRemoved(object sender, Circuit.ElementEventArgs e)
        {
            if (e.Element is Circuit.Symbol && ((Circuit.Symbol)e.Element).Component is Probe)
            {
                Probe probe = (Probe)((Circuit.Symbol)e.Element).Component;

                Scope.Display.Signals.Remove(probe.Signal);
            }
            ConnectProbes();
        }

        private void OnProbeSelected(object sender, EventArgs e)
        {
            IEnumerable<Circuit.Symbol> selected = SimulationSchematic.ProbesOf(schematic.Schematic.Selected);
            if (selected.Any())
                Scope.Display.SelectedSignal = ((Probe)selected.First().Component).Signal;
        }

        public void ConnectProbes()
        {
            lock (probes)
            {
                probes.Clear();
                foreach (Probe i in ((SimulationSchematic)schematic.Schematic).Probes.Where(i => i.ConnectedTo != null))
                    probes.Add(i);
            }
        }

        private bool rebuild = true;
        private void ProcessSamples(double[] In, double[] Out, double SampleRate)
        {
            if (rebuild || (simulation != null && (Oversample != simulation.Oversample || SampleRate != (double)simulation.SampleRate)))
            {
                try
                {
                    Circuit.Quantity h = new Circuit.Quantity(Constant.One / (SampleRate * Oversample), Circuit.Units.s);
                    if (solution == null || solution.TimeStep != h)
                    {
                        solution = Circuit.TransientSolution.SolveCircuit(circuit, h, Log);
                        arguments = solution.Parameters.ToDictionary(i => i.Name, i => 0.5);
                        Dispatcher.Invoke(() => Parameters.UpdateControls(solution.Parameters));
                    }
                    simulation = new Circuit.LinqCompiledSimulation(solution, Oversample, Log);
                }
                catch (System.Exception ex)
                {
                    Log.WriteLine(Circuit.MessageType.Error, ex.Message);
                    simulation = null;
                }
                rebuild = false;
            }

            // If there is no simulation, just zero the samples and return.
            if (simulation == null)
            {
                for (int i = 0; i < Out.Length; ++i)
                    Out[i] = 0.0;
                return;
            }

            try
            {
                lock (probes)
                {
                    // Build the signal list.
                    IEnumerable<KeyValuePair<SyMath.Expression, double[]>> signals = probes.Select(i => i.AllocBuffer(Out.Length));
                    if (!ReferenceEquals(output, null))
                        signals = signals.Append(new KeyValuePair<SyMath.Expression, double[]>(output, Out));

                    // Process the samples!
                    if (!ReferenceEquals(Input, null))
                        simulation.Run(Input, In, signals, arguments, Iterations);
                    else
                        simulation.Run(In.Length, signals, arguments, Iterations);

                    // Show the samples on the oscilloscope.
                    Scope.ProcessSignals(Out.Length, probes.Select(i => new KeyValuePair<Signal, double[]>(i.Signal, i.Buffer)), SampleRate);
                }
            }
            catch (Circuit.SimulationDiverged Ex)
            {
                // If the simulation diverged more than one second ago, reset it and hope it doesn't happen again.
                Log.WriteLine(Circuit.MessageType.Error, Ex.Message);
                if ((double)Ex.At > SampleRate)
                    simulation.Reset();
                else
                    simulation = null;
            }
            catch (Exception ex)
            {
                // If there was a more serious error, kill the simulation so the user can fix it.
                Log.WriteLine(Circuit.MessageType.Error, ex.Message);
                simulation = null;
            }
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

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
