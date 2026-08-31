using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Rosters;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;
using IMemberService = Servus.Mobile.Services.Members.IMemberService;

namespace Servus.Mobile.Features.Members;

public partial class MembersViewModel : ObservableObject
{
    private readonly IMemberService _memberService;
    private readonly IOrganizationService _organizationService;
    private readonly IRosterService _rosterService;

    public MembersViewModel(IMemberService memberService, IOrganizationService organizationService, IRosterService rosterService)
    {
        _memberService = memberService;
        _organizationService = organizationService;
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsFormVisible { get; set; }

    [ObservableProperty]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPhone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial OrganizationDto? SelectedOrganization { get; set; }

    [ObservableProperty]
    public partial RosterDto? SelectedRoster { get; set; }

    [ObservableProperty]
    public partial string NewMemberPosition { get; set; } = "1";

    public ObservableCollection<MemberDto> AllMembers { get; } = [];

    public ObservableCollection<OrganizationDto> Organizations { get; } = [];

    public ObservableCollection<RosterDto> Rosters { get; } = [];

    public ObservableCollection<RosterMemberGroup> GroupedMembers { get; } = [];

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

            var organizations = await _organizationService.GetMyOrganizationsAsync();
            Organizations.Clear();
            foreach (var organization in organizations)
            {
                Organizations.Add(organization);
            }

            await LoadGroupedMembersAsync(organizations);
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

    private async Task LoadGroupedMembersAsync(IReadOnlyList<OrganizationDto> organizations)
    {
        var membersById = AllMembers.ToDictionary(m => m.Id);
        var seenMemberIds = new HashSet<Guid>();
        var groups = new List<RosterMemberGroup>();

        foreach (var organization in organizations)
        {
            IReadOnlyList<RosterDto> rosters;
            try
            {
                rosters = await _rosterService.GetRostersAsync(organization.Id);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var roster in rosters)
            {
                IReadOnlyList<RosterMemberDto> rosterMembers;
                try
                {
                    rosterMembers = await _rosterService.GetRosterMembersAsync(roster.Id);
                }
                catch (Exception)
                {
                    continue;
                }

                var entries = rosterMembers
                    .Where(rm => membersById.ContainsKey(rm.MemberId))
                    .OrderBy(rm => rm.WorkdayPosition)
                    .Select((rm, index) => new MemberGroupEntry(membersById[rm.MemberId], index + 1))
                    .ToList();

                foreach (var entry in entries)
                {
                    seenMemberIds.Add(entry.Member.Id);
                }

                if (entries.Count > 0)
                {
                    groups.Add(new RosterMemberGroup(organization.Name, roster.Name, entries));
                }
            }
        }

        var unassigned = AllMembers
            .Where(m => !seenMemberIds.Contains(m.Id))
            .Select(m => new MemberGroupEntry(m, 0))
            .ToList();

        if (unassigned.Count > 0)
        {
            groups.Add(new RosterMemberGroup("Sem organização", "Ainda não colocado em nenhuma escala", unassigned));
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

    partial void OnSelectedOrganizationChanged(OrganizationDto? value)
    {
        SelectedRoster = null;
        Rosters.Clear();
        if (value is null) return;

        _ = LoadRostersAsync(value.Id);
    }

    private async Task LoadRostersAsync(Guid organizationId)
    {
        try
        {
            var rosters = await _rosterService.GetRostersAsync(organizationId);
            Rosters.Clear();
            foreach (var roster in rosters)
            {
                Rosters.Add(roster);
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

        if (SelectedRoster is null)
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
            await _rosterService.AddRosterMemberAsync(SelectedRoster.Id, created.Id, position, QueueType.Workday);

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
        SelectedOrganization = null;
        SelectedRoster = null;
        NewMemberPosition = "1";
        Rosters.Clear();
    }
}
