using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.MemberClaimCodes;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Unavailabilities;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using MemberMembershipDto = Servus.Mobile.Services.Members.MemberMembershipDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Members;

[QueryProperty(nameof(MemberId), "memberId")]
public partial class MemberDetailViewModel : ObservableObject
{
    private readonly IMemberService _memberService;
    private readonly IRosterService _rosterService;
    private readonly IMemberClaimCodeService _claimCodeService;
    private readonly IMemberUnavailabilityService _unavailabilityService;

    public MemberDetailViewModel(
        IMemberService memberService,
        IRosterService rosterService,
        IMemberClaimCodeService claimCodeService,
        IMemberUnavailabilityService unavailabilityService)
    {
        _memberService = memberService;
        _rosterService = rosterService;
        _claimCodeService = claimCodeService;
        _unavailabilityService = unavailabilityService;
    }

    [ObservableProperty]
    private Guid memberId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private bool isLinkedToAccount;

    [ObservableProperty]
    private string memberSince = string.Empty;

    [ObservableProperty]
    private string membershipsSummary = string.Empty;

    public ObservableCollection<MembershipRow> Memberships { get; } = [];

    public ObservableCollection<UnavailabilityRow> Unavailabilities { get; } = [];

    // "Adicionar a uma escala" form
    [ObservableProperty]
    private bool isAddMembershipFormVisible;

    [ObservableProperty]
    private RosterDto? selectedRoster;

    [ObservableProperty]
    private SubrosterDto? selectedSubroster;

    [ObservableProperty]
    private string newMembershipPosition = string.Empty;

    public ObservableCollection<RosterDto> Rosters { get; } = [];

    public ObservableCollection<SubrosterDto> AvailableSubrosters { get; } = [];

    // Unavailability form
    [ObservableProperty]
    private bool isAddUnavailabilityFormVisible;

    [ObservableProperty]
    private DateTime unavailabilityStart = DateTime.Today;

    [ObservableProperty]
    private DateTime unavailabilityEnd = DateTime.Today.AddDays(7);

    // Claim code
    [ObservableProperty]
    private bool isGeneratingClaimCode;

    [ObservableProperty]
    private string? generatedClaimCode;

    [ObservableProperty]
    private string claimCodeStatusMessage = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || MemberId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
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
            await LoadRostersAsync();
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

        var rosterCount = memberships.Select(m => m.RosterId).Distinct().Count();
        MembershipsSummary = memberships.Count == 0
            ? "Este membro ainda não está em nenhuma escala."
            : $"{memberships.Count} escala(s) em {rosterCount} organização(ões).";
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

