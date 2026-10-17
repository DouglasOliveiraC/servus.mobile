using System.Collections.ObjectModel;
using System.Globalization;
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

    // Sections: the queue, schedule generation and access used to be stacked on one page, and a
    // 30-person queue plus a 30-row duty count list made everything below them unreachable.
    public const string QueueSection = "queue";
    public const string ScheduleSection = "schedule";
    public const string AccessSection = "access";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQueueSection), nameof(IsScheduleSection), nameof(IsAccessSection))]
    public partial string SelectedSection { get; set; } = QueueSection;

    public bool IsQueueSection => SelectedSection == QueueSection;

    public bool IsScheduleSection => SelectedSection == ScheduleSection;

    public bool IsAccessSection => SelectedSection == AccessSection;

    [RelayCommand]
    private void SelectSection(string section) => SelectedSection = section;

    // Members
    private const int QueuePreviewCount = 8;

    [ObservableProperty]
    public partial bool IsAddMemberFormVisible { get; set; }

    [ObservableProperty]
    public partial string MemberSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMemberPosition { get; set; } = "1";

    public ObservableCollection<RosterMemberRow> Members { get; } = [];

    // What the list renders: the first QueuePreviewCount by rank until expanded, or every match
    // while searching (a search hit hidden behind "ver fila completa" would read as "not found").
    public ObservableCollection<RosterMemberRow> VisibleMembers { get; } = [];

    [ObservableProperty]
    public partial string QueueSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsQueueExpanded { get; set; }

    public bool IsSearchingQueue => !string.IsNullOrWhiteSpace(QueueSearchText);

    public bool ShowQueueSearch => Members.Count > QueuePreviewCount;

    public bool ShowQueueToggle => !IsSearchingQueue && Members.Count > QueuePreviewCount;

    public string QueueToggleText => IsQueueExpanded ? "Mostrar menos" : $"Ver fila completa ({Members.Count})";

    public bool ShowNoQueueMatch => IsSearchingQueue && VisibleMembers.Count == 0;

    public string QueueTabLabel => Members.Count == 0 ? "Fila" : $"Fila · {Members.Count}";

    partial void OnQueueSearchTextChanged(string value) => RefreshVisibleMembers();

    partial void OnIsQueueExpandedChanged(bool value) => RefreshVisibleMembers();

    [RelayCommand]
    private void ToggleQueueExpanded() => IsQueueExpanded = !IsQueueExpanded;

    private void RefreshVisibleMembers()
    {
        IEnumerable<RosterMemberRow> rows = Members;
        if (IsSearchingQueue)
        {
            var term = QueueSearchText.Trim();
            var compare = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
            rows = rows.Where(r => compare.IndexOf(r.MemberName, term, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
        }
        else if (!IsQueueExpanded)
        {
            rows = rows.Take(QueuePreviewCount);
        }

        VisibleMembers.Clear();
        foreach (var row in rows)
        {
            VisibleMembers.Add(row);
        }

        OnPropertyChanged(nameof(IsSearchingQueue));
        OnPropertyChanged(nameof(ShowQueueSearch));
        OnPropertyChanged(nameof(ShowQueueToggle));
        OnPropertyChanged(nameof(QueueToggleText));
        OnPropertyChanged(nameof(ShowNoQueueMatch));
        OnPropertyChanged(nameof(QueueTabLabel));
    }

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

            // Duty counts ride on the queue rows instead of a second list of the same people.
            var dutyCounts = (await _rosterService.GetDutyCountsAsync(RosterId))
                .ToDictionary(c => c.MemberId, c => c.AssignmentCount);

            Members.Clear();
            foreach (var member in members.OrderBy(m => m.Rank))
            {
                Members.Add(new RosterMemberRow(member) { DutyCount = dutyCounts.GetValueOrDefault(member.MemberId) });
            }

            RefreshVisibleMembers();

            // RosterAccessDto only carries MemberId (the API does not resolve names), so the
            // roster members loaded above supply the display name.
            var memberNamesById = members.ToDictionary(m => m.MemberId, m => m.MemberName);

            // The access list is admin-only (403 otherwise); requesting it unconditionally would
            // turn the whole screen into an error for anyone who can only view the roster.
            var accesses = CanManage ? await _rosterService.GetRosterAccessesAsync(RosterId) : [];
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
        GenerateScheduleResultDto result;
        try
        {
            var from = DateOnly.FromDateTime(GenerateFrom);
            var to = DateOnly.FromDateTime(GenerateTo);
            result = await _rosterService.GenerateScheduleAsync(RosterId, from, to);
        }
        catch (Exception)
        {
            GenerateResultMessage = null;
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível gerar os plantões.", "OK");
            return;
        }

        GenerateResultMessage = $"{result.AssignmentsCreated} plantão(ões) criado(s) em {result.DaysProcessed} dia(s).";
        if (result.UnderstaffedDates.Count > 0)
        {
            GenerateResultMessage += $" {result.UnderstaffedDates.Count} dia(s) ficaram sem gente suficiente.";
        }

        // Outside the try above: a failed reload must not be reported as a failed generation,
        // since the assignments are already committed by then.
        await LoadAsync();
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

    /// <summary>
    /// O perfil consolidado exige permissão de gestão no servidor, então só faz sentido
    /// abri-lo de uma fila que este usuário administra — abrir sem CanManage renderia 403.
    /// </summary>
    [RelayCommand]
    private Task OpenMemberProfileAsync(RosterMemberRow member)
    {
        if (!CanManage) return Task.CompletedTask;

        return NavigationService.GoToAsync(Routes.RosterMemberProfile, new Dictionary<string, object>
        {
            ["rosterId"] = RosterId,
            ["memberId"] = member.MemberId
        });
    }

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
    public Guid MemberId => Dto.MemberId;
    public int Rank => Dto.Rank;
    public string MemberName => Dto.MemberName;

    public int DutyCount { get; init; }

    public string DutyCountText => DutyCount == 1 ? "1 plantão" : $"{DutyCount} plantões";

    // A prioridade é por rotação: exibimos as duas juntas porque a linha é uma só.
    public string? PendingReturnPriorityReason =>
        Dto.PendingReturnPriorityWorkdayReason ?? Dto.PendingReturnPriorityWeekendReason;
}
