using Marketplace.API.Data.Dtos;

namespace Marketplace.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<string?> GeneratePasswordResetTokenAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
    Task<bool> SeedAdminUserAsync(string username, string password, CancellationToken cancellationToken = default);
}
