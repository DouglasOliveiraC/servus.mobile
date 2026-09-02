using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(RosterId), "rosterId")]
[QueryProperty(nameof(RosterName), "rosterName")]
public partial class RosterDetailViewModel : BaseViewModel
{
    private readonly IRosterService _rosterService;
    private readonly IMemberService _memberService;

    public RosterDetailViewModel(
        IRosterService rosterService,
        IMemberService memberService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _rosterService = rosterService;
        _memberService = memberService;
    }

    [ObservableProperty]
    public partial Guid RosterId { get; set; }

    [ObservableProperty]
    public partial string RosterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    /// <summary>Drives visibility of every admin-only action in this page (add/remove member,
    /// generate schedule, grant/revoke access, settings) — backed by the API's per-request
    /// authorization check (org admin OR RosterAccess.IsFullAdmin), never trusted as the
    /// actual access control.</summary>
    [ObservableProperty]
    public partial bool CanManage { get; set; }

    // Members
    [ObservableProperty]
    public partial bool IsAddMemberFormVisible { get; set; }

    [ObservableProperty]
    public partial string MemberSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMemberPosition { get; set; } = "1";

    public ObservableCollection<RosterMemberRow> Members { get; } = [];

    public ObservableCollection<MemberDto> MemberSearchResults { get; } = [];

    // Import em lote
    [ObservableProperty]
    public partial bool IsImportFormVisible { get; set; }

    [ObservableProperty]
    public partial string ImportText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ImportSummaryText { get; set; }

    [ObservableProperty]
    public partial string? ImportResultMessage { get; set; }

    // Ponto de partida (fila que já rodava antes de entrar no sistema)
    [ObservableProperty]
    public partial bool IsStartingPointFormVisible { get; set; }

    [ObservableProperty]
    public partial RosterMemberRow? StartingPointWorkdayMember { get; set; }

    [ObservableProperty]
    public partial RosterMemberRow? StartingPointWeekendMember { get; set; }

    [ObservableProperty]
    public partial string? StartingPointResultMessage { get; set; }

    // Schedule generation
    [ObservableProperty]
    public partial DateTime GenerateFrom { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime GenerateTo { get; set; } = DateTime.Today.AddDays(30);

    [ObservableProperty]
    public partial string? GenerateResultMessage { get; set; }

    // Duty counts
    public ObservableCollection<MemberDutyCountDto> DutyCounts { get; } = [];

    // Access
    [ObservableProperty]
    public partial bool IsGrantAccessFormVisible { get; set; }

    [ObservableProperty]
    public partial string AccessSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NewAccessIsFullAdmin { get; set; }

    public ObservableCollection<RosterAccessRow> Accesses { get; } = [];

    public ObservableCollection<MemberDto> AccessSearchResults { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var roster = await _rosterService.GetRosterByIdAsync(RosterId);
            CanManage = roster.CanManage;

            var members = await _rosterService.GetRosterMembersAsync(RosterId);
            Members.Clear();
            foreach (var member in members.OrderBy(m => m.Rank))
            {
                Members.Add(new RosterMemberRow(member));
            }

            var dutyCounts = await _rosterService.GetDutyCountsAsync(RosterId);
            DutyCounts.Clear();
            foreach (var count in dutyCounts.OrderByDescending(c => c.AssignmentCount))
            {
                DutyCounts.Add(count);
            }

            // RosterAccessDto só traz MemberId (o endpoint da API não resolve nome);
            // usa a lista de membros da escala, já carregada acima, para exibir o nome.
            var memberNamesById = members.ToDictionary(m => m.MemberId, m => m.MemberName);

            var accesses = await _rosterService.GetRosterAccessesAsync(RosterId);
            Accesses.Clear();
            foreach (var access in accesses)
            {
                var memberName = memberNamesById.TryGetValue(access.MemberId, out var name) ? name : "Membro fora da escala";
                Accesses.Add(new RosterAccessRow(access.Id, access.MemberId, memberName, access.IsFullAdmin));
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
    private void ToggleAddMemberForm()
    {
        IsAddMemberFormVisible = !IsAddMemberFormVisible;
        MemberSearchResults.Clear();
        MemberSearchText = string.Empty;
    }

    [RelayCommand]
    private async Task SearchMembersAsync()
    {
        try
        {
            var results = await _memberService.SearchMembersAsync(MemberSearchText);
            MemberSearchResults.Clear();
            foreach (var member in results)
            {
                MemberSearchResults.Add(member);
            }
        }
        catch (Exception)
        {
            // busca falhou silenciosamente; a lista de resultados permanece vazia
        }
    }

    [RelayCommand]
    private async Task AddMemberAsync(MemberDto member)
    {
        var rank = int.TryParse(NewMemberPosition, out var p) ? p : Members.Count + 1;

        try
        {
            await _rosterService.AddRosterMemberAsync(RosterId, member.Id, rank);
            IsAddMemberFormVisible = false;
            MemberSearchResults.Clear();
            MemberSearchText = string.Empty;
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível adicionar o membro.", "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(RosterMemberRow member)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Remover membro", $"Remover \"{member.MemberName}\" da escala?", "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _rosterService.RemoveRosterMemberAsync(member.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível remover o membro.", "OK");
        }
    }

    [RelayCommand]
    private void ToggleImportForm()
    {
        IsImportFormVisible = !IsImportFormVisible;
        ImportText = string.Empty;
        ImportSummaryText = null;
        ImportResultMessage = null;
    }

    partial void OnImportTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ImportSummaryText = null;
            return;
        }

        var parsed = MemberListParser.Parse(value);
        var parts = new List<string> { $"{parsed.Rows.Count} pessoa(s)" };
        if (parsed.DuplicateNames.Count > 0)
        {
            parts.Add($"{parsed.DuplicateNames.Count} nome(s) repetido(s) ignorado(s)");
        }
        if (parsed.Errors.Count > 0)
        {
            parts.Add($"{parsed.Errors.Count} linha(s) com problema");
        }
        ImportSummaryText = string.Join(" · ", parts);
    }

