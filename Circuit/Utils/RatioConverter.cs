using System;
using System.ComponentModel;

namespace Circuit
{
    class RatioConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
                return Ratio.Parse((string)value);
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return ((Ratio)value).ToString();
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (value is string)
            {
                try
                {
                    Ratio.Parse((string)value);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return base.IsValid(context, value);
        }
    }
}
