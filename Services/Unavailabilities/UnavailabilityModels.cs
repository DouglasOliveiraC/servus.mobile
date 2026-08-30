namespace Servus.Mobile.Services.Unavailabilities;

public record MemberUnavailabilityDto(Guid Id, Guid MemberId, DateOnly StartDate, DateOnly EndDate, DateTime CreatedAt);
