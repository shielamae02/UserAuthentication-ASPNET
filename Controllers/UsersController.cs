using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuthentication_ASPNET.Controllers.Utils;
using UserAuthentication_ASPNET.Services.Users;

namespace UserAuthentication_ASPNET.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger
) : ControllerBase
{

}
