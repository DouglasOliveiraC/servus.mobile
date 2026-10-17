namespace Servus.Mobile.Controls;

public partial class SearchField : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SearchField), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchField), "Buscar");

    public SearchField()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // The borderless entry has no focus indicator of its own, so the pill carries it.
    private void OnInputFocused(object? sender, FocusEventArgs e) =>
        Pill.Stroke = (Color)Application.Current!.Resources["BrandPrimary"];

    private void OnInputUnfocused(object? sender, FocusEventArgs e) =>
        Pill.Stroke = (Color)Application.Current!.Resources["Neutral200"];
}
