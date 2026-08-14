namespace Marketplace.API.Configurations;

/// <summary>
/// Configuración para la integración con Cloudinary (Patrón Options).
/// </summary>
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
