using System.ComponentModel.DataAnnotations;

namespace UserAuthentication_ASPNET.Models.Dtos.Auth;

public class AuthForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; init; } = string.Empty;
}
