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
using SyMath;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for Oscilloscope.xaml
    /// </summary>
    public partial class Oscilloscope : UserControl, INotifyPropertyChanged
    {
        //public IEnumerable<string> Signals { get { return scope.Signals.Select(i => i.Name).ToString(); } }

        public Oscilloscope() 
        { 
            InitializeComponent();
            scope.Signals.ItemAdded += Signals_ItemAdded;
            scope.Signals.ItemRemoved += Signals_ItemRemoved;
        }

        public OscilloscopeControl Scope { get { return scope; } }

        void Signals_ItemAdded(object sender, SignalEventArgs e)
        {
            ComboBoxItem item = new ComboBoxItem()
            {
                Background = scope.Background,
                Foreground = e.Signal.Pen.Brush,
                Content = e.Signal.Name,
                Tag = e.Signal
            };

            e.Signal.Tag = item;

            // Add item to the combo box.
            signals.Items.Add(item);
            scope.SelectedSignal = e.Signal;
        }

        void Signals_ItemRemoved(object sender, SignalEventArgs e)
        {
            signals.Items.Remove(e.Signal.Tag);
            signals.SelectedValue = scope.SelectedSignal;
        }
        
        public void ProcessSignals(int SampleCount, IEnumerable<KeyValuePair<Signal, double[]>> Signals)
        {
            scope.ProcessSignals(SampleCount, Signals);
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