    [RelayCommand]
    private async Task ImportMembersAsync()
    {
        var parsed = MemberListParser.Parse(ImportText);
        if (parsed.Rows.Count == 0) return;

        try
        {
            var entries = parsed.Rows
                .Select(r => new ImportRosterMemberEntryDto(r.Name, r.Email, r.Phone))
                .ToList();
            var result = await _rosterService.ImportRosterMembersAsync(RosterId, entries);
            ImportResultMessage = $"{result.Imported.Count} membro(s) importado(s).";
            IsImportFormVisible = false;
            ImportText = string.Empty;
            ImportSummaryText = null;
            await LoadAsync();
        }
        catch (Exception)
        {
            ImportResultMessage = null;
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível importar a lista.", "OK");
        }
    }

    [RelayCommand]
    private void ToggleStartingPointForm()
    {
        IsStartingPointFormVisible = !IsStartingPointFormVisible;
        StartingPointWorkdayMember = null;
        StartingPointWeekendMember = null;
        StartingPointResultMessage = null;
    }

    [RelayCommand]
    private async Task SaveStartingPointAsync()
    {
        if (StartingPointWorkdayMember is null && StartingPointWeekendMember is null) return;

        try
        {
            await _rosterService.SetNextUpRosterMemberAsync(
                RosterId,
                StartingPointWorkdayMember?.Id,
                StartingPointWeekendMember?.Id);
            StartingPointResultMessage = "Ponto de partida salvo.";
            IsStartingPointFormVisible = false;
            StartingPointWorkdayMember = null;
            StartingPointWeekendMember = null;
            await LoadAsync();
        }
        catch (Exception)
        {
            StartingPointResultMessage = null;
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível salvar o ponto de partida.", "OK");
        }
    }

    [RelayCommand]
    private async Task GenerateScheduleAsync()
    {
        try
        {
            var from = DateOnly.FromDateTime(GenerateFrom);
            var to = DateOnly.FromDateTime(GenerateTo);
            var schedule = await _rosterService.GenerateScheduleAsync(RosterId, from, to);
            var assignmentCount = schedule.Days.Sum(d => d.Assignments.Count);
            GenerateResultMessage = $"Plantões gerados: {schedule.Days.Count} dia(s), {assignmentCount} plantão(ões).";
            await LoadAsync();
        }
        catch (Exception)
        {
            GenerateResultMessage = null;
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível gerar os plantões.", "OK");
        }
    }

    [RelayCommand]
    private void ToggleGrantAccessForm()
    {
        IsGrantAccessFormVisible = !IsGrantAccessFormVisible;
        AccessSearchResults.Clear();
        AccessSearchText = string.Empty;
    }

    [RelayCommand]
    private async Task SearchAccessMembersAsync()
    {
        try
        {
            var results = await _memberService.SearchMembersAsync(AccessSearchText);
            AccessSearchResults.Clear();
            foreach (var member in results)
            {
                AccessSearchResults.Add(member);
            }
        }
        catch (Exception)
        {
            // busca falhou silenciosamente; a lista de resultados permanece vazia
        }
    }

    [RelayCommand]
    private async Task GrantAccessAsync(MemberDto member)
    {
        try
        {
            await _rosterService.GrantRosterAccessAsync(RosterId, member.Id, NewAccessIsFullAdmin);
            IsGrantAccessFormVisible = false;
            AccessSearchResults.Clear();
            AccessSearchText = string.Empty;
            NewAccessIsFullAdmin = false;
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível conceder o acesso.", "OK");
        }
    }

    [RelayCommand]
    private Task OpenSettingsAsync() =>
        NavigationService.GoToAsync(Routes.RosterSettings, new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });

    [RelayCommand]
    private Task ViewScheduleAsync() =>
        NavigationService.GoToAsync(Routes.RosterSchedule, new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });

    [RelayCommand]
    private Task ViewSwapHistoryAsync() =>
        NavigationService.GoToAsync(Routes.SwapHistory, new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });

    [RelayCommand]
    private async Task RevokeAccessAsync(RosterAccessRow access)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Revogar acesso", "Revogar este acesso de gestão?", "Revogar", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _rosterService.RevokeRosterAccessAsync(access.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível revogar o acesso.", "OK");
        }
    }

    partial void OnMemberSearchTextChanged(string value) => SearchMembersCommand.Execute(null);

    partial void OnAccessSearchTextChanged(string value) => SearchAccessMembersCommand.Execute(null);
}

public record RosterAccessRow(Guid Id, Guid MemberId, string MemberName, bool IsFullAdmin);

// Rank é a posição 1-based na ordem institucional da fila — uma só, percorrida pelas duas
// rotações — e é o que a tela mostra. Muda apenas se alguém for adicionado, removido ou
// reordenado, nunca porque a escala rodou.
public record RosterMemberRow(RosterMemberDto Dto)
{
    public Guid Id => Dto.Id;
    public int Rank => Dto.Rank;
    public string MemberName => Dto.MemberName;

    // A prioridade é por rotação: exibimos as duas juntas porque a linha é uma só.
    public string? PendingReturnPriorityReason =>
        Dto.PendingReturnPriorityWorkdayReason ?? Dto.PendingReturnPriorityWeekendReason;
}
