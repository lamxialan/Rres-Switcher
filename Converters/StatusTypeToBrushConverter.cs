using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RRes.Converters
{
    public class StatusTypeToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(16, 185, 129)); // Emerald Green
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(245, 158, 11)); // Amber
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(239, 68, 68));    // Rose Red
        private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(56, 189, 248));    // Sky Blue

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                switch (type.ToLowerInvariant())
                {
                    case "success": return SuccessBrush;
                    case "warning": return WarningBrush;
                    case "error": return ErrorBrush;
                    case "info":
                    default: return InfoBrush;
                }
            }
            return InfoBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
