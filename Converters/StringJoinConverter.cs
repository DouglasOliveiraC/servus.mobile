using System.Collections;
using System.Globalization;

namespace Servus.Mobile.Converters;

public class StringJoinConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable enumerable)
        {
            return string.Empty;
        }

        var items = enumerable.Cast<object>().Select(o => o?.ToString() ?? string.Empty);
        return string.Join(", ", items);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
