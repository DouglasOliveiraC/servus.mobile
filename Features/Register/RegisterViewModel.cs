using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Auth;

// Color/Colors: Microsoft.Maui.Graphics (via Microsoft.Maui.Controls global usings)

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

    [ObservableProperty]
    private int passwordStrength;

    [ObservableProperty]
    private string passwordStrengthLabel = string.Empty;

    [ObservableProperty]
    private Color passwordStrengthColor = Colors.Transparent;

    public event EventHandler? RegisterSucceeded;

    partial void OnPasswordChanged(string value) => UpdatePasswordStrength(value);

    // Heurística simples de força (0-4): comprimento mínimo + presença de minúscula,
    // maiúscula, dígito e símbolo. Cada critério atendido soma um ponto.
    private void UpdatePasswordStrength(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            PasswordStrength = 0;
            PasswordStrengthLabel = string.Empty;
            PasswordStrengthColor = Colors.Transparent;
            return;
        }

        var score = 0;
        if (value.Length >= MinPasswordLength) score++;
        if (value.Any(char.IsLower)) score++;
        if (value.Any(char.IsUpper)) score++;
        if (value.Any(char.IsDigit)) score++;
        if (value.Any(c => !char.IsLetterOrDigit(c))) score++;

        PasswordStrength = score;
        (PasswordStrengthLabel, PasswordStrengthColor) = score switch
        {
            <= 1 => ("Muito fraca", Color.FromArgb("#B5453F")),
            2 => ("Fraca", Color.FromArgb("#C97A3D")),
            3 => ("Razoável", Color.FromArgb("#C9A63D")),
            4 => ("Forte", Color.FromArgb("#5C9A63")),
            _ => ("Muito forte", Color.FromArgb("#3D6E68")) // Success (verde, independente do BrandPrimary)
        };
    }

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
            var result = await _authService.RegisterAsync(CapitalizeName(Name), Email.Trim(), Password);
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

    // "joão da silva" -> "João Da Silva": capitaliza cada palavra do nome antes de
    // enviar, já que o app não impõe formatação enquanto a pessoa digita.
    private static string CapitalizeName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0) return trimmed;

        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            words[i] = char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        }

        return string.Join(' ', words);
    }

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
