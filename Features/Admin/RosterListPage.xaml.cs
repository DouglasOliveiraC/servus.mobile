namespace Servus.Mobile.Features.Admin;

public partial class RosterListPage : ContentPage
{
    private readonly RosterListViewModel _viewModel;

    public RosterListPage(RosterListViewModel viewModel)
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
