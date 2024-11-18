namespace UserAuthentication_ASPNET.Models.Dtos;

public class SuccessResponseDto<T>
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
}