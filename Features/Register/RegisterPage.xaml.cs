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
        // Texto propositalmente igual para e-mail novo e já cadastrado — a API responde o mesmo
        // nos dois casos para não revelar quem tem conta, e o alerta não pode desfazer isso.
        await Shell.Current.DisplayAlertAsync(
            "Verifique seu e-mail",
            "Enviamos um e-mail com as instruções para ativar seu acesso. Confirme seu e-mail para poder entrar.",
            "OK");
        await _navigationService.GoBackAsync();
    }
}
