using System.ComponentModel.DataAnnotations;

namespace StudentApi.Dtos;

public sealed class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, MinLength(8), MaxLength(128)]
    public required string Password { get; init; }
}

public sealed class LoginDto
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

public sealed record UserDto(Guid Id, string FullName, string Email);

public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
