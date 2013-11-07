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
                    foreach (Audio.Channel i in device.InputChannels)
                        inputs.Items.Add(new ListBoxItem() { Content = i.Name, Tag = i });
                    foreach (Audio.Channel i in device.OutputChannels)
                        outputs.Items.Add(new ListBoxItem() { Content = i.Name, Tag = i });
                }

                NotifyChanged("Device"); 
            }
        }
        
        public Audio.Channel[] Inputs { get { return inputs.SelectedItems.Cast<ListBoxItem>().Select(i => i.Tag).Cast<Audio.Channel>().ToArray(); } }
        public Audio.Channel[] Outputs { get { return outputs.SelectedItems.Cast<ListBoxItem>().Select(i => i.Tag).Cast<Audio.Channel>().ToArray(); } }

        private bool enabled = true;
        public bool Enabled { get { return enabled; } set { enabled = value; NotifyChanged("Enabled"); } }

        private Audio.Stream stream = null;

        public AudioConfig()
        {
            InitializeComponent();
            RefreshDrivers();

            Settings settings = App.Current.Settings;

            Driver = Audio.Driver.Drivers.FirstOrDefault(i => i.Name == settings.AudioDriver);
            if (Driver == null)
                Driver = Audio.Driver.Drivers.First();
            Device = Driver.Devices.FirstOrDefault(i => i.Name == settings.AudioDevice);
            if (Device == null && Driver != null)
                Device = Driver.Devices.First();
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
            settings.AudioDriver = Driver.Name;
            settings.AudioDevice = Device.Name;
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

        private void ShowControlPanel(object sender, EventArgs e)
        {
            Audio.Device d = Device;
            if (d != null)
                d.ShowControlPanel();
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
                using (Audio.SamplesLock y = new Audio.SamplesLock(i, true, false))
                    for (int j = 0; j < y.Count; ++j)
                        x[j] += y[j];

            // Write the sum to the outputs.
            foreach (Audio.SampleBuffer i in Out)
                using (Audio.SamplesLock y = new Audio.SamplesLock(i, false, true))
                    for (int j = 0; j < y.Count; ++j)
                        y[j] = x[j];

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
        
        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
