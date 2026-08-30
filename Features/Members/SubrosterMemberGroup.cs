using System.Collections.ObjectModel;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;

namespace Servus.Mobile.Features.Members;

// Rank é a posição relativa 1-based do membro dentro da fila desta subroster (não o
// Position bruto do backend, que é um contador interno que só cresce a cada geração de
// escala e nunca reflete a ordem atual sozinho).
public record MemberGroupEntry(MemberDto Member, int Rank);

public class SubrosterMemberGroup : ObservableCollection<MemberGroupEntry>
{
    public string RosterName { get; }

    public string SubrosterName { get; }

    public SubrosterMemberGroup(string rosterName, string subrosterName, IEnumerable<MemberGroupEntry> entries)
        : base(entries)
    {
        RosterName = rosterName;
        SubrosterName = subrosterName;
    }
}
