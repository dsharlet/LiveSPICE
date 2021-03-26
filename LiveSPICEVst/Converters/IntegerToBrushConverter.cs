using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using StandardBrushes = System.Windows.Media.Brushes;

namespace LiveSPICEVst.Converters
{
    class IntegerToBrushConverter : IValueConverter
    {
        public List<Brush> Brushes { get; } = new List<Brush>();

        public Brush Default { get; set; } = StandardBrushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int position && position < Brushes.Count ? Brushes[position] : Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
