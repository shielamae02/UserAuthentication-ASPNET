using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserAuthentication_ASPNET.Controllers.Utils;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Services.AuthService;

namespace UserAuthentication_ASPNET.Controllers
{
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

    }
}