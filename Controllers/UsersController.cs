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
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetUserDetails()
    {
        try
        {
            var userId = ControllerUtil.GetUserId(User);

            if (userId == -1)
            {
                logger.LogWarning("Unauthorized access attempt detected for user ID: {UserId}.", userId);
                return Unauthorized();
            }

            var response = await userService.GetUserDetailsAsync(userId);

            logger.LogInformation("Successfully fetched user details for userId: {UserId}.", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to fetch user details.");
            return Problem("An error occurred while processing your request.");
        }
    }
}
