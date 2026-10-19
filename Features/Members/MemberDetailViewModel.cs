using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.MemberClaimCodes;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Unavailabilities;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using MemberMembershipDto = Servus.Mobile.Services.Members.MemberMembershipDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Members;

[QueryProperty(nameof(MemberId), "memberId")]
public partial class MemberDetailViewModel : BaseViewModel
{
    private readonly IMemberService _memberService;
    private readonly IOrganizationService _organizationService;
    private readonly IRosterService _rosterService;
    private readonly IMemberClaimCodeService _claimCodeService;
    private readonly IMemberUnavailabilityService _unavailabilityService;
    private readonly IAuthService _authService;

    public MemberDetailViewModel(
        IMemberService memberService,
        IOrganizationService organizationService,
        IRosterService rosterService,
        IMemberClaimCodeService claimCodeService,
        IMemberUnavailabilityService unavailabilityService,
        IAuthService authService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _memberService = memberService;
        _organizationService = organizationService;
        _rosterService = rosterService;
        _claimCodeService = claimCodeService;
        _unavailabilityService = unavailabilityService;
        _authService = authService;
    }

    // False for demo accounts: claim codes are refused to them by the API, so the section
    // would only lead to an error.
    [ObservableProperty]
    public partial bool CanShareClaimCode { get; set; }

