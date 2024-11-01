namespace UserAuthentication_ASPNET.Models.Utils
{
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

    }
}