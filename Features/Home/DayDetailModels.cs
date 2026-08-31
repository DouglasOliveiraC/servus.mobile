namespace Servus.Mobile.Features.Home;

public record DayRosterGroup(Guid RosterId, string OrganizationName, string RosterName, IReadOnlyList<string> MemberNames);
