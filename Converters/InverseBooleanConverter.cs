using System;
using System.Globalization;
using System.Windows.Data;

namespace RRes.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (targetType == typeof(System.Windows.Visibility))
                {
                    return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                }
                return !b;
            }
            return targetType == typeof(System.Windows.Visibility) ? System.Windows.Visibility.Visible : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return false;
        }
    }
}
