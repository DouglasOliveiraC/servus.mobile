using System.Globalization;

namespace Servus.Mobile.Converters;

// Usado por barras de indicador segmentado (ex: força de senha): a barra N acende
// quando value >= ConverterParameter (o índice/threshold daquela barra, 1-based).
public class IntAtLeastConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && parameter is not null && int.TryParse(parameter.ToString(), out var threshold) && i >= threshold;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
