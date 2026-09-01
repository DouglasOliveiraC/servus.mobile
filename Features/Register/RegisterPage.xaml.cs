using Servus.Mobile.Services.Navigation;

namespace Servus.Mobile.Features.Register;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterViewModel _viewModel;
    private readonly INavigationService _navigationService;

    public RegisterPage(RegisterViewModel viewModel, INavigationService navigationService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationService = navigationService;
        BindingContext = _viewModel;
        _viewModel.RegisterSucceeded += OnRegisterSucceeded;
    }

    private async void OnRegisterSucceeded(object? sender, EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync("Conta criada", "Sua conta foi criada. Entre com o e-mail e a senha que você acabou de definir.", "OK");
        await _navigationService.GoBackAsync();
    }
}
