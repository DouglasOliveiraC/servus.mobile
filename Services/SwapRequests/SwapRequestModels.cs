namespace Servus.Mobile.Services.SwapRequests;

public enum SwapRequestStatusDto
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Approved = 3,
    Cancelled = 4,
    Expired = 5
}

public record SwapRequestDto(
    Guid Id,
    Guid SubrosterId,
    Guid? RequestingDutyAssignmentId,
    Guid? TargetDutyAssignmentId,
    Guid RequestingMemberId,
    Guid TargetMemberId,
    SwapRequestStatusDto Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ApprovedAt,
    DateTime ExpiresAt);
