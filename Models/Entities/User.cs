using Microsoft.EntityFrameworkCore;

namespace UserAuthentication_ASPNET.Models.Entities;
[Index(nameof(Email), IsUnique = true)]
public sealed class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = null!;

    // navigation property
    public ICollection<Token> Tokens { get; init; } = new List<Token>();
}
