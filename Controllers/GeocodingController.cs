using Marketplace.API.Dtos;
using Marketplace.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeocodingController : ControllerBase
{
    private readonly INominatimGeocodingService _geocodingService;

    public GeocodingController(INominatimGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<GeocodeResultDto>> GeocodeAddress(
        [FromBody] GeocodeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new { Message = "La dirección es requerida." });
        }

        var result = await _geocodingService.GeocodeAddressAsync(request.Address, request.City, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
