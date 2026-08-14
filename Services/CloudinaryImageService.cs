using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Marketplace.API.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Services;

/// <summary>
/// Implementación de IImageService utilizando el SDK oficial de Cloudinary.
/// </summary>
public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException("Las credenciales de Cloudinary no están adecuadamente configuradas en appsettings.json.");
        }

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret
        );

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("El archivo proporcionado está vacío o no es válido.");
        }

        using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "marketplace_properties",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Error al subir la imagen a Cloudinary: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() 
               ?? uploadResult.Url?.ToString() 
               ?? throw new InvalidOperationException("Cloudinary no devolvió una URL válida para la imagen.");
    }
}
