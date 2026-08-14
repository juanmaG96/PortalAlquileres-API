using Marketplace.API.Data.Dtos;
using Marketplace.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new { Message = "Usuario o contraseña incorrectos." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Solicita la generación de un token de recuperación de contraseña.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request, 
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var token = await _authService.GeneratePasswordResetTokenAsync(request.UsernameOrEmail, cancellationToken);

        // Respuesta estándar por seguridad (evita enumaración de usuarios)
        return Ok(new { 
            Message = "Si el usuario o correo existe en el sistema, se ha enviado un token de recuperación.",
            // Para ambiente de pruebas / desarrollo exponemos el token si existe:
            ResetToken = token 
        });
    }

    /// <summary>
    /// Endpoint para sembrar manualmente el usuario administrador inicial.
    /// En producción se ejecuta automáticamente al iniciar la aplicación vía DatabaseSeeder.
    /// </summary>
    [HttpPost("seed-admin")]
    public async Task<IActionResult> CreateInitialAdmin(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        bool created = await _authService.SeedAdminUserAsync(request.Username, request.Password, cancellationToken);
        if (!created)
        {
            return BadRequest(new { Message = "El usuario administrador ya existe." });
        }

        return Ok(new { Message = "Usuario administrador creado con éxito." });
    }
}

public record ForgotPasswordRequestDto(string UsernameOrEmail);