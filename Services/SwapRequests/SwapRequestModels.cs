namespace Servus.Mobile.Services.SwapRequests;

// Must keep Servus.Domain.Enums.SwapRequestStatus's order: the API sends the numeric value.
public enum SwapRequestStatusDto
{
    Pending = 0,
    AwaitingAdminApproval = 1,
    Accepted = 2,
    Declined = 3,
    Cancelled = 4,
    Expired = 5
}

public record SwapRequestDto(
    Guid Id,
    Guid RosterId,
    Guid? RequestingDutyAssignmentId,
    Guid? TargetDutyAssignmentId,
    Guid RequestingMemberId,
    Guid TargetMemberId,
    SwapRequestStatusDto Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ApprovedAt,
    DateTime ExpiresAt)
{
    // Filled by the client, not the API: approving takes roster/organization admin rights,
    // which the request itself does not carry.
    public bool CanApprove { get; init; }
}
