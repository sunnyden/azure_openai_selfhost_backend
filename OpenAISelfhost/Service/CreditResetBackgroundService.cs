using OpenAISelfhost.Service.Interface;

namespace OpenAISelfhost.Service
{
    public class CreditResetBackgroundService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<CreditResetBackgroundService> logger;
        private readonly TimeSpan checkInterval = TimeSpan.FromHours(24); // Check daily

        public CreditResetBackgroundService(IServiceProvider serviceProvider, ILogger<CreditResetBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Credit Reset Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                        userService.ProcessMonthlyCreditReset();
                        logger.LogInformation("Monthly credit reset check completed at {Time}", DateTime.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred during monthly credit reset check");
                }

                await Task.Delay(checkInterval, stoppingToken);
            }
        }
    }
}