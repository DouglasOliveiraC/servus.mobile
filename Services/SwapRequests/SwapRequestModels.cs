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
    DateTime ExpiresAt,
    string? RosterName = null,
    string? RequestingMemberName = null,
    string? TargetMemberName = null,
    SwapDutySummaryDto? RequestingDuty = null,
    SwapDutySummaryDto? TargetDuty = null)
{
    public string RequestingDutyText => $"Plantão de {RequestingMemberName ?? "quem pediu"}: {Describe(RequestingDuty)}";

    public string TargetDutyText => $"Plantão de {TargetMemberName ?? "quem recebeu"}: {Describe(TargetDuty)}";

    public string Summary => RosterName is null
        ? $"{RequestingMemberName ?? "Membro"} ⇄ {TargetMemberName ?? "membro"}"
        : $"{RequestingMemberName ?? "Membro"} ⇄ {TargetMemberName ?? "membro"} · {RosterName}";

    private static string Describe(SwapDutySummaryDto? duty) =>
        duty is null ? "plantão não encontrado" : $"{duty.Date:dd/MM/yyyy} · {duty.ShiftStartTime:HH:mm}–{duty.ShiftEndTime:HH:mm}";

    // Filled by the client, not the API: true for the items that came from the
    // awaiting-approval list, which only holds swaps the user is allowed to approve.
    public bool CanApprove { get; init; }
}

public record SwapDutySummaryDto(DateOnly Date, TimeOnly ShiftStartTime, TimeOnly ShiftEndTime);
