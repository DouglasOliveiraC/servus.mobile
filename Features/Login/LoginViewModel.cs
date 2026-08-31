using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Features.Register;
using Servus.Mobile.Services.Auth;

namespace Servus.Mobile.Features.Login;

public partial class LoginViewModel : ObservableObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? EmailError { get; set; }

    [ObservableProperty]
    public partial string? PasswordError { get; set; }

    [ObservableProperty]
    public partial string? ServerError { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public event EventHandler? LoginSucceeded;

    [RelayCommand]
    private Task ForgotPasswordAsync()
        => Shell.Current.DisplayAlertAsync(
            "Recuperar senha",
            "Fale com o administrador da sua unidade para redefinir sua senha.",
            "OK");

    [RelayCommand]
    private Task ContinueWithGoogleAsync()
        => Shell.Current.DisplayAlertAsync(
            "Entrar com Google",
            "Login com Google ainda não está disponível nesta versão.",
            "OK");

    [RelayCommand]
    private static Task GoToRegisterAsync() => Shell.Current.GoToAsync(nameof(RegisterPage));

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ServerError = null;

        if (!Validate())
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Email.Trim(), Password);
            if (result.Success)
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ServerError = result.ErrorMessage;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy;

    private bool Validate()
    {
        EmailError = string.IsNullOrWhiteSpace(Email)
            ? "Informe seu e-mail."
            : !EmailRegex.IsMatch(Email.Trim())
                ? "Informe um e-mail válido."
                : null;

        PasswordError = string.IsNullOrEmpty(Password)
            ? "Informe sua senha."
            : null;

        return EmailError is null && PasswordError is null;
    }

    partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
}
