using System.ComponentModel.DataAnnotations;

namespace UserAuthentication_ASPNET.Models.Dtos.Auth;

public class AuthResetPasswordDto
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string RePassword { get; init; } = string.Empty;
}