    [ObservableProperty]
    public partial Guid MemberId { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLinkedToAccount { get; set; }

    [ObservableProperty]
    public partial string MemberSince { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MembershipsSummary { get; set; } = string.Empty;

    public ObservableCollection<MembershipRow> Memberships { get; } = [];

    public ObservableCollection<UnavailabilityRow> Unavailabilities { get; } = [];

    // "Adicionar a uma escala" form
    [ObservableProperty]
    public partial bool IsAddMembershipFormVisible { get; set; }

    [ObservableProperty]
    public partial OrganizationDto? SelectedOrganization { get; set; }

    [ObservableProperty]
    public partial RosterDto? SelectedRoster { get; set; }

    [ObservableProperty]
    public partial string NewMembershipPosition { get; set; } = string.Empty;

    public ObservableCollection<OrganizationDto> Organizations { get; } = [];

    public ObservableCollection<RosterDto> AvailableRosters { get; } = [];

    // Unavailability form
    [ObservableProperty]
    public partial bool IsAddUnavailabilityFormVisible { get; set; }

    [ObservableProperty]
    public partial DateTime UnavailabilityStart { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime UnavailabilityEnd { get; set; } = DateTime.Today.AddDays(7);

    // Claim code
    [ObservableProperty]
    public partial bool IsGeneratingClaimCode { get; set; }

    [ObservableProperty]
    public partial string? GeneratedClaimCode { get; set; }

    [ObservableProperty]
    public partial string ClaimCodeStatusMessage { get; set; } = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || MemberId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            CanShareClaimCode = (await _authService.GetCurrentUserAsync())?.IsDemo != true;

            var members = await _memberService.SearchMembersAsync(null);
            var member = members.FirstOrDefault(m => m.Id == MemberId);
            if (member is null)
            {
                HasError = true;
                return;
            }

            Name = member.Name;
            Email = member.Email ?? string.Empty;
            Phone = member.Phone ?? string.Empty;
            IsLinkedToAccount = member.UserId is not null;
            MemberSince = $"No sistema desde {member.CreatedAt.ToLocalTime():dd/MM/yyyy}";

            await LoadMembershipsAsync();
            await LoadUnavailabilitiesAsync();
            await LoadOrganizationsAsync();
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

    private async Task LoadMembershipsAsync()
    {
        var memberships = await _memberService.GetMembershipsAsync(MemberId);

        Memberships.Clear();
        foreach (var membership in memberships)
        {
            Memberships.Add(new MembershipRow(membership));
        }

        var organizationCount = memberships.Select(m => m.OrganizationId).Distinct().Count();
        MembershipsSummary = memberships.Count == 0
            ? "Este membro ainda não está em nenhuma escala."
            : $"{memberships.Count} escala(s) em {organizationCount} organização(ões).";
    }

    private async Task LoadUnavailabilitiesAsync()
    {
        try
        {
            var unavailabilities = await _unavailabilityService.GetByMemberIdAsync(MemberId);
            Unavailabilities.Clear();
            foreach (var unavailability in unavailabilities.OrderByDescending(u => u.StartDate))
            {
                Unavailabilities.Add(new UnavailabilityRow(unavailability));
            }
        }
        catch (Exception)
        {
            // Ausências dependem de permissão sobre o membro; sem ela a seção fica vazia
            // em vez de derrubar o resto da tela.
            Unavailabilities.Clear();
        }
    }

    private async Task LoadOrganizationsAsync()
    {
        var organizations = await _organizationService.GetMyOrganizationsAsync();
        Organizations.Clear();
        foreach (var organization in organizations.OrderBy(o => o.Name))
        {
            Organizations.Add(organization);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlertAsync("Nome obrigatório", "O membro precisa de um nome.", "OK");
            return;
        }

        var name = Name.Trim();
        var email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        var phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();

        try
        {
            await _memberService.UpdateMemberAsync(MemberId, name, email, phone);
            await Shell.Current.DisplayAlertAsync("Membro", "Alterações salvas.", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível salvar as alterações.", "OK");
        }
    }

    [RelayCommand]
    private void ToggleAddMembershipForm()
    {
        IsAddMembershipFormVisible = !IsAddMembershipFormVisible;
        if (!IsAddMembershipFormVisible)
        {
            ResetMembershipForm();
        }
    }

    partial void OnSelectedOrganizationChanged(OrganizationDto? value)
    {
        SelectedRoster = null;
        AvailableRosters.Clear();
        if (value is null) return;

        _ = LoadAvailableRostersAsync(value.Id);
    }

    private async Task LoadAvailableRostersAsync(Guid organizationId)
    {
        try
        {
            var rosters = await _rosterService.GetRostersAsync(organizationId);
            var alreadyIn = Memberships.Select(m => m.RosterId).ToHashSet();

            AvailableRosters.Clear();
            foreach (var roster in rosters.Where(r => !alreadyIn.Contains(r.Id)).OrderBy(r => r.Name))
            {
                AvailableRosters.Add(roster);
            }
        }
        catch (Exception)
        {
            AvailableRosters.Clear();
        }
    }

    [RelayCommand]
    private async Task AddMembershipAsync()
    {
        if (SelectedRoster is null)
        {
            await Shell.Current.DisplayAlertAsync("Escala obrigatória", "Escolha a organização e a escala em que o membro vai entrar.", "OK");
            return;
        }

        // Sem posição informada, entra no fim da fila daquela escala.
        int rank;
        if (string.IsNullOrWhiteSpace(NewMembershipPosition))
        {
            var queue = await _rosterService.GetRosterMembersAsync(SelectedRoster.Id);
            rank = queue.Count + 1;
        }
        else if (!int.TryParse(NewMembershipPosition, out rank) || rank < 1)
        {
            await Shell.Current.DisplayAlertAsync("Posição inválida", "A posição na fila precisa ser um número maior que zero.", "OK");
            return;
        }

        try
        {
            await _rosterService.AddRosterMemberAsync(SelectedRoster.Id, MemberId, rank);
            ResetMembershipForm();
            IsAddMembershipFormVisible = false;
            await LoadMembershipsAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível adicionar o membro a esta escala.", "OK");
        }
    }

    [RelayCommand]
    private async Task ChangePositionAsync(MembershipRow row)
    {
        var input = await Shell.Current.DisplayPromptAsync(
            "Posição na fila",
            $"Nova posição de {Name} em \"{row.RosterName}\" (1 a {Math.Max(row.QueueSize, 1)}):",
            initialValue: row.Rank.ToString(),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(input)) return;
        if (!int.TryParse(input, out var newRank) || newRank < 1)
        {
            await Shell.Current.DisplayAlertAsync("Posição inválida", "Informe um número maior que zero.", "OK");
            return;
        }

        try
        {
            await _rosterService.ReorderRosterMemberAsync(row.RosterMemberId, newRank);
            await LoadMembershipsAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível mudar a posição.", "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveMembershipAsync(MembershipRow row)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Sair da escala",
            $"Remover {Name} da escala \"{row.RosterName}\"? O membro continua cadastrado e nas outras escalas.",
            "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _rosterService.RemoveRosterMemberAsync(row.RosterMemberId);
            await LoadMembershipsAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível remover o membro desta escala.", "OK");
        }
    }

    [RelayCommand]
    private async Task ToggleAccessAsync(MembershipRow row)
    {
        try
        {
            if (row.AccessId is { } accessId)
            {
                var confirmed = await Shell.Current.DisplayAlertAsync(
                    "Revogar gestão",
                    $"Tirar a permissão de gerenciar \"{row.RosterName}\" de {Name}?",
                    "Revogar", "Cancelar");
                if (!confirmed) return;

                await _rosterService.RevokeRosterAccessAsync(accessId);
            }
            else
            {
                var isFullAdmin = await Shell.Current.DisplayAlertAsync(
                    "Conceder gestão",
                    $"Que nível de acesso {Name} terá em \"{row.RosterName}\"?",
                    "Administrador total", "Somente visualizar");

                await _rosterService.GrantRosterAccessAsync(row.RosterId, MemberId, isFullAdmin);
            }

            await LoadMembershipsAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível alterar a permissão de gestão.", "OK");
        }
    }

    [RelayCommand]
    private Task OpenRosterAsync(MembershipRow row) =>
        NavigationService.GoToAsync(Routes.RosterDetail, new Dictionary<string, object>
        {
            ["rosterId"] = row.RosterId,
            ["rosterName"] = row.RosterName
        });

    [RelayCommand]
    private void ToggleAddUnavailabilityForm()
    {
        IsAddUnavailabilityFormVisible = !IsAddUnavailabilityFormVisible;
    }

    [RelayCommand]
    private async Task AddUnavailabilityAsync()
    {
        if (UnavailabilityEnd < UnavailabilityStart)
        {
            await Shell.Current.DisplayAlertAsync("Datas inválidas", "A data final não pode ser anterior à inicial.", "OK");
            return;
        }

        try
        {
            var result = await _unavailabilityService.CreateAsync(
                MemberId,
                DateOnly.FromDateTime(UnavailabilityStart),
                DateOnly.FromDateTime(UnavailabilityEnd));

            IsAddUnavailabilityFormVisible = false;
            await LoadUnavailabilitiesAsync();

            // Cadastro feito pelo admin já vale de imediato, então pode invalidar plantões
            // que já foram publicados — sem este aviso ele só descobriria olhando a escala.
            if (result.ConflictingPublishedDuties > 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Escala precisa ser regerada",
                    $"A ausência atinge {result.ConflictingPublishedDuties} plantão(ões) já publicado(s) deste membro. Regere a escala desse período ou troque o membro nesses dias.",
                    "OK");
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível registrar a ausência.", "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveUnavailabilityAsync(UnavailabilityRow row)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Remover ausência", $"Remover o período {row.Period}?", "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _unavailabilityService.DeleteAsync(row.Id);
            await LoadUnavailabilitiesAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível remover a ausência.", "OK");
        }
    }

    [RelayCommand]
    private async Task GenerateClaimCodeAsync()
    {
        IsGeneratingClaimCode = true;
        GeneratedClaimCode = null;
        ClaimCodeStatusMessage = string.Empty;
        try
        {
            // O código é emitido no escopo de uma organização; sem nenhuma escala o membro
            // ainda não pertence a nenhuma, então não há escopo para emitir.
            var organizationId = Memberships.FirstOrDefault()?.OrganizationId;
            if (organizationId is null)
            {
                ClaimCodeStatusMessage = "Não foi possível gerar o código: este membro ainda não está em nenhuma escala.";
                return;
            }

            var result = await _claimCodeService.GenerateMemberClaimCodeAsync(organizationId.Value, MemberId, expiresAt: null);
            GeneratedClaimCode = result.Code;
        }
        catch (Exception)
        {
            ClaimCodeStatusMessage = "Não foi possível gerar o código agora.";
        }
        finally
        {
            IsGeneratingClaimCode = false;
        }
    }

    [RelayCommand]
    private async Task CopyClaimCodeAsync()
    {
        if (string.IsNullOrEmpty(GeneratedClaimCode)) return;

        await Clipboard.Default.SetTextAsync(GeneratedClaimCode);
        ClaimCodeStatusMessage = "Código copiado para a área de transferência.";
    }

    [RelayCommand]
    private async Task DeleteMemberAsync()
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir membro",
            $"Excluir \"{Name}\" remove a pessoa de todas as escalas e apaga seu histórico de plantões. Não pode ser desfeito.\n\nDigite \"{Name}\" para confirmar:");

        if (!string.Equals(typed?.Trim(), Name, StringComparison.Ordinal)) return;

        try
        {
            await _memberService.DeleteMemberAsync(MemberId);
            await NavigationService.GoBackAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir o membro.", "OK");
        }
    }

    private void ResetMembershipForm()
    {
        SelectedOrganization = null;
        SelectedRoster = null;
        NewMembershipPosition = string.Empty;
        AvailableRosters.Clear();
    }
}

public partial class MembershipRow : ObservableObject
{
    public MembershipRow(MemberMembershipDto dto)
    {
        RosterMemberId = dto.RosterMemberId;
        RosterId = dto.RosterId;
        RosterName = dto.RosterName;
        OrganizationId = dto.OrganizationId;
        OrganizationName = dto.OrganizationName;
        Rank = dto.Rank;
        QueueSize = dto.QueueSize;
        AccessId = dto.AccessId;
        IsFullAdmin = dto.IsFullAdmin;
    }

    public Guid RosterMemberId { get; }
    public Guid RosterId { get; }
    public string RosterName { get; }
    public Guid OrganizationId { get; }
    public string OrganizationName { get; }
    public int Rank { get; }
    public int QueueSize { get; }
    public Guid? AccessId { get; }
    public bool IsFullAdmin { get; }

    public string PositionLabel => QueueSize > 0 ? $"{Rank}º de {QueueSize}" : $"{Rank}º";

    public bool HasAccess => AccessId is not null;

    public string AccessLabel => AccessId is null
        ? "Sem acesso de gestão"
        : IsFullAdmin ? "Administrador da escala" : "Acesso de visualização";

    public string AccessActionLabel => AccessId is null ? "Conceder gestão" : "Revogar gestão";
}

public class UnavailabilityRow
{
    public UnavailabilityRow(MemberUnavailabilityDto dto)
    {
        Id = dto.Id;
        StartDate = dto.StartDate;
        EndDate = dto.EndDate;
        ApprovalStatus = dto.Status;
    }

    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }
    public MemberUnavailabilityStatus ApprovalStatus { get; }

    public string Period => $"{StartDate:dd/MM/yyyy} a {EndDate:dd/MM/yyyy}";

    /// <summary>
    /// Fase no tempo (encerrada/em andamento/agendada), mas só depois de aprovada: um período
    /// pendente ou recusado ainda não vale para a escala, e mostrá-lo como "Agendada" diria
    /// ao admin que aquele membro já está fora daqueles dias quando ele não está.
    /// </summary>
    public string Status
    {
        get
        {
            if (ApprovalStatus == MemberUnavailabilityStatus.Pending) return "Aguardando aprovação";
            if (ApprovalStatus == MemberUnavailabilityStatus.Rejected) return "Recusada";

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (EndDate < today) return "Encerrada";
            return StartDate <= today ? "Em andamento" : "Agendada";
        }
    }
}
