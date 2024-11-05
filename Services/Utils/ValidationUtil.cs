using System.ComponentModel.DataAnnotations;

namespace UserAuthentication_ASPNET.Services.Utils;
public static class ValidationUtil
{
    public static void ValidateFields(object obj, Dictionary<string, string> validationErrors)
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
                    var newMemberName = char.ToLower(memberName[0]) + memberName[1..];
                    validationErrors[newMemberName] = validationResult.ErrorMessage ?? "";
                }
            }
        }
    }
}
