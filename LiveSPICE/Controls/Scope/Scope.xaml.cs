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
    public enum ScopeMode
    {
        Oscilloscope,
        Spectrogram,
    }

    /// <summary>
    /// Interaction logic for Oscilloscope.xaml
    /// </summary>
    public partial class Scope : UserControl, INotifyPropertyChanged
    {
        //public IEnumerable<string> Signals { get { return scope.Signals.Select(i => i.Name).ToString(); } }

        public Scope() 
        { 
            InitializeComponent();
            Display.Signals.ItemAdded += Signals_ItemAdded;
            Display.Signals.ItemRemoved += Signals_ItemRemoved;
        }

        public SignalsDisplay Display { get { return display; } }

        void Signals_ItemAdded(object sender, SignalEventArgs e)
        {
            ComboBoxItem item = new ComboBoxItem()
            {
                Background = Display.Background,
                Foreground = e.Signal.Pen.Brush,
                Content = e.Signal.Name,
                Tag = e.Signal
            };

            e.Signal.Tag = item;

            // Add item to the combo box.
            signals.Items.Add(item);
            Display.SelectedSignal = e.Signal;
        }

        void Signals_ItemRemoved(object sender, SignalEventArgs e)
        {
            signals.Items.Remove(e.Signal.Tag);
            signals.SelectedValue = Display.SelectedSignal;
        }
        
        public void ProcessSignals(int SampleCount, double SampleRate)
        {
            Display.ProcessSignals(SampleCount, SampleRate);
        }

        public long Clock { get { return Display.Clock; } }

        public void ClearSignals()
        {
            Display.ClearSignals();
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
