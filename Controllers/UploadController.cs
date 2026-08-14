using Marketplace.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IImageService _imageService;

    public UploadController(IImageService imageService)
    {
        _imageService = imageService;
    }

    /// <summary>
    /// Sube una imagen a Cloudinary y retorna su URL segura.
    /// Endpoint protegido que requiere cabecera 'Authorization: Bearer <token>'.
    /// </summary>
    /// <param name="file">Imagen enviada como multipart/form-data con la clave 'file'.</param>
    /// <returns>JSON con la propiedad 'url'.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se ha seleccionado ningún archivo de imagen para subir." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Formato no permitido. Solo se aceptan imágenes JPG, PNG y WEBP." });
        }

        // Limite opcional de tamaño (ej. 10MB)
        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { message = "El archivo excede el tamaño máximo permitido de 10 MB." });
        }

        try
        {
            var imageUrl = await _imageService.UploadImageAsync(file);
            return Ok(new { url = imageUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
