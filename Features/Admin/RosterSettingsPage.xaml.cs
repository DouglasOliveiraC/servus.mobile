namespace Servus.Mobile.Features.Admin;

public partial class RosterSettingsPage : ContentPage
{
    private readonly RosterSettingsViewModel _viewModel;

    public RosterSettingsPage(RosterSettingsViewModel viewModel)
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
