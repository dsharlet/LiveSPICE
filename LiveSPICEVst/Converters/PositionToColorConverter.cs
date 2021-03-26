using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace LiveSPICEVst.Converters
{
    class PositionToColorConverter : IValueConverter
    {
        public List<Brush> Brushes { get; } = new List<Brush>();

        public Brush Default { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int position)
            {
                return Brushes[position];
            }
            return Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
