using Avalonia.Data.Converters;

namespace CertBox.Converters
{
    public class BoolToClassConverter : IValueConverter
    {
        public static readonly BoolToClassConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool and true && parameter is string className)
            {
                return new[] { className };
            }

            return Array.Empty<string>();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}