using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CertBox.Converters
{
    public class ExpiredColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpired && targetType == typeof(IBrush))
            {
                return isExpired
                    ? new SolidColorBrush(Color.Parse("#FF0000"))
                    : new SolidColorBrush(Color.Parse("#FFFFFF"));
            }

            return new SolidColorBrush(Color.Parse("#FFFFFF")); // Default to white
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}