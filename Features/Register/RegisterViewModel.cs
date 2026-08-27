using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Auth;

namespace Servus.Mobile.Features.Register;

public partial class RegisterViewModel : ObservableObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int MinPasswordLength = 8;

    private readonly IAuthService _authService;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string? nameError;

    [ObservableProperty]
    private string? emailError;

    [ObservableProperty]
    private string? passwordError;

    [ObservableProperty]
    private string? confirmPasswordError;

    [ObservableProperty]
    private string? serverError;

    [ObservableProperty]
    private bool isBusy;

    public event EventHandler? RegisterSucceeded;

    [RelayCommand]
    private static Task GoBackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        ServerError = null;

        if (!Validate())
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.RegisterAsync(Name.Trim(), Email.Trim(), Password);
            if (result.Success)
            {
                RegisterSucceeded?.Invoke(this, EventArgs.Empty);
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

    private bool CanRegister() => !IsBusy;

    private bool Validate()
    {
        NameError = string.IsNullOrWhiteSpace(Name)
            ? "Informe seu nome."
            : null;

        EmailError = string.IsNullOrWhiteSpace(Email)
            ? "Informe seu e-mail."
            : !EmailRegex.IsMatch(Email.Trim())
                ? "Informe um e-mail válido."
                : null;

        PasswordError = string.IsNullOrEmpty(Password)
            ? "Informe uma senha."
            : Password.Length < MinPasswordLength
                ? $"A senha deve ter pelo menos {MinPasswordLength} caracteres."
                : null;

        ConfirmPasswordError = string.IsNullOrEmpty(ConfirmPassword)
            ? "Confirme sua senha."
            : ConfirmPassword != Password
                ? "As senhas não coincidem."
                : null;

        return NameError is null && EmailError is null && PasswordError is null && ConfirmPasswordError is null;
    }

    partial void OnIsBusyChanged(bool value) => RegisterCommand.NotifyCanExecuteChanged();
}
