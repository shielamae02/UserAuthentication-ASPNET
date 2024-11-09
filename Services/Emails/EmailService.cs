using MailKit.Security;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;
using UserAuthentication_ASPNET.Models.Configurations;

namespace UserAuthentication_ASPNET.Services.Emails;

public class EmailService(
    IOptions<Smtp> smtpOptions,
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

    private readonly Smtp _smtp = smtpOptions.Value;


}
