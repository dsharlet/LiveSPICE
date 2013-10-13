using System;
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
using SyMath;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for Simulation.xaml
    /// </summary>
    public partial class Simulation : Window, INotifyPropertyChanged
    {
        protected Circuit.Quantity sampleRate = new Circuit.Quantity(48e3m, Circuit.Units.Hz);
        public Circuit.Quantity SampleRate
        {
            get { return sampleRate; }
            set { sampleRate.Set(value); NotifyChanged("SampleRate"); }
        }

        protected Circuit.Quantity latency = new Circuit.Quantity(30e-3m, Circuit.Units.s);
        public Circuit.Quantity Latency
        {
            get { return latency; }
            set { latency.Set(value); NotifyChanged("Latency"); }
        }

        protected int bitsPerSample = 16;
        public int BitsPerSample
        {
            get { return bitsPerSample; }
            set { bitsPerSample = value; NotifyChanged("BitsPerSample"); }
        }

        protected Circuit.Quantity inputGain = new Circuit.Quantity(1, Circuit.Units.None);
        public Circuit.Quantity InputGain
        {
            get { return inputGain; }
            set { inputGain.Set(value); NotifyChanged("InputGain"); }
        }

        protected Circuit.Quantity outputGain = new Circuit.Quantity(1, Circuit.Units.None);
        public Circuit.Quantity OutputGain
        {
            get { return outputGain; }
            set { outputGain.Set(value); NotifyChanged("OutputGain"); }
        }

        protected Circuit.Simulation simulation = null;
        protected AudioIo.WaveIo waveIo = null;

        protected Dictionary<SyMath.Expression, double[]> signals = new Dictionary<SyMath.Expression, double[]>();

        public Simulation(Circuit.Schematic Simulate, Circuit.Quantity SampleRate, int BitsPerSample, Circuit.Quantity Latency)
        {
            InitializeComponent();

            Closed += OnClosed;

            // Make a clone of the schematic so we can mess with it.
            Circuit.Schematic clone = Circuit.Schematic.Deserialize(Simulate.Serialize(), log);

            clone.Elements.ItemAdded += RefreshProbes;
            clone.Elements.ItemRemoved += RefreshProbes;

            schematic.Schematic = new SimulationSchematic(clone);
            sampleRate = SampleRate;

            waveIo = new AudioIo.WaveIo(ProcessSamples, (int)sampleRate, 1, bitsPerSample, (double)latency);

            Build();
        }

        public void RefreshProbes(object sender, Circuit.ElementEventArgs e)
        {
            lock (signals)
            {
                Probe[] probes = ((SimulationSchematic)schematic.Schematic).Probes.ToArray();

                signals = probes.ToDictionary(i => i.V, i => new double[0]);
            }
        }
        
        protected void Build()
        {
            try
            {
                Circuit.Circuit circuit = schematic.Schematic.Schematic.Build(log);
                simulation = new Circuit.Simulation(circuit, sampleRate, parameters.Oversample, log);
            }
            catch (System.Exception ex)
            {
                log.WriteLine(Circuit.MessageType.Error, ex.Message);
            }
        }
        
        private void ProcessSamples(double[] Samples, int Rate)
        {
            // If there is no simulation, just zero the samples and return.
            if (simulation == null)
            {
                for (int i = 0; i < Samples.Length; ++i)
                    Samples[i] = 0.0;
                return;
            }

            try
            {
                // Apply input gain.
                double inputGain = (double)InputGain;
                if (System.Math.Abs(inputGain - 1.0) > 1e-2)
                    for (int i = 0; i < Samples.Length; ++i)
                        Samples[i] *= inputGain;

                lock (signals)
                {
                    // Build the signal list.
                    foreach (SyMath.Expression i in signals.Keys.ToArray())
                        if (signals[i].Length < Samples.Length)
                            signals[i] = new double[Samples.Length];
                    // Send the output samples to the Samples array.
                    signals[parameters.Output.Value] = Samples;

                    // Process the samples!
                    simulation.Process(parameters.Input, Samples, signals, parameters.Iterations);

                    // Show the samples on the oscilloscope.
                    oscilloscope.ProcessSignals(signals, new Circuit.Quantity(Rate, Circuit.Units.Hz));
                }

                // Apply output gain.
                double outputGain = (double)OutputGain;
                if (System.Math.Abs(outputGain - 1.0) > 1e-2)
                    for (int i = 0; i < Samples.Length; ++i)
                        Samples[i] *= outputGain;
            }
            catch (OverflowException ex)
            {
                // If the simulation diverged, reset it and hope it doesn't happen again.
                log.WriteLine(Circuit.MessageType.Error, ex.Message);
                simulation.Reset();
            }
            catch (Exception ex)
            {
                // If there was a more serious error, kill the simulation so the user can fix it.
                log.WriteLine(Circuit.MessageType.Error, ex.Message);
                simulation = null;
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            waveIo.Dispose();
            waveIo = null;
        }

        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e) { Build(); }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
