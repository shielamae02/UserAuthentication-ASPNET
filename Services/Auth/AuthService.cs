using AutoMapper;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
using UserAuthentication_ASPNET.Models;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Utils;
using UserAuthentication_ASPNET.Services.Utils;
using UserAuthentication_ASPNET.Models.Entities;
using UserAuthentication_ASPNET.Models.Response;
using UserAuthentication_ASPNET.Services.Emails;
using UserAuthentication_ASPNET.Models.Dtos.Auth;

namespace UserAuthentication_ASPNET.Services.AuthService;

public class AuthService(
    DataContext context,
    IMapper mapper,
    IEmailService emailService,
    JWTSettings jwt,
    ILogger<AuthService> logger
) : IAuthService
{
    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(AuthRegisterDto authRegister)
    {
        var validationErrors = new Dictionary<string, string>();

        if (await context.Users.AnyAsync(u => u.Email.Equals(authRegister.Email)))
        {
            validationErrors.Add("email", "Invalid email address.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.ValidationError, Error.ErrorType.ValidationError, validationErrors);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var user = mapper.Map<User>(authRegister);
            user.Password = PasswordUtil.HashPassword(authRegister.Password);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var authDto = TokenUtil.GenerateTokens(user, jwt);
            await SaveRefreshTokenAsync(user, authDto.Refresh, jwt.RefreshTokenExpiry);
            await transaction.CommitAsync();

            return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, authDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "An error occurred while registering the user.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(Error.ERROR_CREATING_RESOURCE("user"), Error.ErrorType.InternalServerError, validationErrors);
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(AuthLoginDto authLogin)
    {
        var validationErrors = new Dictionary<string, string>();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authLogin.Email));

        if (user is null || !PasswordUtil.VerifyPassword(user.Password, authLogin.Password))
        {
            validationErrors.Add("user", "Invalid credentials.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        var authDto = TokenUtil.GenerateTokens(user, jwt);
        await SaveRefreshTokenAsync(user, authDto.Refresh, jwt.RefreshTokenExpiry);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            Success.IS_AUTHENTICATED, authDto);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await context.Tokens.FirstOrDefaultAsync(
            u => u.Refresh.Equals(refreshToken));

        if (token is null || token.IsRevoked || token.Expiration < DateTime.UtcNow)
            return false;

        token.IsRevoked = true;
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshUserTokensAsync(string refreshToken)
    {
        var validationErrors = new Dictionary<string, string>();

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var principal = TokenUtil.ValidateToken(refreshToken, jwt);
            if (principal is null)
            {
                validationErrors.Add("token", "Invalid refresh token.");
                return ApiResponse<AuthResponseDto>.ErrorResponse(
                    Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
            }

            var token = await context.Tokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Refresh == refreshToken);

            if (token is null || token.IsRevoked || token.Expiration < DateTime.UtcNow)
            {
                validationErrors.Add("token", "Refresh token is already expired or invalid.");
                return ApiResponse<AuthResponseDto>.ErrorResponse(
                    Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
            }

            if (!TokenUtil.IsTokenNearExpiration(principal, bufferMinutes: 10))
            {
                var newAccessToken = new AuthResponseDto
                {
                    Refresh = refreshToken,
                    Access = TokenUtil.GenerateToken(
                            token.User, jwt, TokenType.ACCESS)
                };

                return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, newAccessToken);
            }

            var user = token.User;
            token.IsRevoked = true;

            var authDto = TokenUtil.GenerateTokens(user, jwt);

            await SaveRefreshTokenAsync(user, authDto.Refresh, jwt.RefreshTokenExpiry);
            await transaction.CommitAsync();

            return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, authDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "An error occurred while refreshing user's token.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(Error.ERROR_CREATING_RESOURCE("token"), Error.ErrorType.InternalServerError, validationErrors);
        }
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(AuthForgotPasswordDto authForgotPassword)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authForgotPassword.Email));
        if (user is null)
        {
            return ApiResponse<string>.SuccessResponse(Success.PASSWORD_RESET_INSTRUCTION_SENT, null);
        }

        var resetToken = TokenUtil.GenerateToken(user, jwt, TokenType.RESET);
        var resetLink = $"http://localhost:5077/reset-password?token={resetToken}";

        var isEmailSent = await emailService.SendEmailAsync(
            emails: [authForgotPassword.Email],
            subject: "Password Reset Request",
            content: resetLink
        );

        if (!isEmailSent)
        {
            return ApiResponse<string>.ErrorResponse(
                Error.EmailSendFailed,
                Error.ErrorType.BadRequest,
                new Dictionary<string, string>{{
                    "email", "Failed to send email for password reset."
                }}
            );
        }

        await SaveRefreshTokenAsync(user, resetToken, jwt.ResetTokenExpiry);

        return ApiResponse<string>.SuccessResponse(Success.PASSWORD_RESET_INSTRUCTION_SENT, null);
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(string token, AuthResetPasswordDto authResetPassword)
    {
        var validationErrors = new Dictionary<string, string>();

        var principal = TokenUtil.ValidateToken(token, jwt);

        if (principal is null)
        {
            validationErrors.Add("token", "Invalid token payload.");
            return ApiResponse<string>.ErrorResponse(
                Error.Unauthorized,
                Error.ErrorType.Unauthorized,
                validationErrors
            );
        }

        var purposeClaim = principal.Claims.FirstOrDefault(c => c.Type == "purpose" && c.Value == "reset-password")?.Value;
        var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(purposeClaim) || string.IsNullOrEmpty(emailClaim))
        {
            validationErrors.Add("token", "Invalid token payload.");
            return ApiResponse<string>.ErrorResponse(
                Error.Unauthorized,
                Error.ErrorType.Unauthorized,
                validationErrors
            );
        }

        // Find the user
        var user = await context.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Email.Equals(emailClaim));

        if (user is null)
        {
            validationErrors.Add("user", "Invalid credentials.");
            return ApiResponse<string>.ErrorResponse(
               Error.Unauthorized,
               Error.ErrorType.Unauthorized,
               validationErrors
           );
        }

        var isTokenValid = user.Tokens.Any(t =>
                t.Refresh.Equals(token) &&
                !t.IsRevoked &&
                t.Expiration > DateTime.UtcNow);

        if (!isTokenValid)
        {
            validationErrors.Add("token", "It looks like you clicked on an invalid password reset link. Please try again.");
            return ApiResponse<string>.ErrorResponse(
               Error.Unauthorized,
               Error.ErrorType.Unauthorized,
               validationErrors
           );
        }

        using (var transaction = await context.Database.BeginTransactionAsync())
        {
            try
            {
                var activeTokens = user.Tokens.Where(t => t.Expiration > DateTime.UtcNow && !t.IsRevoked);
                foreach (var activeToken in activeTokens)
                {
                    activeToken.IsRevoked = true;
                }

                user.Password = PasswordUtil.HashPassword(authResetPassword.Password);

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponse<string>.SuccessResponse(
                    Success.PASSWORD_RESET_SUCCESSFUL, null);

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                validationErrors.Add("transaction", "An error occurred during password reset.");
                return ApiResponse<string>.ErrorResponse(
                    Error.ValidationError,
                    Error.ErrorType.InternalServerError,
                    validationErrors
                );
            }
        }
    }

    private async Task SaveRefreshTokenAsync(User user, string refreshToken, int expiryDays)
    {
        var token = new Token
        {
            User = user,
            UserId = user.Id,
            Refresh = refreshToken,
            Expiration = DateTime.UtcNow.AddDays(expiryDays)
        };


        user.Tokens.Add(token);
        await context.Tokens.AddAsync(token);
        await context.SaveChangesAsync();
    }

    public async Task RemoveRevokedTokenAsync()
    {
        await context.Tokens
            .Where(t => t.IsRevoked || t.Expiration < DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }
}
