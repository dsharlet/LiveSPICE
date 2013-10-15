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
        public IEnumerable<SyMath.Expression> Signals { get { return scope.Signals.Keys; } }

        public Oscilloscope() { InitializeComponent(); }

        public void RemoveSignal(SyMath.Expression Signal) 
        {
            OscilloscopeControl.Signal s;
            if (scope.Signals.TryRemove(Signal, out s))
                signals.Items.Remove(s.Tag);

            // If the removed signal is selected, select a different signal.
            if (Signal == (SyMath.Expression)signals.SelectedValue)
            {
                if (scope.Signals.Any())
                    signals.SelectedValue = scope.Signals.First().Key;
                else
                    signals.SelectedValue = null;
            }
        }

        public void AddSignal(SyMath.Expression Signal, Pen Style)
        {
            ComboBoxItem item = new ComboBoxItem()
            {
                Background = scope.Background,
                Foreground = Style.Brush,
                Content = Signal,
            };

            scope.Signals.TryAdd(Signal, new OscilloscopeControl.Signal() { Pen = Style, Tag = item });

            // Add item to the combo box.
            signals.Items.Add(item);
            if (signals.SelectedValue == null)
                signals.SelectedValue = Signal;
        }

        public void ProcessSignals(long LastIndex, IDictionary<SyMath.Expression, double[]> Signals, Circuit.Quantity Rate)
        {
            scope.ProcessSignals(LastIndex, Signals, Rate);
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
