using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Services.Users;
using UserAuthentication_ASPNET.Controllers.Utils;
using UserAuthentication_ASPNET.Models.Dtos.Users;

namespace UserAuthentication_ASPNET.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger
) : ControllerBase
{
    /// <summary>
    ///     Fetches the details of the authenticated user.
    /// </summary>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> containing:
    ///     - <see cref="OkObjectResult"/> with the user's details.
    ///     - <see cref="UnauthorizedObjectResult"/> if the user credentials are invalid.
    ///     - <see cref="ProblemDetails"/> if an internal server error occurs.
    /// </returns>
    /// <response code="200">Returns the user's details.</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal Server Error.</response>
    [Authorize]
    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponseDto<UserDetailsDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponseDto))]
    public async Task<IActionResult> GetUserDetails()
    {
        var userId = ControllerUtil.GetUserId(User);

        if (userId == -1)
        {
            logger.LogWarning("Unauthorized access attempt detected for user ID: {UserId}.", userId);
            return Unauthorized();
        }

        try
        {
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
