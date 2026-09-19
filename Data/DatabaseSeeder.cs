using Marketplace.API.Services;

namespace Marketplace.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Paysandu Admin
        string paysanduUser = configuration["AdminSeedSettings:PaysanduAdmin:Username"] ?? "admin@paysandu.com";
        string paysanduPass = configuration["AdminSeedSettings:PaysanduAdmin:Password"] ?? "Admin123!";
        string paysanduCity = "Paysandú";

        // CDelu Admin
        string cdeluUser = configuration["AdminSeedSettings:CDeluAdmin:Username"] ?? "admin@cdelu.com";
        string cdeluPass = configuration["AdminSeedSettings:CDeluAdmin:Password"] ?? "Admin123!";
        string cdeluCity = "Concepción del Uruguay";

        try
        {
            // Seed Paysandu Admin
            bool seededPaysandu = await authService.SeedAdminUserAsync(paysanduUser, paysanduPass, paysanduCity);
            if (seededPaysandu)
            {
                logger.LogInformation("Data Seed: Usuario Administrador '{Username}' creado exitosamente para {City}.", paysanduUser, paysanduCity);
            }

            // Seed CDelu Admin
            bool seededCDelu = await authService.SeedAdminUserAsync(cdeluUser, cdeluPass, cdeluCity);
            if (seededCDelu)
            {
                logger.LogInformation("Data Seed: Usuario Administrador '{Username}' creado exitosamente para {City}.", cdeluUser, cdeluCity);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la ejecución del Data Seed de Administradores.");
        }
    }
}
