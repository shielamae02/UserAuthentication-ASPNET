using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
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
                validationErrors.Add("Email", "Invalid email address.");
                return ApiResponse<AuthResponseDto>.ErrorResponse(
                    Error.ValidationError, Error.ErrorType.ValidationError, validationErrors);
            }

            var user = mapper.Map<User>(authRegister);
            user.Password = PasswordUtil.HashPassword(authRegister.Password);

            var authDto = TokenUtil.GenerateTokens(user, configuration);

            await context.Users.AddAsync(user);
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

        if (user == null)
        {
            validationErrors.Add("Email", "Invalid credentials.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        if (!PasswordUtil.VerifyPassword(user.Password, authLogin.Password))
        {
            validationErrors.Add("Password", "Invalid credentials.");
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                Error.Unauthorized, Error.ErrorType.Unauthorized, validationErrors);
        }

        var authDto = TokenUtil.GenerateTokens(user, configuration);

        await context.SaveChangesAsync();
        return ApiResponse<AuthResponseDto>.SuccessResponse(
            Success.IS_AUTHENTICATED, authDto);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await context.Tokens.FirstOrDefaultAsync(
            u => u.Refresh.Equals(refreshToken));

        if (token is null || token.isRevoked || token.Expiration < DateTime.UtcNow)
            return false;

        token.isRevoked = true;
        await context.SaveChangesAsync();

        return true;
    }
}
