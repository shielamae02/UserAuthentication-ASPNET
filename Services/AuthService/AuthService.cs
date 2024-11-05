using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
using System.IdentityModel.Tokens.Jwt;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Utils;
using UserAuthentication_ASPNET.Services.Utils;
using UserAuthentication_ASPNET.Models.Entities;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.AuthService;

public class AuthService(
    DataContext context,
    IMapper mapper,
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
            var validationResponse = ValidationUtil.ValidateFields<AuthResponseDto>(authRegister, validationErrors);

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

        var validationResponse = ValidationUtil.ValidateFields<AuthResponseDto>(authLogin, validationErrors);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(authLogin.Email));

        if (user == null)
        {
            validationErrors.Add("email", "Invalid credentials.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        if (!PasswordUtil.VerifyPassword(user.Password, authLogin.Password))
        {
            validationErrors.Add("password", "Invalid credentials.");
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

        var principal = TokenUtil.ValidateRefreshToken(refreshToken, configuration);
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
            Access = TokenUtil.GenerateAccess(token.User, configuration),
            Refresh = TokenUtil.GenerateRefresh(token.User, configuration)
        };

        return ApiResponse<AuthResponseDto>.SuccessResponse(Success.IS_AUTHENTICATED, newAccessToken);
    }

}
