using System.Globalization;
using Servus.Mobile.Services.SwapHistory;
using Servus.Mobile.Services.SwapRequests;

namespace Servus.Mobile.Features.Swaps;

public class StatusBadgeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is SwapRequestStatusDto status
            ? status switch
            {
                SwapRequestStatusDto.Pending => Color.FromArgb("#B08A2E"),
                SwapRequestStatusDto.AwaitingAdminApproval => Color.FromArgb("#3A6EA5"),
                SwapRequestStatusDto.Accepted => Color.FromArgb("#3D6E68"), // Success (green, independent of BrandPrimary)
                SwapRequestStatusDto.Declined => Color.FromArgb("#B5453F"),
                _ => Color.FromArgb("#6B7680")
            }
            : Color.FromArgb("#6B7680");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// Same labels as servus.web's swapRequestStatusLabel.
public class SwapStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            SwapRequestStatusDto.Pending => "Pendente",
            SwapRequestStatusDto.AwaitingAdminApproval => "Aguardando aprovação",
            SwapRequestStatusDto.Accepted => "Aceito",
            SwapRequestStatusDto.Declined => "Recusado",
            SwapRequestStatusDto.Cancelled => "Cancelado",
            SwapRequestStatusDto.Expired => "Expirado",
            _ => string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// Same labels as servus.web's swapCauseLabel.
public class SwapCauseLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            SwapCauseDto.PeerSwap => "Troca entre membros",
            SwapCauseDto.AdminSwap => "Troca administrativa",
            _ => string.Empty
        };

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
