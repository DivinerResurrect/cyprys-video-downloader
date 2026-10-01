using System.Globalization;
using System.Windows.Data;

namespace VideoDownloader.Converters;

public sealed class EqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true || parameter?.ToString() is not string text)
            return Binding.DoNothing;

        return targetType.IsEnum ? Enum.Parse(targetType, text) : text;
    }
}
