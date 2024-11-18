using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Dtos.Auth;
using UserAuthentication_ASPNET.Controllers.Utils;
using UserAuthentication_ASPNET.Services.AuthService;

namespace UserAuthentication_ASPNET.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    ///     Registers a new user.
    /// </summary>
    /// <param name="authRegister"></param>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> containing:
    ///     - <see cref="StatusCodeResult" /> with the access and refresh tokens.  
    ///     - <see cref="BadRequestObjectResult" /> if the request is invalid.  
    ///     - <see cref="ProblemDetails" /> if an internal server error occurs.  
    /// </returns>
    /// <response code="201"> Returns the access and refresh tokens. </response>
    /// <response code="400"> Bad request. </response>
    /// <response code="500"> Internal server error. </response>
    [HttpPost("register")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SuccessResponseDto<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterUser([FromBody] AuthRegisterDto authRegister)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var response = await authService.RegisterAsync(authRegister);

            if (response.Status.Equals("error"))
            {
                logger.LogWarning("Registration attempt failed for email: {Email}", authRegister.Email);
                return ControllerUtil.GetActionResultFromError(response);
            }

            logger.LogInformation("Registration successful for email: {Email}", authRegister.Email);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Registration failed unexpectedly for email: {Email}.", authRegister.Email);
            return Problem("An error occured while processing your request.");
        }
    }

    /// <summary>
    ///     Authenticates registered user.
    /// </summary>
    /// <param name="authLogin"></param>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> containing:
    ///     - <see cref="OkObjectResult"/> with the access and refresh tokens.
    ///     - <see cref="BadRequestObjectResult"/> if the request is invalid.
    ///     - <see cref="UnauthorizedObjectResult"/> if the user entered invalid credentials.
    ///     - <see cref="ProblemDetails" /> if an internal server error occurs. 
    /// </returns>
    /// <response code="200"> Returns the access and refresh tokens. </response>
    /// <response code="400"> Bad request. </response>
    /// <response code="401"> Unauthorized access. </response>
    /// <response code="500"> Internal server error. </response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponseDto<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginUser([FromBody] AuthLoginDto authLogin)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var response = await authService.LoginAsync(authLogin);

            if (response.Status.Equals("error"))
            {
                logger.LogWarning("Login attempt failed for email: {Email}", authLogin.Email);
                return ControllerUtil.GetActionResultFromError(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Login failed unexpectedly for email: {Email}.", authLogin.Email);
            return Problem("An error occured while processing your request.");
        }
    }

    /// <summary>
    ///     Refreshes the user's authentication tokens.
    /// </summary>
    /// <param name="authRefreshToken"></param>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> containing:
    ///     - <see cref="OkObjectResult"/> if the request is valid.
    ///     - <see cref="BadRequestObjectResult"/> if the request is invalid.
    ///     - <see cref="UnauthorizedObjectResult"/> if a credential is invalid.
    ///     - <see cref="ProblemDetails"/> if an internal server error occurs.
    /// </returns>
    /// <response code="200">Returns the new access and refresh tokens.</response>
    /// <response code="400">Bad request.</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpPost("refresh")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponseDto<AuthResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshUserTokens([FromBody] AuthRefreshTokenDto authRefreshToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var userId = ControllerUtil.GetUserId(User);

            if (userId == 1)
                return Unauthorized();

            var response = await authService.RefreshUserTokensAsync(authRefreshToken.Token);
            if (response.Status.Equals("error"))
            {
                logger.LogWarning("Failed to refresh token for userId: {UserId}", userId);
                return ControllerUtil.GetActionResultFromError(response);
            }

            logger.LogInformation("Successfully refreshed token for userId: {UserId}", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to refresh token.");
            return Problem("An error occured while processing your request.");
        }
    }

    /// <summary>
    ///     Blacklists the user's refresh token.
    /// </summary>
    /// <param name="authRefreshToken"></param>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> contaning:
    ///     - <see cref="NoContentResult"/> if the request is valid.
    ///     - <see cref="BadRequestObjectResult"/> if the request is invalid.
    ///     - <see cref="ProblemDetails"/> if an internal server error occurs.
    /// </returns>
    /// <response code="204">No content.</response> 
    /// <response code="400">Bad request.</response> 
    /// <response code="500">Internal server error.</response> 
    [HttpPost("logout")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LogoutUser([FromBody] AuthRefreshTokenDto authRefreshToken)
    {
        try
        {
            var response = await authService.LogoutAsync(authRefreshToken.Token);

            if (!response)
            {
                logger.LogWarning("Logout failed.");
                return BadRequest();
            }

            logger.LogInformation("User successfully logged out.");
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error in logging out user.");
            return Problem("An error occurred while processing your request.");
        }
    }

    /// <summary>
    ///     Sends an email with the reset password link.
    /// </summary>
    /// <param name="authForgotPassword"></param>
    /// <returns>
    ///     Returns an <see cref="IActionResult"/> containing:
    ///     - <see cref="OkObjectResult"/> if the request is valid.
    ///     - <see cref="BadRequestObjectResult"/> if the request is invalid.
    ///     - <see cref="ProblemDetails"/> if an internal server error occurs.
    /// </returns>
    /// <response code="200">Sends an email to the user.</response>
    /// <response code="400">Bad request.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("forgot-password")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessResponseDto<string>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponseDto))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] AuthForgotPasswordDto authForgotPassword)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var response = await authService.ForgotPasswordAsync(authForgotPassword);

            if (response.Status.Equals("error"))
            {
                return ControllerUtil.GetActionResultFromError(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to send email for password reset.");
            return Problem("An error occurred while processing your request.");
        }
    }

    [HttpPost("reset-password/{token}")]
    public async Task<IActionResult> ResetPassword([FromRoute][Required] string token, [FromBody] AuthResetPasswordDto resetPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var response = await authService.ResetPasswordAsync(token, resetPasswordDto);

            if (response.Status.Equals("error"))
            {
                return ControllerUtil.GetActionResultFromError(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Failed to reset user password.");
            return Problem("An error occurred while processing your request.");
        }
    }
}
