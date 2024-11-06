using UserAuthentication_ASPNET.Services.AuthService;

namespace UserAuthentication_ASPNET.Services;
public class AuthBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<AuthBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AuthBackgroundService is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var currentTime = DateTime.UtcNow;

            if (currentTime.Hour == 0 && currentTime.Minute == 0)
            {
                using var scope = serviceProvider.CreateScope();

                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                await authService.RemoveRevokedTokenAsync();
            }

            await Task.Delay(1000, stoppingToken);
        }

        logger.LogInformation("AuthBackgroundService has stopped.");
    }
}