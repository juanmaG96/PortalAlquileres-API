using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Marketplace.API.Services;

/// <summary>
/// Interfaz para el servicio de gestión y subida de imágenes a servicios en la nube.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Sube una imagen recibida como IFormFile a Cloudinary y retorna su URL segura HTTPS.
    /// </summary>
    /// <param name="file">Archivo de imagen enviado en el request multipart/form-data.</param>
    /// <returns>URL segura (SecureUrl) generada por Cloudinary.</returns>
    Task<string> UploadImageAsync(IFormFile file);
}
