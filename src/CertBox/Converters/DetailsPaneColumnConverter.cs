using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace CertBox.Converters
{
    public class DetailsPaneColumnConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasSelection = value != null;
            if (hasSelection)
            {
                // When a certificate is selected: 3:1 ratio with splitter
                return new GridLength[]
                {
                    new(3, GridUnitType.Star),
                    new(5, GridUnitType.Pixel),
                    new(1, GridUnitType.Star)
                };
            }
            else
            {
                // When no certificate is selected: DataGrid takes all space, splitter and details pane hidden
                return new[] { new GridLength(1, GridUnitType.Star), new GridLength(0), new GridLength(0) };
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}