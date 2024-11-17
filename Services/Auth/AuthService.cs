using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
using System.IdentityModel.Tokens.Jwt;
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
    IConfiguration configuration,
    ILogger<AuthService> logger
) : IAuthService
{
    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(AuthRegisterDto authRegister)
    {
        Dictionary<string, string> validationErrors = [];

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (await context.Users.AnyAsync(u => u.Email.Equals(authRegister.Email)))
            {
                validationErrors.Add("email", "Invalid email address.");
                return ApiResponse<AuthResponseDto>.ErrorResponse(
                    Error.ValidationError, Error.ErrorType.ValidationError, validationErrors);
            }

            var user = mapper.Map<User>(authRegister);
            user.Password = PasswordUtil.HashPassword(authRegister.Password);

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var authDto = TokenUtil.GenerateTokens(user, configuration);

            var token = new Token
            {
                UserId = user.Id,
                User = user,
                Refresh = authDto.Refresh,
                Expiration = DateTime.UtcNow.AddDays(
                    Convert.ToDouble(configuration["JWT:RefreshTokenExpiry"]))
            };

            user.Tokens.Add(token);
            await context.Tokens.AddAsync(token);
            await context.SaveChangesAsync();
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
        Dictionary<string, string> validationErrors = [];

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authLogin.Email));

        if (user == null || !PasswordUtil.VerifyPassword(user.Password, authLogin.Password))
        {
            validationErrors.Add("user", "Invalid credentials.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        var authDto = TokenUtil.GenerateTokens(user, configuration);

        var token = new Token
        {
            User = user,
            UserId = user.Id,
            Refresh = authDto.Refresh,
            Expiration = DateTime.UtcNow.AddDays(Convert.ToDouble(configuration["JWT:RefreshTokenExpiry"]))
        };

        await context.Tokens.AddAsync(token);
        await context.SaveChangesAsync();
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

        var principal = TokenUtil.ValidateToken(refreshToken, configuration);
        if (principal == null)
        {
            validationErrors.Add("token", "Invalid refresh token.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        var token = await context.Tokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Refresh == refreshToken);

        if (token == null || token.IsRevoked || token.Expiration < DateTime.UtcNow)
        {
            validationErrors.Add("Token", "Refresh token is already expired or invalid.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        var expClaim = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrEmpty(expClaim) ||
        !long.TryParse(expClaim, out var expSeconds) ||
        DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime < DateTime.UtcNow.AddMinutes(10))
        {
            var user = token.User;
            var newTokensGenerated = TokenUtil.GenerateTokens(user, configuration);

            token.IsRevoked = true;

            var newRefreshToken = new Token
            {
                UserId = user.Id,
                Refresh = newTokensGenerated.Refresh,
                Expiration = DateTime.UtcNow.AddDays(Convert.ToDouble(configuration["JWT:RefreshTokenExpiry"]))
            };

            user.Tokens.Add(newRefreshToken);
            await context.Tokens.AddAsync(newRefreshToken);
            await context.SaveChangesAsync();

            return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, newTokensGenerated);
        }

        var newAccessToken = new AuthResponseDto
        {
            Access = TokenUtil.GenerateToken(token.User, configuration, TokenType.ACCESS),
            Refresh = TokenUtil.GenerateToken(token.User, configuration, TokenType.REFRESH)
        };

        return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, newAccessToken);
    }
    public async Task RemoveRevokedTokenAsync()
    {
        var tokens = await context.Tokens
            .Where(t => t.IsRevoked || t.Expiration < DateTime.UtcNow)
            .ToListAsync();

        if (tokens.Count != 0)
        {
            context.Tokens.RemoveRange(tokens);
            await context.SaveChangesAsync();
        }
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(AuthForgotPasswordDto forgotPasswordDto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(forgotPasswordDto.Email));
        if (user == null)
        {
            return ApiResponse<string>.SuccessResponse(Success.PASSWORD_RESET_INSTRUCTION_SENT, null);
        }

        var resetToken = TokenUtil.GenerateToken(user, configuration, TokenType.RESET);

        var resetLink = $"http://localhost:5077/reset-password?token={resetToken}";

        var isEmailSent = await emailService.SendEmailAsync(
            emails: [forgotPasswordDto.Email],
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

        return ApiResponse<string>.SuccessResponse(Success.PASSWORD_RESET_INSTRUCTION_SENT, resetToken);
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(string token, AuthResetPasswordDto resetPasswordDto)
    {
        Dictionary<string, string> validationErrors = [];

        var principal = TokenUtil.ValidateToken(token, configuration);

        if (principal == null)
        {
            validationErrors.Add("token", "Invalid token payload.");
            return ApiResponse<string>.ErrorResponse(
                Error.Unauthorized,
                Error.ErrorType.Unauthorized,
                validationErrors
            );
        }

        var purposeClaim = principal.Claims.FirstOrDefault(c => c.Type == "purpose" && c.Value == "password-reset");
        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        if (purposeClaim == null || userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
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
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            validationErrors.Add("user", "Invalid credentials.");
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

                user.Password = PasswordUtil.HashPassword(resetPasswordDto.Password);

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
}
