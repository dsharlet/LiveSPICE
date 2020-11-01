using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace LiveSPICEVst
{
    /// <summary>
    /// Interaction logic for SimulationInterface.xaml
    /// </summary>
    public partial class SimulationInterface : UserControl
    {
        public SimulationInterface()
        {
            InitializeComponent();
        }
    }

    public class ObjectTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value == null) ? null : value.GetType();
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
