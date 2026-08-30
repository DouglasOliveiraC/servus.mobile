using System.Globalization;
using Servus.Mobile.Services.SwapRequests;

namespace Servus.Mobile.Features.Swaps;

public class StatusBadgeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SwapRequestStatusDto status
            ? status switch
            {
                SwapRequestStatusDto.Pending => Color.FromArgb("#B08A2E"),
                SwapRequestStatusDto.Accepted => Color.FromArgb("#3A6EA5"),
                SwapRequestStatusDto.Approved => Color.FromArgb("#3D6E68"), // Success (verde, independente do BrandPrimary)
                _ => Color.FromArgb("#B5453F")
            }
            : Color.FromArgb("#6B7680");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsPendingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SwapRequestStatusDto status && status == SwapRequestStatusDto.Pending;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsAcceptedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SwapRequestStatusDto status && status == SwapRequestStatusDto.Accepted;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
