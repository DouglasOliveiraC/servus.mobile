namespace Servus.Mobile.Services.Members;

public record MemberDto(Guid Id, string Name, string? Email, string? Phone, Guid? UserId, DateTime CreatedAt);

public record MemberMembershipDto(
    Guid SubrosterMemberId,
    Guid SubrosterId,
    string SubrosterName,
    Guid RosterId,
    string RosterName,
    int Rank,
    int QueueSize,
    Guid? AccessId,
    bool IsFullAdmin);
