namespace Servus.Mobile.Features.Home;

public record DaySubrosterGroup(Guid SubrosterId, string RosterName, string SubrosterName, IReadOnlyList<string> MemberNames);
