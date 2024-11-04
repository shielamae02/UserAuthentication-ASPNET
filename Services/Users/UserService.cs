using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserAuthentication_ASPNET.Data;
using UserAuthentication_ASPNET.Models.Dtos.Users;
using UserAuthentication_ASPNET.Models.Response;
using UserAuthentication_ASPNET.Models.Utils;

namespace UserAuthentication_ASPNET.Services.Users;
public class UserService(
    DataContext context,
    IMapper mapper) : IUserService
{
    public async Task<ApiResponse<UserDetailsDto>> GetUserDetailsAsync(int id)
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Id.Equals(id));

        var userDetails = mapper.Map<UserDetailsDto>(user);

        return ApiResponse<UserDetailsDto>.SuccessResponse(Success.RESOURCE_RETRIEVED("User"), userDetails);
    }
}
