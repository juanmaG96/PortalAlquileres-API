using System.ComponentModel.DataAnnotations;

namespace Marketplace.API.Data.Dtos;

public record LoginRequestDto(
    [Required] string Username, 
    [Required] string Password
);

public record AuthResponseDto(
    string Token, 
    string Username, 
    DateTime Expiration
);