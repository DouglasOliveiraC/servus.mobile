namespace Servus.Mobile.Services.Members;

public record MemberDto(Guid Id, string Name, string? Email, string? Phone, Guid? UserId, DateTime CreatedAt);

public record MemberMembershipDto(
    Guid RosterMemberId,
    Guid RosterId,
    string RosterName,
    Guid OrganizationId,
    string OrganizationName,
    int Rank,
    int QueueSize,
    Guid? AccessId,
    bool IsFullAdmin);
