using System.ComponentModel.DataAnnotations;
using UserAuthentication_ASPNET.Models.Utils;
using UserAuthentication_ASPNET.Models.Response;

namespace UserAuthentication_ASPNET.Services.Utils;
public static class ValidationUtil
{
    public static ApiResponse<T>? ValidateFields<T>(object obj, Dictionary<string, string> validationErrors)
    {
        var validationContext = new ValidationContext(obj);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(obj, validationContext, validationResults, true);

        validationErrors.Clear();

        if (!isValid)
        {
            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    validationErrors[memberName] = validationResult.ErrorMessage;
                }
            }

            return ApiResponse<T>.ErrorResponse(
                Error.ValidationError,
                Error.ErrorType.ValidationError,
                validationErrors
            );
        }

        return null;
    }
}
