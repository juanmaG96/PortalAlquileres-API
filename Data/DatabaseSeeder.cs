using Marketplace.API.Services;

namespace Marketplace.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Intentar obtener credenciales desde IOptions / Variables de Entorno
        string defaultUsername = configuration["AdminSeedSettings:Username"] ?? "admin";
        string defaultPassword = configuration["AdminSeedSettings:Password"] ?? "Admin123!";

        try
        {
            bool seeded = await authService.SeedAdminUserAsync(defaultUsername, defaultPassword);
            if (seeded)
            {
                logger.LogInformation("Data Seed: Usuario Administrador Base '{Username}' creado exitosamente.", defaultUsername);
            }
            else
            {
                logger.LogInformation("Data Seed: El usuario administrador base '{Username}' ya existe en la base de datos.", defaultUsername);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la ejecución del Data Seed de Administrador.");
        }
    }
}
