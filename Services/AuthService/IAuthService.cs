using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.AuthService;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(AuthRegisterDto authRegister);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(AuthLoginDto authLogin);
    Task<bool> LogoutAsync(string refreshToken);
    Task<ApiResponse<AuthResponseDto>> RefreshUserTokensAsync(string refreshToken);
}
