using AutoMapper;
using UserAuthentication_ASPNET.Data;
using UserAuthentication_ASPNET.Models.Dtos;
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

    }
}