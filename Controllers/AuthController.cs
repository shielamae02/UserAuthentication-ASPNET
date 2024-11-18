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
    [HttpPost("register")]
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
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Registration failed unexpectedly for email: {Email}.", authRegister.Email);
            return Problem("An error occured while processing your request.");
        }
    }

    [HttpPost("login")]
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

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshUserTokens([FromBody] AuthRefreshTokenDto refreshTokenDto)
    {
        try
        {
            var userId = ControllerUtil.GetUserId(User);

            if (userId == 1)
                return Unauthorized();

            var response = await authService.RefreshUserTokensAsync(refreshTokenDto.Token);
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

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutUser([FromBody] AuthRefreshTokenDto refreshTokenDto)
    {
        try
        {
            var response = await authService.LogoutAsync(refreshTokenDto.Token);

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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] AuthForgotPasswordDto forgotPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ControllerUtil.GenerateValidationError<AuthResponseDto>(ModelState));
        }

        try
        {
            var response = await authService.ForgotPasswordAsync(forgotPasswordDto);

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
