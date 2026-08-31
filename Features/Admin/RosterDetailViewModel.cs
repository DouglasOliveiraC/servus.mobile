using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(RosterId), "rosterId")]
[QueryProperty(nameof(RosterName), "rosterName")]
public partial class RosterDetailViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;
    private readonly IMemberService _memberService;

    public RosterDetailViewModel(IRosterService rosterService, IMemberService memberService)
    {
        _rosterService = rosterService;
        _memberService = memberService;
    }

    [ObservableProperty]
    private Guid rosterId;

    [ObservableProperty]
    private string rosterName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    // Members
    [ObservableProperty]
    private bool isAddMemberFormVisible;

    [ObservableProperty]
    private string memberSearchText = string.Empty;

    [ObservableProperty]
    private string newMemberPosition = "1";

    public ObservableCollection<RosterMemberRow> Members { get; } = [];

    public ObservableCollection<MemberDto> MemberSearchResults { get; } = [];

    // Schedule generation
    [ObservableProperty]
    private DateTime generateFrom = DateTime.Today;

    [ObservableProperty]
    private DateTime generateTo = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string? generateResultMessage;

    // Duty counts
    public ObservableCollection<MemberDutyCountDto> DutyCounts { get; } = [];

    // Access
    [ObservableProperty]
    private bool isGrantAccessFormVisible;

    [ObservableProperty]
    private string accessSearchText = string.Empty;

    [ObservableProperty]
    private bool newAccessIsFullAdmin;

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
            var members = await _rosterService.GetRosterMembersAsync(RosterId);
            var orderedMembers = members.OrderBy(m => m.WorkdayPosition).ToList();
            Members.Clear();
            for (var i = 0; i < orderedMembers.Count; i++)
            {
                // WorkdayPosition é um contador interno do algoritmo de fila (só cresce, nunca
                // reseta); o rank exibido é a ordem relativa 1-based dentro da fila atual.
                Members.Add(new RosterMemberRow(orderedMembers[i], i + 1));
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
            await _rosterService.AddRosterMemberAsync(RosterId, member.Id, rank, QueueType.Workday);
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
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(RosterSettingsPage), new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });
    }

    [RelayCommand]
    private async Task ViewScheduleAsync()
    {
        await Shell.Current.GoToAsync("RosterSchedulePage", new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });
    }

    [RelayCommand]
    private async Task ViewSwapHistoryAsync()
    {
        await Shell.Current.GoToAsync("SwapHistoryPage", new Dictionary<string, object>
        {
            ["rosterId"] = RosterId
        });
    }

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

// WorkdayPosition (do DTO original) é um contador interno do algoritmo de fila — só cresce a
// cada geração de escala e nunca reseta. Rank é a posição relativa 1-based dentro da fila
// atual, que é o que a tela deve mostrar; ela muda se alguém for adicionado/removido/reordenado.
public record RosterMemberRow(RosterMemberDto Dto, int Rank)
{
    public Guid Id => Dto.Id;
    public string MemberName => Dto.MemberName;
    public string? PendingReturnPriorityReason => Dto.PendingReturnPriorityWorkdayReason;
}
