using UserAuthentication_ASPNET.Models.Dtos.Users;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.Users;

public interface IUserService
{
    /// <summary>
    ///     Fetches the details of the authenticated user.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    ///     The details of the user.
    /// </returns>
    public Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(int id);
}
