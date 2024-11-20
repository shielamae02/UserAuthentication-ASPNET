using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Response;
using UserAuthentication_ASPNET.Models.Dtos.Auth;

namespace UserAuthentication_ASPNET.Services.AuthService;

public interface IAuthService
{
    /// <summary>
    ///     Registers a new user.
    /// </summary>
    /// <param name="authRegister"></param>
    /// <returns>
    ///     Access and refresh tokens if the registration is successful.
    /// </returns>
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(AuthRegisterDto authRegister);

    /// <summary>
    ///     Logs in a registered user.
    /// </summary>
    /// <param name="authLogin"></param>
    /// <returns>
    ///     Access and refresh tokens if the login is successful.
    /// </returns>
    Task<ApiResponse<AuthResponseDto>> LoginAsync(AuthLoginDto authLogin);

    /// <summary>
    ///     Logouts a user and invalidate their refresh token.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns>
    ///     A boolean indicating whether the process was successful.
    /// </returns>
    Task<bool> LogoutAsync(string refreshToken);

    /// <summary>
    ///     Refreshes the access and refresh tokens for the authenticated user.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns>
    ///     Access and refresh tokens.
    /// </returns>
    Task<ApiResponse<AuthResponseDto>> RefreshUserTokensAsync(string refreshToken);

    /// <summary>
    ///     Sends an email with the reset password link.
    /// </summary>
    /// <returns>
    ///     Message indicating whether the sending of email was successful.
    /// </returns>
    Task<ApiResponse<string>> ForgotPasswordAsync(AuthForgotPasswordDto authForgotPassword);

    /// <summary>
    ///     Resets the user's password.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="authResetPassword"></param>
    /// <returns>
    ///     Message indicating whether the password reset process was successful.
    /// </returns>
    Task<ApiResponse<string>> ResetPasswordAsync(string token, AuthResetPasswordDto authResetPassword);

    /// <summary>
    ///     Remove the user's revoked and expired tokens.
    /// </summary>
    Task RemoveRevokedTokenAsync();
}
