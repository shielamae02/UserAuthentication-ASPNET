using UserAuthentication_ASPNET.Models.Dtos.Users;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.Users;

public interface IUserService
{
    public Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(int id);
}
