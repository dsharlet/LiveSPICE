using System.Windows;
using System.Windows.Controls;
using SimulationStatusEnum = LiveSPICE.Common.SimulationStatus;

namespace LiveSPICE.UI.Controls
{
    /// <summary>
    /// Interaction logic for SimulationStatus.xaml
    /// </summary>
    public partial class SimulationStatus : UserControl
    {
        public SimulationStatus()
        {
            InitializeComponent();
        }

        public SimulationStatusEnum Status
        {
            get { return (SimulationStatusEnum)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Status.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(SimulationStatusEnum), typeof(SimulationStatus), new PropertyMetadata(SimulationStatusEnum.Ready));


        public double CpuLoad
        {
            get { return (double)GetValue(CpuLoadProperty); }
            set { SetValue(CpuLoadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CpuLoad.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CpuLoadProperty =
            DependencyProperty.Register("CpuLoad", typeof(double), typeof(SimulationStatus), new PropertyMetadata(0d));

    }
}
