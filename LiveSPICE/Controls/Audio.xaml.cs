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
using Circuit;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for Audio.xaml
    /// </summary>
    public partial class Audio : UserControl, INotifyPropertyChanged
    {
        protected AudioIo.WaveIo waveIo = null;

        protected Quantity sampleRate = new Quantity(48e3m, Units.Hz);
        public Quantity SampleRate
        {
            get { return sampleRate; }
            set { sampleRate.Set(value); NotifyChanged("SampleRate"); }
        }

        protected Quantity latency = new Quantity(30e-3m, Units.s);
        public Quantity Latency
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
        
        public Audio()
        {
            InitializeComponent();
        }
        
        public void Run(AudioIo.WaveIo.SampleHandler SampleCallback)
        {
            waveIo = new AudioIo.WaveIo(SampleCallback, (int)sampleRate, 1, bitsPerSample, (double)latency);
        }

        public void Stop() 
        {
            if (waveIo != null)
            {
                waveIo.Dispose();
                waveIo = null;
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
