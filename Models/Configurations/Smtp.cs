namespace UserAuthentication_ASPNET.Models.Configurations;

public class Smtp
{
    public string Server { get; set; } = null!;
    public int Port { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
