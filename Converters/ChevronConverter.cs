using System.Globalization;

namespace Servus.Mobile.Converters;

// Usado por cabeçalhos de seção retrátil: mostra a seta apontando para baixo quando
// recolhida e para cima quando expandida.
public class ChevronConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "⌃" : "⌄";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
