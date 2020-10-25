using System;
using System.Globalization;
using System.Windows.Data;

namespace LiveSPICE
{
    // Converter to help enable controls when an object is null.
    class ObjectNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return !object.Equals(value, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }
}
