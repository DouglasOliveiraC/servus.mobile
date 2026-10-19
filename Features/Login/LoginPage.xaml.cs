using Servus.Mobile.Services.Navigation;

namespace Servus.Mobile.Features.Login;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private readonly INavigationService _navigationService;

    public LoginPage(LoginViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationService = navigationService;
        BindingContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Not awaited: it must never hold up the session restore or the form.
        _ = _viewModel.WarmUpAsync();
        await _viewModel.TryRestoreSessionAsync();
    }

    private async void OnLoginSucceeded(object? sender, EventArgs e)
    {
        await _navigationService.GoToRootAsync(Routes.Dashboard);
    }
}
