namespace Servus.Mobile.Services.Organizations;

public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> GetMyOrganizationsAsync(CancellationToken cancellationToken = default);

    Task<OrganizationDto> GetOrganizationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateOrganizationAsync(string name, CancellationToken cancellationToken = default);

    Task UpdateOrganizationNameAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task DeleteOrganizationAsync(Guid id, CancellationToken cancellationToken = default);
}