    private async Task LoadRostersAsync()
    {
        var rosters = await _rosterService.GetMyRostersAsync();
        Rosters.Clear();
        foreach (var roster in rosters.OrderBy(r => r.Name))
        {
            Rosters.Add(roster);
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

    partial void OnSelectedRosterChanged(RosterDto? value)
    {
        SelectedSubroster = null;
        AvailableSubrosters.Clear();
        if (value is null) return;

        _ = LoadAvailableSubrostersAsync(value.Id);
    }

    private async Task LoadAvailableSubrostersAsync(Guid rosterId)
    {
        try
        {
            var subrosters = await _rosterService.GetSubrostersAsync(rosterId);
            var alreadyIn = Memberships.Select(m => m.SubrosterId).ToHashSet();

            AvailableSubrosters.Clear();
            foreach (var subroster in subrosters.Where(s => !alreadyIn.Contains(s.Id)).OrderBy(s => s.Name))
            {
                AvailableSubrosters.Add(subroster);
            }
        }
        catch (Exception)
        {
            AvailableSubrosters.Clear();
        }
    }

    [RelayCommand]
    private async Task AddMembershipAsync()
    {
        if (SelectedSubroster is null)
        {
            await Shell.Current.DisplayAlertAsync("Escala obrigatória", "Escolha a organização e a escala em que o membro vai entrar.", "OK");
            return;
        }

        // Sem posição informada, entra no fim da fila daquela escala.
        int rank;
        if (string.IsNullOrWhiteSpace(NewMembershipPosition))
        {
            var queue = await _rosterService.GetSubrosterMembersAsync(SelectedSubroster.Id);
            rank = queue.Count + 1;
        }
        else if (!int.TryParse(NewMembershipPosition, out rank) || rank < 1)
        {
            await Shell.Current.DisplayAlertAsync("Posição inválida", "A posição na fila precisa ser um número maior que zero.", "OK");
            return;
        }

        try
        {
            await _rosterService.AddSubrosterMemberAsync(SelectedSubroster.Id, MemberId, rank);
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
            $"Nova posição de {Name} em \"{row.SubrosterName}\" (1 a {Math.Max(row.QueueSize, 1)}):",
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
            await _rosterService.ReorderSubrosterMemberAsync(row.SubrosterMemberId, newRank);
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
            $"Remover {Name} da escala \"{row.SubrosterName}\"? O membro continua cadastrado e nas outras escalas.",
            "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _rosterService.RemoveSubrosterMemberAsync(row.SubrosterMemberId);
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
                    $"Tirar a permissão de gerenciar \"{row.SubrosterName}\" de {Name}?",
                    "Revogar", "Cancelar");
                if (!confirmed) return;

                await _rosterService.RevokeSubrosterAccessAsync(accessId);
            }
            else
            {
                var isFullAdmin = await Shell.Current.DisplayAlertAsync(
                    "Conceder gestão",
                    $"Que nível de acesso {Name} terá em \"{row.SubrosterName}\"?",
                    "Administrador total", "Somente visualizar");

                await _rosterService.GrantSubrosterAccessAsync(row.SubrosterId, MemberId, isFullAdmin);
            }

            await LoadMembershipsAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível alterar a permissão de gestão.", "OK");
        }
    }

    [RelayCommand]
    private async Task OpenSubrosterAsync(MembershipRow row)
    {
        await Shell.Current.GoToAsync("SubrosterDetailPage", new Dictionary<string, object>
        {
            ["subrosterId"] = row.SubrosterId,
            ["subrosterName"] = row.SubrosterName
        });
    }

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
            await _unavailabilityService.CreateAsync(
                MemberId,
                DateOnly.FromDateTime(UnavailabilityStart),
                DateOnly.FromDateTime(UnavailabilityEnd));

            IsAddUnavailabilityFormVisible = false;
            await LoadUnavailabilitiesAsync();
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
            var rosterId = Memberships.FirstOrDefault()?.RosterId;
            if (rosterId is null)
            {
                ClaimCodeStatusMessage = "Não foi possível gerar o código: este membro ainda não está em nenhuma escala.";
                return;
            }

            var result = await _claimCodeService.GenerateMemberClaimCodeAsync(rosterId.Value, MemberId, expiresAt: null);
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
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir o membro.", "OK");
        }
    }

    private void ResetMembershipForm()
    {
        SelectedRoster = null;
        SelectedSubroster = null;
        NewMembershipPosition = string.Empty;
        AvailableSubrosters.Clear();
    }
}

public partial class MembershipRow : ObservableObject
{
    public MembershipRow(MemberMembershipDto dto)
    {
        SubrosterMemberId = dto.SubrosterMemberId;
        SubrosterId = dto.SubrosterId;
        SubrosterName = dto.SubrosterName;
        RosterId = dto.RosterId;
        RosterName = dto.RosterName;
        Rank = dto.Rank;
        QueueSize = dto.QueueSize;
        AccessId = dto.AccessId;
        IsFullAdmin = dto.IsFullAdmin;
    }

    public Guid SubrosterMemberId { get; }
    public Guid SubrosterId { get; }
    public string SubrosterName { get; }
    public Guid RosterId { get; }
    public string RosterName { get; }
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
    }

    public Guid Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public string Period => $"{StartDate:dd/MM/yyyy} a {EndDate:dd/MM/yyyy}";

    public string Status
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (EndDate < today) return "Encerrada";
            return StartDate <= today ? "Em andamento" : "Agendada";
        }
    }
}
