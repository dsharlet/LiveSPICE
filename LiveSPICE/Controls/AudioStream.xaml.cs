using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for AudioConfiguration.xaml
    /// </summary>
    public partial class AudioStream : UserControl, INotifyPropertyChanged
    {
        protected double latency = 50.0;//App.Current.Settings.Latency;
        public double Latency
        {
            get { return latency; }
            set 
            {
                latency = value;
                App.Current.Settings.Latency = value;
                OpenStream(); 
                NotifyChanged("Latency"); 
            }
        }

        private Audio.Driver driver;
        public Audio.Driver Driver
        {
            get { return driver; }
            set 
            { 
                driver = value; 
                
                devices.Items.Clear();
                if (value != null)
                {
                    App.Current.Settings.AudioDriver = driver.Name;

                    foreach (Audio.Device i in driver.Devices)
                        devices.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
                    Device = driver.Devices.FindOrDefault(i => i.Name == App.Current.Settings.AudioDevice, driver.Devices.FirstOrDefault());
                    OpenStream();
                }

                NotifyChanged("Driver"); 
            }
        }

        private Audio.Device device;
        public Audio.Device Device
        {
            get { return device; }
            set 
            {
                device = value;

                inputs.Items.Clear();
                outputs.Items.Clear();
                if (device != null)
                {
                    App.Current.Settings.AudioDevice = device.Name;

                    foreach (Audio.Channel i in device.InputChannels)
                        inputs.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
                    foreach (Audio.Channel i in device.OutputChannels)
                        outputs.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });

                    Input = device.InputChannels.FindOrDefault(i => i.Name == App.Current.Settings.AudioInput, device.InputChannels.FirstOrDefault());
                    Output = device.OutputChannels.FindOrDefault(i => i.Name == App.Current.Settings.AudioOutput, device.OutputChannels.FirstOrDefault());
                }

                NotifyChanged("Device"); 
            }
        }

        private Audio.Channel input;
        public Audio.Channel Input
        {
            get { return input; }
            set 
            {
                input = value;
                if (input != null)
                {
                    App.Current.Settings.AudioInput = input.Name;
                    OpenStream();
                }
                NotifyChanged("Input"); 
            }
        }

        private Audio.Channel output;
        public Audio.Channel Output
        {
            get { return output; }
            set
            {
                output = value;
                if (output != null)
                {
                    App.Current.Settings.AudioOutput = output.Name;
                    OpenStream();
                }
                NotifyChanged("Output"); 
            }
        }

        private double inputGain = App.Current.Settings.InputGain;
        public int InputGain 
        {
            get { return (int)Math.Round(20 * Math.Log(inputGain, 10)); }
            set 
            {
                inputGain = Math.Pow(10, value / 20.0);
                App.Current.Settings.InputGain = inputGain; 
                NotifyChanged("InputGain"); 
            }
        }

        private double outputGain = App.Current.Settings.OutputGain;
        public int OutputGain 
        {
            get { return (int)Math.Round(20 * Math.Log(outputGain, 10)); }
            set 
            {
                outputGain = Math.Pow(10, value / 20.0);
                App.Current.Settings.OutputGain = outputGain; 
                NotifyChanged("OutputGain"); 
            }
        }

        public Audio.Stream.SampleHandler Callback = null;

        private Audio.Stream stream = null;

        public AudioStream()
        {
            InitializeComponent();
            RefreshDrivers();
            OpenStream();
        }
                
        private void OpenStream()
        {
            Stop();
            try
            {
                if (Device != null && Input != null && Output != null)
                {
                    stream = Device.Open(SampleCallback, Input, Output, Latency / 1000.0);

                    Settings settings = App.Current.Settings;
                    settings.AudioDriver = Driver.Name;
                    settings.AudioDevice = Device.Name;
                    settings.AudioInput = Input.Name;
                    settings.AudioOutput = Output.Name;
                }
            }
            catch (System.Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        public void Stop()
        {
            if (stream != null)
                stream.Stop();
            stream = null;
        }

        private void SampleCallback(double[] In, double[] Out, double SampleRate)
        {
            // Apply input gain.
            double peak = 0.0;
            for (int i = 0; i < In.Length; ++i)
            {
                double v = In[i];
                v *= inputGain;
                peak = Math.Max(peak, Math.Abs(v));
                In[i] = v;
            }
            Dispatcher.InvokeAsync(() => inputLevel.Background = StatusBrush(peak));

            // Call the callback.
            if (Callback != null)
                Callback(In, Out, SampleRate);

            // Apply output gain.
            peak = 0.0;
            for (int i = 0; i < Out.Length; ++i)
            {
                double v = Out[i];
                v *= outputGain;
                peak = Math.Max(peak, Math.Abs(v));
                Out[i] = v;
            }
            Dispatcher.InvokeAsync(() => outputLevel.Background = StatusBrush(peak));
        }

        private static Brush StatusBrush(double peak)
        {
            if (peak < 0.6)
                return Brushes.Green;
            if (peak < 0.8)
                return Brushes.Yellow;
            if (peak < 0.99)
                return Brushes.Red;
            return Brushes.Black;
        }

        private void RefreshDrivers()
        {
            drivers.Items.Clear();
            foreach (Audio.Driver i in Audio.Driver.Drivers)
                drivers.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            Driver = Audio.Driver.Drivers.FindOrDefault(i => i.Name == App.Current.Settings.AudioDriver, Audio.Driver.Drivers.FirstOrDefault());
        }
        
        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    static class EnumerableExtensions
    {
        public static T FindOrDefault<T>(this IEnumerable<T> This, Func<T, bool> Predicate, T Default)
        {
            T x = This.FirstOrDefault(Predicate);
            if (x != null)
                return x;
            return Default;
        }
    }
}
