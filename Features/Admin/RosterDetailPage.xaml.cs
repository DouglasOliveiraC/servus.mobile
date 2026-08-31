namespace Servus.Mobile.Features.Admin;

public partial class RosterDetailPage : ContentPage
{
    private readonly RosterDetailViewModel _viewModel;

    public RosterDetailPage(RosterDetailViewModel viewModel)
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
