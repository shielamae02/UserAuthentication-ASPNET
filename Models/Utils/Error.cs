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
    }
}