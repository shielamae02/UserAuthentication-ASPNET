public class PasswordUtil
{
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool VerifyPassword(string hashPassword, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashPassword);
    }
}