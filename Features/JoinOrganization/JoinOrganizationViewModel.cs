using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.MemberClaimCodes;

namespace Servus.Mobile.Features.JoinOrganization;

public partial class JoinOrganizationViewModel : ObservableObject
{
    private readonly IMemberClaimCodeService _claimCodeService;

    public JoinOrganizationViewModel(IMemberClaimCodeService claimCodeService)
    {
        _claimCodeService = claimCodeService;
    }

    [ObservableProperty]
    private string code = string.Empty;

    [ObservableProperty]
    private string? serverError;

    [ObservableProperty]
    private string? successMessage;

    [ObservableProperty]
    private bool isBusy;

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
            var result = await _claimCodeService.RedeemAsync(Code.Trim());
            SuccessMessage = $"Você entrou como {result.MemberName}!";
            await Task.Delay(1200);
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception)
        {
            ServerError = "Código inválido ou expirado. Verifique e tente novamente.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanJoin() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => JoinCommand.NotifyCanExecuteChanged();
}
