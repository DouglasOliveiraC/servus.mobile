using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.MemberClaimCodes;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Features.JoinOrganization;

public partial class JoinOrganizationViewModel : BaseViewModel
{
    private readonly IMemberClaimCodeService _claimCodeService;

    public JoinOrganizationViewModel(IMemberClaimCodeService claimCodeService, INavigationService navigationService, ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _claimCodeService = claimCodeService;
    }

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ServerError { get; set; }

    [ObservableProperty]
    public partial string? SuccessMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync()
    {
        ServerError = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(Code))
        {
            ServerError = "Informe o código de convite.";
            return;
        }

        IsBusy = true;
        try
        {
            await _claimCodeService.RedeemAsync(Code.Trim());
            SuccessMessage = "Pronto! Sua conta foi vinculada à organização.";
            await Task.Delay(1200);
            await NavigationService.GoToRootAsync(Routes.Dashboard);
        }
        catch (ApiException ex)
        {
            ServerError = DescribeRedeemFailure(ex);
        }
        catch (Exception)
        {
            ServerError = "Não foi possível usar o código agora. Tente novamente.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Every 409 here is a different user-facing situation; only the problem title (the domain
    // exception message) tells them apart.
    private static string DescribeRedeemFailure(ApiException ex) => ex.StatusCode switch
    {
        HttpStatusCode.NotFound => "Código não encontrado. Verifique se digitou corretamente.",
        HttpStatusCode.TooManyRequests => "Muitas tentativas. Aguarde um momento antes de tentar novamente.",
        HttpStatusCode.Conflict when ex.TitleContains("expired") => "Este código expirou. Peça um novo ao administrador.",
        HttpStatusCode.Conflict when ex.TitleContains("already been redeemed") => "Este código já foi usado.",
        HttpStatusCode.Conflict when ex.TitleContains("different email") => "Este código foi gerado para outro e-mail. Entre com a conta do e-mail cadastrado no membro.",
        HttpStatusCode.Conflict when ex.TitleContains("member is already linked") => "Este membro já está vinculado a outra conta.",
        HttpStatusCode.Conflict when ex.TitleContains("account is already linked") => "Sua conta já está vinculada a outro membro.",
        _ => "Código inválido ou expirado. Verifique e tente novamente."
    };

    private bool CanJoin() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => JoinCommand.NotifyCanExecuteChanged();
}
