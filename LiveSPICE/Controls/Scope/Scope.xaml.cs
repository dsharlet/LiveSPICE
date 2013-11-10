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
        public SignalCollection Signals { get { return oscilloscope.Signals; } }

        public Signal SelectedSignal { get { return oscilloscope.SelectedSignal; } set { oscilloscope.SelectedSignal = value; NotifyChanged("SelectedSignal"); } }

        public Scope() 
        { 
            InitializeComponent();
            Signals.ItemAdded += signals_ItemAdded;
            Signals.ItemRemoved += signals_ItemRemoved;

            oscilloscope.Signals = Signals;
        }

        void signals_ItemAdded(object sender, SignalEventArgs e)
        {
            ComboBoxItem item = new ComboBoxItem()
            {
                Background = Brushes.DarkGray,
                Foreground = e.Signal.Pen.Brush,
                Content = e.Signal.Name,
                Tag = e.Signal
            };

            e.Signal.Tag = item;

            // Add item to the combo box.
            selectedSignal.Items.Add(item);

            if (!Signals.Contains(SelectedSignal))
            {
                if (Signals.Any())
                    SelectedSignal = Signals.First();
                else if (SelectedSignal != null)
                    SelectedSignal = null;
            }
        }

        void signals_ItemRemoved(object sender, SignalEventArgs e)
        {
            selectedSignal.Items.Remove(e.Signal.Tag);

            if (!Signals.Contains(SelectedSignal))
            {
                if (Signals.Any())
                    SelectedSignal = Signals.First();
                else if (SelectedSignal != null)
                    SelectedSignal = null;
            }
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
