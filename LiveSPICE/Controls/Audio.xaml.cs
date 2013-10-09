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
    /// Interaction logic for Audio.xaml
    /// </summary>
    public partial class Audio : UserControl, INotifyPropertyChanged
    {
        protected AudioIo.WaveIo waveIo = null;

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

        protected Circuit.Quantity inputGain = new Circuit.Quantity(1, Circuit.Units.None);
        public Circuit.Quantity InputGain
        {
            get { return inputGain; }
            set { inputGain.Set(value); NotifyChanged("InputGain"); }
        }

        protected Circuit.Quantity outputGain = new Circuit.Quantity(1, Circuit.Units.None);
        public Circuit.Quantity OutputGain
        {
            get { return outputGain; }
            set { outputGain.Set(value); NotifyChanged("OutputGain"); }
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
