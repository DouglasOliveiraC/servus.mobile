namespace Servus.Mobile.Services.Members;

public interface IMemberService
{
    Task<MemberDto?> GetMyMemberAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberDto>> SearchMembersAsync(string? search, CancellationToken cancellationToken = default);

    Task<MemberDto> CreateMemberAsync(string name, string? email = null, string? phone = null, CancellationToken cancellationToken = default);

    Task UpdateMemberAsync(Guid id, string name, string? email, string? phone, CancellationToken cancellationToken = default);

    Task DeleteMemberAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every Roster this Member is queued in, already joined with its owning Organization —
    /// avoids the client fanning out over organizations and rosters just to find where one
    /// Member sits.
    /// </summary>
    Task<IReadOnlyList<MemberMembershipDto>> GetMembershipsAsync(Guid id, CancellationToken cancellationToken = default);
}
