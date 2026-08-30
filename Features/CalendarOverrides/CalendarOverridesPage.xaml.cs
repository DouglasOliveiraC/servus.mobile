namespace Servus.Mobile.Features.CalendarOverrides;

public partial class CalendarOverridesPage : ContentPage
{
    private readonly CalendarOverridesViewModel _viewModel;

    public CalendarOverridesPage(CalendarOverridesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}
