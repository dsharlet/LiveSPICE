using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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

        private DispatcherTimer refreshTimer;

        public SignalDisplay()
        {
            refreshTimer = new DispatcherTimer(DispatcherPriority.DataBind)
            {
                Interval = TimeSpan.FromMilliseconds(32),  // 60 Hz
                IsEnabled = true,
            };
            refreshTimer.Tick +=
                (o, e) => InvalidateVisual();
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