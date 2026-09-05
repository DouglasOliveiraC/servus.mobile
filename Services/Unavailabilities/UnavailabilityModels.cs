namespace Servus.Mobile.Services.Unavailabilities;

/// <summary>
/// Espelha Servus.Domain.Enums.MemberUnavailabilityStatus (a API serializa enum como
/// inteiro, então a ordem dos membros aqui tem de bater com a de lá).
/// </summary>
public enum MemberUnavailabilityStatus
{
    Pending,
    Approved,
    Rejected
}

public record MemberUnavailabilityDto(Guid Id, Guid MemberId, DateOnly StartDate, DateOnly EndDate, MemberUnavailabilityStatus Status, DateTime CreatedAt)
{
    public bool IsPending => Status == MemberUnavailabilityStatus.Pending;
    public bool IsRejected => Status == MemberUnavailabilityStatus.Rejected;

    public string StatusLabel => Status switch
    {
        MemberUnavailabilityStatus.Pending => "Aguardando aprovação",
        MemberUnavailabilityStatus.Rejected => "Recusada",
        _ => "Aprovada"
    };
}

/// <summary>
/// ConflictingPublishedDuties só é diferente de zero quando o período já nasce aprovado
/// (cadastro feito por quem administra o membro) e atinge plantões já publicados.
/// </summary>
public record CreateUnavailabilityResult(Guid Id, MemberUnavailabilityStatus Status, int ConflictingPublishedDuties);
