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
    /// Interaction logic for Simulation.xaml
    /// </summary>
    public partial class Simulation : UserControl, INotifyPropertyChanged
    {
        protected int oversample = 8;
        public int Oversample
        {
            get { return oversample; }
            set { oversample = value; NotifyChanged("Oversample"); }
        }

        protected int iterations = 8;
        public int Iterations
        {
            get { return iterations; }
            set { iterations = value; NotifyChanged("Iterations"); }
        }

        protected Circuit.Quantity input = new Circuit.Quantity("Vin[t]", Circuit.Units.V);
        public Circuit.Quantity Input
        {
            get { return input; }
            set { input = value; NotifyChanged("Input"); }
        }

        protected Circuit.Quantity output = new Circuit.Quantity("Vout[t]", Circuit.Units.V);
        public Circuit.Quantity Output
        {
            get { return output; }
            set { output = value; NotifyChanged("Output"); }
        }
        
        public Simulation()
        {
            InitializeComponent();
        }

        protected Circuit.Simulation simulation;

        public void Run(Circuit.Simulation Simulation) { simulation = Simulation; }

        public void Process(double[] Samples, int Rate, Oscilloscope Oscilloscope)
        {
            Oscilloscope.AddSignal(Input.ToString(), Samples, Rate);

            if (simulation != null)
            {
                simulation.Process(input, Samples, output, Samples);
                Oscilloscope.AddSignal(Output.ToString(), Samples);
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
