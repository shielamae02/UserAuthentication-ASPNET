namespace UserAuthentication_ASPNET.Services.Emails;

public interface IEmailService
{
    Task<bool> SendEmailAsync(IEnumerable<string> emails, string subject, string content);
}