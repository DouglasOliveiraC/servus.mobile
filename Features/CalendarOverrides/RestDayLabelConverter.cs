using System.Globalization;

namespace Servus.Mobile.Features.CalendarOverrides;

public class RestDayLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Folga/feriado (toque p/ alternar)" : "Dia normal (toque p/ alternar)";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
