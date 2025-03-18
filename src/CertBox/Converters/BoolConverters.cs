using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace CertBox.Converters
{
    public static class BoolConverters
    {
        public static readonly IValueConverter Not = new FuncValueConverter<bool, bool>(b => !b);

        public static readonly IValueConverter ToDouble = new FuncValueConverter<bool, double>(b => b ? 1.0 : 0.0);

        public static readonly IValueConverter IsNotNull = new FuncValueConverter<object, bool>(obj => obj != null);

        public static readonly IValueConverter SelectedToWidth = new FuncValueConverter<object, GridLength>(
            obj => obj != null ? new GridLength(300) : new GridLength(0)
        );
    }
}