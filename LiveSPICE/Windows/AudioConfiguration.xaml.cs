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
    public partial class AudioConfiguration : Window, INotifyPropertyChanged
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

        public Audio.Driver Driver
        {
            get { return (Audio.Driver)drivers.SelectedValue; }
            set { drivers.SelectedValue = value; NotifyChanged("Driver"); }
        }

        public Audio.Device Device
        {
            get { return (Audio.Device)devices.SelectedValue; }
            set { drivers.SelectedValue = value; NotifyChanged("Device"); }
        }

        public Audio.Channel InputChannel
        {
            get { return (Audio.Channel)input.SelectedValue; }
            set { input.SelectedValue = value; NotifyChanged("InputChannel"); }
        }

        public Audio.Channel OutputChannel
        {
            get { return (Audio.Channel)output.SelectedValue; }
            set { output.SelectedValue = value; NotifyChanged("OutputChannel"); }
        }
        
        public AudioConfiguration()
        {
            InitializeComponent();

            RefreshDrivers();
        }
                
        private void RefreshDrivers()
        {
            drivers.Items.Clear();
            foreach (Audio.Driver i in Audio.Driver.Drivers)
                drivers.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            if (drivers.Items.Count > 0) drivers.SelectedItem = drivers.Items[0];
        }

        private void drivers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Audio.Driver driver = (Audio.Driver)drivers.SelectedValue;
            devices.Items.Clear();
            foreach (Audio.Device i in driver.Devices)
                devices.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            if (devices.Items.Count > 0) devices.SelectedItem = devices.Items[0];
        }

        private void devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Audio.Device device = (Audio.Device)devices.SelectedValue;
            input.Items.Clear();
            output.Items.Clear();
            foreach (Audio.Channel i in device.InputChannels)
                input.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            foreach (Audio.Channel i in device.OutputChannels)
                output.Items.Add(new ComboBoxItem() { Content = i.Name, Tag = i });
            if (input.Items.Count > 0) input.SelectedItem = input.Items[0];
            if (output.Items.Count > 0) output.SelectedItem = output.Items[0];
        }

        private void input_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void output_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
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
