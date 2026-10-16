using Microsoft.Maui.Controls.Shapes;

namespace Servus.Mobile.Controls;

// Mobile counterpart of servus.web's KpiCard (components/ui/KpiCard.tsx): label + tinted icon,
// big value, muted hint. Accent pairs follow the web's accentStyles table.
public partial class KpiTile : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(KpiTile), string.Empty);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(KpiTile), "—");

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(KpiTile), string.Empty);

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(Geometry), typeof(KpiTile));

    public static readonly BindableProperty AccentProperty =
        BindableProperty.Create(nameof(Accent), typeof(Color), typeof(KpiTile), Color.FromArgb("#1F3A5F"));

    public static readonly BindableProperty AccentSurfaceProperty =
        BindableProperty.Create(nameof(AccentSurface), typeof(Color), typeof(KpiTile), Color.FromArgb("#E7ECF2"));

    public KpiTile()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color Accent
    {
        get => (Color)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public Color AccentSurface
    {
        get => (Color)GetValue(AccentSurfaceProperty);
        set => SetValue(AccentSurfaceProperty, value);
    }
}
