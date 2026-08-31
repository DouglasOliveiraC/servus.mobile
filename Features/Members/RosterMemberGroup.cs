using System.Collections.ObjectModel;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;

namespace Servus.Mobile.Features.Members;

// Rank é a posição relativa 1-based do membro dentro da fila deste roster (não o
// Position bruto do backend, que é um contador interno que só cresce a cada geração de
// escala e nunca reflete a ordem atual sozinho).
public record MemberGroupEntry(MemberDto Member, int Rank);

public class RosterMemberGroup : ObservableCollection<MemberGroupEntry>
{
    public string OrganizationName { get; }

    public string RosterName { get; }

    public RosterMemberGroup(string organizationName, string rosterName, IEnumerable<MemberGroupEntry> entries)
        : base(entries)
    {
        OrganizationName = organizationName;
        RosterName = rosterName;
    }
}
