using Marketplace.API.Dtos;

namespace Marketplace.API.Services;

public interface INominatimGeocodingService
{
    Task<GeocodeResultDto> GeocodeAddressAsync(string address, string? city = null, CancellationToken cancellationToken = default);
}
