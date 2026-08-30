using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;

namespace Servus.Mobile.Features.Home;

public static class DashboardPalette
{
    public static ObservableCollection<Brush> Brushes { get; } =
    [
        new SolidColorBrush(Color.FromArgb("#1F3A5F")),
        new SolidColorBrush(Color.FromArgb("#6F8CAA")),
        new SolidColorBrush(Color.FromArgb("#A6BBD1")),
        new SolidColorBrush(Color.FromArgb("#152943")),
        new SolidColorBrush(Color.FromArgb("#C3D0DD")),
        new SolidColorBrush(Color.FromArgb("#42658A")),
        new SolidColorBrush(Color.FromArgb("#E3E6E5")),
        new SolidColorBrush(Color.FromArgb("#6B7680")),
    ];
}
