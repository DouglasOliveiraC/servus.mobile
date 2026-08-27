namespace Servus.Mobile.Features.Home;

public record RosterSummary(Guid Id, string Name, int SubrosterCount, int UpcomingAssignmentCount);

public record UpcomingDuty(string RosterName, string SubrosterName, DateOnly Date, IReadOnlyList<string> MemberNames, bool IsMine);
