namespace UserAuthentication_ASPNET.Services.Emails;

public class EmailBackgroundService(
    IEmailService emailService,
    EmailQueue emailQueue,
    ILogger<EmailBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailBackgroundService is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (emailQueue.TryDequeue(out var email))
                {
                    var isSuccess = await emailService.SendEmailAsync(email.emails, email.subject, email.content);

                    if (isSuccess)
                    {
                        logger.LogInformation("Email sent successfully.");
                    }
                    else
                    {
                        logger.LogError("Failed to send email.");
                        emailQueue.QueueEmail(email.emails, email.subject, email.content);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("EmailBackgroundService is stopping.");
            }
        }
    }
}
