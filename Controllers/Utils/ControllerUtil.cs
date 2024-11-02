using Microsoft.AspNetCore.Mvc;
using UserAuthentication_ASPNET.Models.Response;
using static UserAuthentication_ASPNET.Models.Utils.Error;

namespace UserAuthentication_ASPNET.Controllers.Utils
{
    public static class ControllerUtil
    {
        public static IActionResult GetActionResultFromError<T>(ApiResponse<T> apiResponse)
        {
            var errorType = apiResponse.ErrorType;

            return errorType switch
            {
                ErrorType.NotFound => new NotFoundObjectResult(apiResponse),
                ErrorType.ValidationError => new BadRequestObjectResult(apiResponse),
                ErrorType.Unauthorized => new UnauthorizedObjectResult(apiResponse),
                ErrorType.BadRequest => new BadRequestObjectResult(apiResponse),
                ErrorType.InternalServerError => new StatusCodeResult(500),
                _ => new BadRequestObjectResult(apiResponse)
            };
        }
    }
}