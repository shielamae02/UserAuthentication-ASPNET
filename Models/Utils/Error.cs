namespace UserAuthentication_ASPNET.Models.Utils;

public static class Error
{
    public enum ErrorType
    {
        ValidationError,
        Unauthorized,
        NotFound,
        BadRequest,
        InternalServerError
    }

    public const string ValidationError = "Validation failed";
    public const string Unauthorized = "Access denied";
    public const string NotFound = "Resource not found";
    public const string AlreadyExists = "Already exists";

    public static string FIELD_IS_REQUIRED(string field)
    {
        return $"{field} is required";
    }

    public static string ERROR_FETCHING_RESOURCE(string resource)
    {
        return $"Failed to fetch {resource}";
    }

    public static string ERROR_CREATING_RESOURCE(string resource)
    {
        return $"Failed to create {resource}";
    }

    public static string ERROR_UPDATING_RESOURCE(string resource)
    {
        return $"Failed to update {resource}";
    }

    public static string ERROR_DELETING_RESOURCE(string resource)
    {
        return $"Failed to delete {resource}";
    }
}