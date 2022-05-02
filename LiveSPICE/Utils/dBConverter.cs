using System;
using System.Globalization;
using System.Windows.Data;

namespace LiveSPICE
{
    class dBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 20 * Math.Log((double)value, 10);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && double.TryParse(s, out var dbValue))
            {
                return Math.Pow(10, dbValue / 20);
            }
            return value;
        }
    }
}
