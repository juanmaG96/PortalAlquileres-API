using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Marketplace.API.Data;
using Marketplace.API.Data.Dtos;
using Marketplace.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Marketplace.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context, 
        IConfiguration config, 
        ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Intento de inicio de sesión fallido para el usuario: {Username}", request.Username);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);
        var expirationMinutes = double.TryParse(_config["JwtSettings:ExpirationInMinutes"], out var exp) ? exp : 60;

        _logger.LogInformation("Usuario {Username} ha iniciado sesión exitosamente.", user.Username);

        return new AuthResponseDto(
            Token: token,
            Username: user.Username,
            Expiration: DateTime.UtcNow.AddMinutes(expirationMinutes)
        );
    }

    /// <summary>
    /// Genera un token seguro para la recuperación de contraseña.
    /// PREPARADO PARA INTEGRACIÓN CON SERVICIO DE EMAIL (Resend / SendGrid / SMTP).
    /// </summary>
    public async Task<string?> GeneratePasswordResetTokenAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail && u.IsActive, cancellationToken);

        if (user == null)
        {
            // Retornamos null o token simulado para no revelar la existencia de usuarios
            _logger.LogInformation("Solicitud de token de recupero para usuario inexistente o inactivo: {User}", usernameOrEmail);
            return null;
        }

        // Generar un token criptográfico seguro de 32 bytes
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var resetToken = Convert.ToHexString(tokenBytes);

        _logger.LogInformation("Token de recuperación generado exitosamente para el usuario: {Username}", user.Username);

        return await Task.FromResult(resetToken);
    }

    public async Task<bool> SeedAdminUserAsync(string username, string password, string city, CancellationToken cancellationToken = default)
    {
        var exists = await _context.AdminUsers.AnyAsync(u => u.Username == username, cancellationToken);
        if (exists)
        {
            return false;
        }

        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            City = city
        };

        _context.AdminUsers.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Usuario administrador base '{Username}' para la ciudad '{City}' insertado en el Data Seed.", username, city);

        return true;
    }

    private string GenerateJwtToken(AdminUser user)
    {
        var secretKey = _config["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("La clave secreta de JWT no está configurada.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };

        if (!string.IsNullOrWhiteSpace(user.City))
        {
            claims.Add(new Claim("City", user.City));
        }

        var expirationMinutes = double.TryParse(_config["JwtSettings:ExpirationInMinutes"], out var exp) ? exp : 60;

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"] ?? "MarketplaceAPI",
            audience: _config["JwtSettings:Audience"] ?? "MarketplaceFrontend",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
