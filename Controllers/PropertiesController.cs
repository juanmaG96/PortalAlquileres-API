using Microsoft.AspNetCore.Authorization;
using Marketplace.API.Dtos;
using Marketplace.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(IPropertyService propertyService, ILogger<PropertiesController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PropertySummaryDto>>> GetProperties(
        [FromQuery] PropertySearchFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetPropertiesAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize]
    public async Task<ActionResult<PagedResultDto<PropertySummaryDto>>> GetAdminProperties(
        [FromQuery] PropertySearchFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetAdminPropertiesAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDetailDto>> GetPropertyById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var property = await _propertyService.GetPropertyByIdAsync(id, cancellationToken);
        if (property == null) return NotFound(new { Message = "Propiedad no encontrada." });
        return Ok(property);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PropertyDetailDto>> CreateProperty(
        [FromBody] PropertyDetailDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var created = await _propertyService.CreatePropertyAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetPropertyById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PropertyDetailDto>> UpdateProperty(
        Guid id,
        [FromBody] PropertyDetailDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _propertyService.UpdatePropertyAsync(id, dto, cancellationToken);
        
        if (updated == null) 
            return NotFound(new { Message = "Propiedad no encontrada o eliminada." });

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> SoftDeleteProperty(
        Guid id,
        CancellationToken cancellationToken)
    {
        bool deleted = await _propertyService.SoftDeletePropertyAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { Message = "Propiedad no encontrada." });
        return NoContent();
    }

    [HttpPatch("{id}/toggle-status")]
    [Authorize]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var success = await _propertyService.ToggleStatusAsync(id);
        
        if (!success) 
            return NotFound(new { message = "Propiedad no encontrada." });

        return Ok(new { message = "Estado actualizado correctamente." });
    }

    [HttpPatch("{id}/restore")]
    [Authorize]
    public async Task<IActionResult> RestoreProperty(Guid id)
    {
        var success = await _propertyService.RestorePropertyAsync(id);

        if (!success) 
            return NotFound(new { message = "Propiedad no encontrada o no eliminada." });

        return Ok(new { message = "Propiedad restaurada correctamente." });
    }
}
