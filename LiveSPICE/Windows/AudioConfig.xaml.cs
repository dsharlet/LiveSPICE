using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for AudioConfiguration.xaml
    /// </summary>
    public partial class AudioConfig : Window, INotifyPropertyChanged
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
                    foreach (Audio.Device i in driver.Devices)
                        devices.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
                    if (devices.Items.Count > 0)
                        Device = driver.Devices.First();
                }

                NotifyChanged(nameof(Driver));
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
                    foreach (Audio.Channel i in device.InputChannels)
                        inputs.Items.Add(new ListBoxItem() { Content = i.Name, Tag = i });
                    foreach (Audio.Channel i in device.OutputChannels)
                        outputs.Items.Add(new ListBoxItem() { Content = i.Name, Tag = i });
                }

                NotifyChanged(nameof(Device));
            }
        }

        public Audio.Channel[] Inputs { get { return inputs.SelectedItems.Cast<ListBoxItem>().Select(i => i.Tag).Cast<Audio.Channel>().ToArray(); } }
        public Audio.Channel[] Outputs { get { return outputs.SelectedItems.Cast<ListBoxItem>().Select(i => i.Tag).Cast<Audio.Channel>().ToArray(); } }

        private bool enabled = true;
        public bool Enabled { get { return enabled; } set { enabled = value; NotifyChanged(nameof(Enabled)); } }

        public bool TestEnabled { get { return Inputs.Any() || Outputs.Any(); } }

        private Audio.Stream stream = null;

        public AudioConfig()
        {
            InitializeComponent();
            RefreshDrivers();

            Settings settings = App.Current.Settings;

            Driver = Audio.Driver.Drivers.FirstOrDefault(i => i.Name == settings.AudioDriver);
            if (Driver == null)
                Driver = Audio.Driver.Drivers.FirstOrDefault();
            if (Driver != null)
            {
                Device = Driver.Devices.FirstOrDefault(i => i.Name == settings.AudioDevice);
                if (Device == null)
                    Device = Driver.Devices.FirstOrDefault();
            }
            foreach (ListBoxItem i in inputs.Items)
                if (settings.AudioInputs.Contains((string)i.Content))
                    inputs.SelectedItems.Add(i);
            foreach (ListBoxItem i in outputs.Items)
                if (settings.AudioOutputs.Contains((string)i.Content))
                    outputs.SelectedItems.Add(i);

            Closed += (o, e) => EndTest(null, null);
        }

        private void OK(object sender, EventArgs e)
        {
            Settings settings = App.Current.Settings;
            settings.AudioDriver = Driver != null ? Driver.Name : "";
            settings.AudioDevice = Device != null ? Device.Name : "";
            settings.AudioInputs = Inputs.Select(i => i.Name).ToArray();
            settings.AudioOutputs = Outputs.Select(i => i.Name).ToArray();

            DialogResult = true;
            Close();
        }

        private void RefreshDrivers()
        {
            drivers.Items.Clear();
            foreach (Audio.Driver i in Audio.Driver.Drivers)
                drivers.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            if (drivers.Items.Count > 0)
                Driver = Audio.Driver.Drivers.First();
        }

        protected Signal signal;

        private void BeginTest(object sender, EventArgs e)
        {
            signal = new Signal();
            scope.Signals.Add(signal);

            stream = Device.Open(Callback, Inputs, Outputs);

            Enabled = false;
        }

        private void Callback(int Count, Audio.SampleBuffer[] In, Audio.SampleBuffer[] Out, double Rate)
        {
            double[] x = new double[Count];

            // Sum the inputs.
            foreach (Audio.SampleBuffer i in In)
                for (int j = 0; j < i.Count; ++j)
                    x[j] += i[j];

            // Write the sum to the outputs.
            foreach (Audio.SampleBuffer i in Out)
                for (int j = 0; j < i.Count; ++j)
                    i[j] = x[j];

            signal.AddSamples(scope.Signals.Clock, x);

            scope.Signals.TickClock(x.Length, Rate);
        }

        private void EndTest(object sender, EventArgs e)
        {
            if (stream != null)
            {
                stream.Stop();
                stream = null;
            }
            scope.Signals.Clear();

            Enabled = true;
        }

        private void inputs_outputs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyChanged(nameof(TestEnabled));
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
