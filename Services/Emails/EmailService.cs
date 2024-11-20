using Polly;
using MimeKit;
using Polly.Retry;
using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using UserAuthentication_ASPNET.Models.Utils;

namespace UserAuthentication_ASPNET.Services.Emails;

public class EmailService(
    IOptions<SMTPSettings> smtpOptions,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly static AsyncRetryPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, // Retry 3 times
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, timeSpan, retryCount, _) =>
            {
                // Log each retry attempt
                Console.WriteLine(
                    $"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
            });

    private readonly SMTPSettings _smtp = smtpOptions.Value;

    public async Task<bool> SendEmailAsync(IEnumerable<string> emails, string subject, string content)
    {
        logger.LogInformation("Sending email notification...");

        var emailList = emails.ToList();

        if (emailList.Count == 0)
        {
            logger.LogWarning("No email addresses were provided to send the notifications.");
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("no.reply.devservice@gmail.com", _smtp.Username));

        foreach (var email in emailList)
        {
            message.To.Add(new MailboxAddress("", email));
            logger.LogInformation($"Sending email to {email}");
        }

        message.Subject = subject;

        var htmlBody = EmailTemplate.GetEmailTemplateForResetPassword(subject, content);

        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };


        var result = await RetryPolicy.ExecuteAsync(async () =>
        {
            // Send the email using MailKit
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtp.Server, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                logger.LogInformation("Email sent successfully.");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogInformation("Failed to send email.");
                return false;
            }
        });

        return result;
    }
}
