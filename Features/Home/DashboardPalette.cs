using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;

namespace Servus.Mobile.Features.Home;

// Hues alternate (navy / teal / steel / amber ...) because slices are sorted by load, so
// neighbours often hold near-equal values and an all-blue ramp made them indistinguishable.
// Every tone is dark enough for the white value labels and for the legend swatches on a white card.
public static class DashboardPalette
{
    public static ObservableCollection<Brush> Brushes { get; } =
    [
        new SolidColorBrush(Color.FromArgb("#1F3A5F")),
        new SolidColorBrush(Color.FromArgb("#3D6E68")),
        new SolidColorBrush(Color.FromArgb("#6F8CAA")),
        new SolidColorBrush(Color.FromArgb("#C27A2C")),
        new SolidColorBrush(Color.FromArgb("#6B7680")),
        new SolidColorBrush(Color.FromArgb("#42658A")),
        new SolidColorBrush(Color.FromArgb("#6E9E97")),
        new SolidColorBrush(Color.FromArgb("#8C6A4F")),
    ];
}
