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
    /// Interaction logic for Channel.xaml
    /// </summary>
    public partial class Channel : UserControl, INotifyPropertyChanged
    {
        private IEnumerable<ComboBoxItem> signals;
        public IEnumerable<ComboBoxItem> Signals { get { return signals; } set { signals = value; NotifyChanged("Signals"); } }

        public Channel(Audio.Channel For, IEnumerable<ComboBoxItem> Signals)
        {
            InitializeComponent();

            name.ToolTip = name.Text = For.Name;
            this.Signals = Signals.ToList();

            Signal = Signals.Select(i => (SyMath.Expression)i.Tag).FirstOrDefault();
        }

        public Brush SignalStatus { get { return level.Background; } set { level.Background = value; } }

        private SyMath.Expression signal;
        public SyMath.Expression Signal { get { return signal; } set { signal = value; NotifyChanged("Signal"); } }
        
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
}
