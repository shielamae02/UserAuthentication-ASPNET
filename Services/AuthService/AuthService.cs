using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Utils;
using UserAuthentication_ASPNET.Services.Utils;
using UserAuthentication_ASPNET.Models.Entities;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.AuthService
{
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
    }
}