using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Members;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Users;

namespace Servus.Mobile.Features.Profile;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IRosterService _rosterService;
    private readonly IMemberService _memberService;

    private Guid _userId;

    public ProfileViewModel(
        IAuthService authService,
        IUserService userService,
        IRosterService rosterService,
        IMemberService memberService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _authService = authService;
        _userService = userService;
        _rosterService = rosterService;
        _memberService = memberService;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? LinkedMemberName { get; set; }

    [ObservableProperty]
    public partial string? DisplayNameMessage { get; set; }

    [ObservableProperty]
    public partial string CurrentPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmNewPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? PasswordMessage { get; set; }

    [ObservableProperty]
    public partial bool PasswordMessageIsError { get; set; }

    [ObservableProperty]
    public partial bool IsChangePasswordExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsChangeEmailExpanded { get; set; }

    [ObservableProperty]
    public partial string NewEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailChangePassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? EmailChangeMessage { get; set; }

    [ObservableProperty]
    public partial bool EmailChangeMessageIsError { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user is null)
            {
                HasError = true;
                return;
            }

            _userId = user.Id;
            var userDetails = await _userService.GetByIdAsync(_userId);
            Email = userDetails.Email;
            DisplayName = userDetails.DisplayName;

            var myMemberId = await _rosterService.GetMyMemberIdAsync();
            if (myMemberId is not null)
            {
                var member = await _memberService.GetMyMemberAsync();
                LinkedMemberName = member?.Name;
            }
            else
            {
                LinkedMemberName = null;
            }
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveDisplayNameAsync()
    {
        DisplayNameMessage = null;
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayNameMessage = "Informe um nome válido.";
            return;
        }

        try
        {
            await _userService.UpdateDisplayNameAsync(_userId, DisplayName.Trim());
            DisplayNameMessage = "Nome atualizado com sucesso.";
        }
        catch (Exception)
        {
            DisplayNameMessage = "Não foi possível atualizar o nome.";
        }
    }

    [RelayCommand]
    private void ToggleChangePassword()
    {
        IsChangePasswordExpanded = !IsChangePasswordExpanded;
        if (!IsChangePasswordExpanded)
        {
            // Fechar limpa os campos: evita deixar uma senha digitada exposta em memória
            // na tela depois que a pessoa recolhe a seção sem concluir a troca.
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            PasswordMessage = null;
        }
    }

    // Espelha src/Servus.Domain/Rules/PasswordPolicy.cs — não há projeto compartilhável
    // entre Mobile e backend, então qualquer mudança na regra precisa ser replicada
    // nos dois lados (e também em passwordPolicy.ts no Web e em RegisterViewModel.cs).
    private static bool IsPasswordComplex(string value) =>
        value.Length >= 8
        && value.Any(char.IsLower)
        && value.Any(char.IsUpper)
        && value.Any(char.IsDigit);

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        PasswordMessage = null;

        if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
        {
            PasswordMessage = "Preencha a senha atual e a nova senha.";
            PasswordMessageIsError = true;
            return;
        }

        if (!IsPasswordComplex(NewPassword))
        {
            PasswordMessage = "A nova senha deve ter pelo menos 8 caracteres, incluindo letra maiúscula, minúscula e número.";
            PasswordMessageIsError = true;
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            PasswordMessage = "A confirmação não corresponde à nova senha.";
            PasswordMessageIsError = true;
            return;
        }

        try
        {
            await _userService.ChangePasswordAsync(_userId, CurrentPassword, NewPassword);
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            IsChangePasswordExpanded = false;

            // A troca revoga a sessão no backend, então o token atual deixa de ser confiável —
            // desloga e manda para o Login em vez de deixar a pessoa numa tela que vai começar
            // a falhar nas próximas chamadas sem explicação.
            await _authService.LogoutAsync();
            await Shell.Current.DisplayAlertAsync(
                "Senha alterada",
                "Sua senha foi alterada com sucesso. Entre novamente com a nova senha.",
                "OK");
            await NavigationService.GoToRootAsync(Routes.Login);
        }
        catch (Exception)
        {
            PasswordMessage = "Não foi possível alterar a senha. Verifique a senha atual.";
            PasswordMessageIsError = true;
        }
    }

    [RelayCommand]
    private void ToggleChangeEmail()
    {
        IsChangeEmailExpanded = !IsChangeEmailExpanded;
        if (!IsChangeEmailExpanded)
        {
            NewEmail = string.Empty;
            EmailChangePassword = string.Empty;
            EmailChangeMessage = null;
        }
    }

    [RelayCommand]
    private async Task RequestEmailChangeAsync()
    {
        EmailChangeMessage = null;

        if (string.IsNullOrWhiteSpace(NewEmail) || !NewEmail.Contains('@'))
        {
            EmailChangeMessage = "Informe um e-mail válido.";
            EmailChangeMessageIsError = true;
            return;
        }

        if (string.IsNullOrEmpty(EmailChangePassword))
        {
            EmailChangeMessage = "Informe sua senha atual para confirmar.";
            EmailChangeMessageIsError = true;
            return;
        }

        try
        {
            await _userService.RequestEmailChangeAsync(_userId, NewEmail.Trim(), EmailChangePassword);
            EmailChangePassword = string.Empty;
            EmailChangeMessageIsError = false;
            EmailChangeMessage = $"Enviamos um link de confirmação para {NewEmail.Trim()}. O e-mail só muda depois que você confirmar por lá.";
        }
        catch (HttpRequestException ex)
        {
            EmailChangeMessageIsError = true;
            EmailChangeMessage = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Senha atual incorreta.",
                HttpStatusCode.BadRequest => "Esse e-mail já está em uso ou é inválido.",
                HttpStatusCode.TooManyRequests => "Muitas tentativas. Aguarde um momento antes de tentar novamente.",
                _ => "Não foi possível solicitar a troca de e-mail."
            };
        }
        catch (Exception)
        {
            EmailChangeMessageIsError = true;
            EmailChangeMessage = "Não foi possível solicitar a troca de e-mail.";
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Excluir conta",
            "Esta ação exclui permanentemente sua conta e não pode ser desfeita. Seu cadastro de membro em organizações não é afetado.",
            "Continuar",
            "Cancelar");

        if (!confirmed) return;

        var typed = await Shell.Current.DisplayPromptAsync(
            "Confirmar exclusão",
            "Digite EXCLUIR para confirmar a exclusão definitiva da sua conta:");

        if (!string.Equals(typed?.Trim(), "EXCLUIR", StringComparison.Ordinal)) return;

        try
        {
            await _userService.DeleteAccountAsync(_userId);
            await _authService.LogoutAsync();
            await NavigationService.GoToRootAsync(Routes.Login);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir sua conta agora.", "OK");
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await NavigationService.GoToRootAsync(Routes.Login);
    }
}
