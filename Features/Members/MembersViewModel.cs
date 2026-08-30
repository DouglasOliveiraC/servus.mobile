using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Members;

public partial class MembersViewModel : ObservableObject
{
    private readonly IMemberService _memberService;
    private readonly IRosterService _rosterService;

    public MembersViewModel(IMemberService memberService, IRosterService rosterService)
    {
        _memberService = memberService;
        _rosterService = rosterService;
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isFormVisible;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newEmail = string.Empty;

    [ObservableProperty]
    private string newPhone = string.Empty;

    [ObservableProperty]
    private RosterDto? selectedRoster;

    [ObservableProperty]
    private SubrosterDto? selectedSubroster;

    [ObservableProperty]
    private string newMemberPosition = "1";

    public ObservableCollection<MemberDto> AllMembers { get; } = [];

    public ObservableCollection<RosterDto> Rosters { get; } = [];

    public ObservableCollection<SubrosterDto> Subrosters { get; } = [];

    public ObservableCollection<SubrosterMemberGroup> GroupedMembers { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var members = await _memberService.SearchMembersAsync(null);
            AllMembers.Clear();
            foreach (var member in members.OrderBy(m => m.Name))
            {
                AllMembers.Add(member);
            }

            var rosters = await _rosterService.GetMyRostersAsync();
            Rosters.Clear();
            foreach (var roster in rosters)
            {
                Rosters.Add(roster);
            }

            await LoadGroupedMembersAsync(rosters);
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

    private async Task LoadGroupedMembersAsync(IReadOnlyList<RosterDto> rosters)
    {
        var membersById = AllMembers.ToDictionary(m => m.Id);
        var seenMemberIds = new HashSet<Guid>();
        var groups = new List<SubrosterMemberGroup>();

        foreach (var roster in rosters)
        {
            IReadOnlyList<SubrosterDto> subrosters;
            try
            {
                subrosters = await _rosterService.GetSubrostersAsync(roster.Id);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var subroster in subrosters)
            {
                IReadOnlyList<SubrosterMemberDto> subrosterMembers;
                try
                {
                    subrosterMembers = await _rosterService.GetSubrosterMembersAsync(subroster.Id);
                }
                catch (Exception)
                {
                    continue;
                }

                var entries = subrosterMembers
                    .Where(sm => membersById.ContainsKey(sm.MemberId))
                    .OrderBy(sm => sm.Position)
                    .Select((sm, index) => new MemberGroupEntry(membersById[sm.MemberId], index + 1))
                    .ToList();

                foreach (var entry in entries)
                {
                    seenMemberIds.Add(entry.Member.Id);
                }

                if (entries.Count > 0)
                {
                    groups.Add(new SubrosterMemberGroup(roster.Name, subroster.Name, entries));
                }
            }
        }

        var unassigned = AllMembers
            .Where(m => !seenMemberIds.Contains(m.Id))
            .Select(m => new MemberGroupEntry(m, 0))
            .ToList();

        if (unassigned.Count > 0)
        {
            groups.Add(new SubrosterMemberGroup("Sem organização", "Ainda não colocado em nenhuma escala", unassigned));
        }

        GroupedMembers.Clear();
        foreach (var group in groups)
        {
            GroupedMembers.Add(group);
        }
    }

    [RelayCommand]
    private void ToggleForm()
    {
        if (IsFormVisible)
        {
            ResetForm();
        }
        else
        {
            IsFormVisible = true;
        }
    }

    partial void OnSelectedRosterChanged(RosterDto? value)
    {
        SelectedSubroster = null;
        Subrosters.Clear();
        if (value is null) return;

        _ = LoadSubrostersAsync(value.Id);
    }

    private async Task LoadSubrostersAsync(Guid rosterId)
    {
        try
        {
            var subrosters = await _rosterService.GetSubrostersAsync(rosterId);
            Subrosters.Clear();
            foreach (var subroster in subrosters)
            {
                Subrosters.Add(subroster);
            }
        }
        catch (Exception)
        {
            // busca falhou silenciosamente; a lista de escalas permanece vazia
        }
    }

    [RelayCommand]
    private async Task SaveMemberAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;

        var name = NewName.Trim();
        var email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail.Trim();
        var phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim();

        if (SelectedSubroster is null)
        {
            await Shell.Current.DisplayAlertAsync("Escala obrigatória", "Todo membro precisa ser cadastrado já em uma organização e escala.", "OK");
            return;
        }

        var duplicate = AllMembers.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            var proceed = await Shell.Current.DisplayAlertAsync(
                "Membro já existe",
                $"Já existe um membro chamado \"{name}\". Deseja cadastrar mesmo assim?",
                "Cadastrar mesmo assim", "Cancelar");
            if (!proceed) return;
        }

        try
        {
            var position = int.TryParse(NewMemberPosition, out var p) ? p : 1;
            var created = await _memberService.CreateMemberAsync(name, email, phone);
            await _rosterService.AddSubrosterMemberAsync(SelectedSubroster.Id, created.Id, position);

            ResetForm();
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível salvar o membro.", "OK");
        }
    }

    [RelayCommand]
    private static async Task OpenMemberAsync(MemberDto member)
    {
        await Shell.Current.GoToAsync(nameof(MemberDetailPage), new Dictionary<string, object>
        {
            ["memberId"] = member.Id
        });
    }

    private void ResetForm()
    {
        IsFormVisible = false;
        NewName = string.Empty;
        NewEmail = string.Empty;
        NewPhone = string.Empty;
        SelectedRoster = null;
        SelectedSubroster = null;
        NewMemberPosition = "1";
        Subrosters.Clear();
    }
}
