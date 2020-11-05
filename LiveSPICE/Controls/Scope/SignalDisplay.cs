using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICE
{
    public class SignalDisplay : Control, INotifyPropertyChanged
    {
        static SignalDisplay() { DefaultStyleKeyProperty.OverrideMetadata(typeof(SignalDisplay), new FrameworkPropertyMetadata(typeof(SignalDisplay))); }

        protected SignalCollection signals = new SignalCollection();
        public SignalCollection Signals
        {
            get { return signals; }
            set
            {
                signals = value;
                InvalidateVisual();
                NotifyChanged(nameof(Signals));
            }
        }

        private System.Timers.Timer refreshTimer;

        public SignalDisplay()
        {
            refreshTimer = new System.Timers.Timer()
            {
                Interval = 16,  // 60 Hz
                AutoReset = true,
                Enabled = true,
            };
            refreshTimer.Elapsed +=
                (o, e) => Dispatcher.InvokeAsync(() => InvalidateVisual(), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            refreshTimer.Start();
        }

        private Signal selected;
        public Signal SelectedSignal { get { return selected; } set { selected = value; NotifyChanged(nameof(SelectedSignal)); } }

        public void Clear()
        {
            signals.Clear();
            InvalidateVisual();
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}