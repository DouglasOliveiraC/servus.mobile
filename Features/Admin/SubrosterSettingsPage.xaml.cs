namespace Servus.Mobile.Features.Admin;

public partial class SubrosterSettingsPage : ContentPage
{
    private readonly SubrosterSettingsViewModel _viewModel;

    public SubrosterSettingsPage(SubrosterSettingsViewModel viewModel)
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
