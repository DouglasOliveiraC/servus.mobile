using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Telemetry;

// Color/Colors: Microsoft.Maui.Graphics (via Microsoft.Maui.Controls global usings)

namespace Servus.Mobile.Features.Register;

public partial class RegisterViewModel : BaseViewModel
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const int MinPasswordLength = 8;

    private readonly IAuthService _authService;

    public RegisterViewModel(IAuthService authService, INavigationService navigationService, ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _authService = authService;
    }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? EmailError { get; set; }

    [ObservableProperty]
    public partial string? PasswordError { get; set; }

    [ObservableProperty]
    public partial string? ConfirmPasswordError { get; set; }

    [ObservableProperty]
    public partial string? ServerError { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial int PasswordStrength { get; set; }

    [ObservableProperty]
    public partial string PasswordStrengthLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Color PasswordStrengthColor { get; set; } = Colors.Transparent;

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
    private Task GoBackAsync() => NavigationService.GoBackAsync();

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

    // Espelha src/Servus.Domain/Rules/PasswordPolicy.cs — não há projeto compartilhável
    // entre Mobile e backend, então qualquer mudança na regra precisa ser replicada
    // nos dois lados (e também em passwordPolicy.ts no Web).
    private static bool IsPasswordComplex(string value) =>
        value.Length >= MinPasswordLength
        && value.Any(char.IsLower)
        && value.Any(char.IsUpper)
        && value.Any(char.IsDigit);

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
            : !IsPasswordComplex(Password)
                ? "A senha deve ter pelo menos 8 caracteres, incluindo letra maiúscula, minúscula e número."
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
