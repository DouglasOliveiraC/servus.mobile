namespace Servus.Mobile.Features.Unavailability;

public partial class UnavailabilityPage : ContentPage
{
    private readonly UnavailabilityViewModel _viewModel;

    public UnavailabilityPage(UnavailabilityViewModel viewModel)
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
