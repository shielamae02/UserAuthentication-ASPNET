namespace UserAuthentication_ASPNET.Models.Dtos
{
    public class AuthResponseDto
    {
        public string Access { get; init; } = null!;
        public string Refresh { get; init; } = null!;
    }
}