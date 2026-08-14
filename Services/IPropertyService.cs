using Marketplace.API.Dtos;

namespace Marketplace.API.Services;

public interface IPropertyService
{
    Task<PagedResultDto<PropertySummaryDto>> GetPropertiesAsync(PropertySearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<PagedResultDto<PropertySummaryDto>> GetAdminPropertiesAsync(PropertySearchFilterDto filter, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto> CreatePropertyAsync(PropertyDetailDto dto, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> UpdatePropertyAsync(Guid id, PropertyDetailDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeletePropertyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestorePropertyAsync(Guid id, CancellationToken cancellationToken = default);
}
