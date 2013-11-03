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
                        outputs.Items.Add(new ListBoxItem() { Content = i.Name, Tag = i });

                    Input = device.InputChannels.FindOrDefault(i => i.Name == App.Current.Settings.AudioInput, device.InputChannels.FirstOrDefault());
                    List<ListBoxItem> selected = new List<ListBoxItem>();
                    foreach (ListBoxItem i in outputs.Items)
                    {
                        if (App.Current.Settings.AudioOutput.Contains(i.Content))
                            selected.Add(i);
                    }
                    outputs.SelectedItems.Clear();
                    foreach (ListBoxItem i in selected)
                        outputs.SelectedItems.Add(i);
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

        public Audio.Channel[] Inputs { get { return new [] { input }; } }
        public Audio.Channel[] Outputs { get { return outputs.SelectedItems.Cast<ListBoxItem>().Select(i => i.Tag).Cast<Audio.Channel>().ToArray(); } }

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


        public delegate void SampleHandler(Audio.SampleBuffer In, Audio.SampleBuffer Out, double SampleRate);

        public SampleHandler Callback = null;

        private Audio.Stream stream = null;
        public Audio.Stream Stream { get { return stream; } }

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
                if (Device != null && Input != null)
                {
                    stream = Device.Open(SampleCallback, Inputs, Outputs);

                    Settings settings = App.Current.Settings;
                    settings.AudioDriver = Driver.Name;
                    settings.AudioDevice = Device.Name;
                    settings.AudioInput = Input.Name;
                    settings.AudioOutput = Outputs.Select(i => i.Name).ToArray();
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

        private void SampleCallback(Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double SampleRate)
        {
            // Apply input gain.
            double peak = 0.0;

            using (Audio.SamplesLock samples = new Audio.SamplesLock(In[0], true, true))
            {
                for (int i = 0; i < samples.Count; ++i)
                {
                    double v = samples[i];
                    peak = Math.Max(peak, Math.Abs(v));
                    v *= inputGain;
                    samples[i] = v;
                }
            }
            Dispatcher.InvokeAsync(() => inputLevel.Background = StatusBrush(peak));

            // Call the callback.
            if (Callback != null)
                Callback(In[0], Out.Any() ? Out[0] : null, SampleRate);

            // Apply output gain.
            if (Out.Any())
            {
                peak = 0.0;

                using (Audio.SamplesLock samples = new Audio.SamplesLock(Out[0], true, true))
                {
                    for (int i = 0; i < samples.Count; ++i)
                    {
                        double v = samples[i];
                        v *= outputGain;
                        peak = Math.Max(peak, Math.Abs(v));
                        samples[i] = v;
                    }
                }
                Dispatcher.InvokeAsync(() => outputLevel.Background = StatusBrush(peak));

                Out[0].SyncRaw();
                for (int i = 1; i < Out.Length; ++i)
                    Out[i].Copy(Out[0]);
            }
        }

        private void OutputsChanged(object sender, EventArgs e)
        {
            OpenStream();
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
            new Asio.Driver();
            drivers.Items.Clear();
            foreach (Audio.Driver i in Audio.Driver.Drivers)
                drivers.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            Driver = Audio.Driver.Drivers.FindOrDefault(i => i.Name == App.Current.Settings.AudioDriver, Audio.Driver.Drivers.FirstOrDefault());
        }

        private void ShowControlPanel(object sender, EventArgs e)
        {
            Audio.Device d = Device;
            if (d != null)
                d.ShowControlPanel();
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
