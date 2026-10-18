using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Features.Login;

public partial class LoginViewModel : BaseViewModel
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAuthService _authService;
    private readonly ITelemetryService _telemetry;

    public LoginViewModel(IAuthService authService, INavigationService navigationService, ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _authService = authService;
        _telemetry = telemetry;
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

    private bool _restoreAttempted;

    // Only on the first appearance: after a logout or an expired session the Login page shows
    // again, and restoring there would sign straight back in.
    public async Task TryRestoreSessionAsync()
    {
        if (_restoreAttempted)
        {
            return;
        }

        _restoreAttempted = true;
        IsBusy = true;
        try
        {
            if (await _authService.TryRestoreSessionAsync())
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var email = await Shell.Current.DisplayPromptAsync(
            "Recuperar senha",
            "Informe o e-mail da sua conta. Enviaremos um link para redefinir a senha.",
            "Enviar",
            "Cancelar",
            keyboard: Keyboard.Email,
            initialValue: Email.Trim());

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (!EmailRegex.IsMatch(email.Trim()))
        {
            await Shell.Current.DisplayAlertAsync("Recuperar senha", "Informe um e-mail válido.", "OK");
            return;
        }

        var result = await _authService.RequestPasswordResetAsync(email.Trim());
        await Shell.Current.DisplayAlertAsync(
            "Recuperar senha",
            result.Success
                ? "Se houver uma conta com esse e-mail, você vai receber o link em instantes. Verifique também a caixa de spam."
                : result.ErrorMessage,
            "OK");
    }

    [RelayCommand]
    private Task ContinueWithGoogleAsync()
        => Shell.Current.DisplayAlertAsync(
            "Entrar com Google",
            "Login com Google ainda não está disponível nesta versão.",
            "OK");

    [RelayCommand]
    private Task GoToRegisterAsync() => NavigationService.GoToAsync(Routes.Register);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ServerError = null;

        if (!Validate())
        {
            // Erro de preenchimento não chega a ser tentativa de login: contá-lo junto
            // inflaria a taxa de falha do fluxo com quem só esqueceu de digitar a senha.
            return;
        }

        // Não usa RunTrackedAsync: o IAuthService devolve Success=false SEM lançar, e o
        // wrapper só sabe transformar exceção em falha — todo login recusado sairia como
        // sucesso (R5 no relatório 10). Aqui o resultado vem do retorno, não da ausência de
        // exceção.
        using var _ = _telemetry.StartOperation("auth.login");
        var stopwatch = Stopwatch.StartNew();
        var outcome = ClientOutcome.Failed;

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Email.Trim(), Password);
            if (result.Success)
            {
                outcome = ClientOutcome.Succeeded;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ServerError = result.ErrorMessage;
            }
        }
        catch (OperationCanceledException)
        {
            outcome = ClientOutcome.Cancelled;
            throw;
        }
        finally
        {
            IsBusy = false;
            stopwatch.Stop();

            // Sem ReasonCode a partir de result.ErrorMessage: a mensagem é texto para a
            // pessoa, muda com tradução e não serve como categoria. O motivo real da recusa
            // quem tem é o servidor, que já emite auth.login.failed com o código de catálogo.
            _telemetry.TrackEvent(
                ClientEventNames.UiActionCompleted,
                outcome,
                stopwatch.Elapsed,
                extra: new Dictionary<string, string> { ["Action"] = "auth.login" });
        }
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task TryDemoAsync()
    {
        ServerError = null;

        // Same shape as LoginAsync: the service reports refusal through the result, not an
        // exception, so the outcome is read from it.
        using var _ = _telemetry.StartOperation("auth.demo");
        var stopwatch = Stopwatch.StartNew();
        var outcome = ClientOutcome.Failed;

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsDemoAsync();
            if (result.Success)
            {
                outcome = ClientOutcome.Succeeded;
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ServerError = result.ErrorMessage;
            }
        }
        catch (OperationCanceledException)
        {
            outcome = ClientOutcome.Cancelled;
            throw;
        }
        finally
        {
            IsBusy = false;
            stopwatch.Stop();

            _telemetry.TrackEvent(
                ClientEventNames.UiActionCompleted,
                outcome,
                stopwatch.Elapsed,
                extra: new Dictionary<string, string> { ["Action"] = "auth.demo" });
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

    partial void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        TryDemoCommand.NotifyCanExecuteChanged();
    }
}
