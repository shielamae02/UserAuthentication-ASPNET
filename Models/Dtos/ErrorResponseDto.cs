using static UserAuthentication_ASPNET.Models.Utils.Error;

namespace UserAuthentication_ASPNET.Models.Dtos;

public class ErrorResponseDto
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = string.Empty;
    public ErrorType? ErrorType { get; init; }
    public Dictionary<string, string>? ValidationErrors { get; init; }

}