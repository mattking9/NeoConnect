
namespace NeoConnect
{
    public abstract class ScheduledAction : IScheduledAction
    {
        protected readonly ILogger<IScheduledAction> _logger;
        protected readonly IEmailService _emailService;

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string? Schedule { get; }
        public bool IsRunning { get; private set; }

        protected ScheduledAction(ILogger<IScheduledAction> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            var correlationId = Guid.NewGuid().ToString();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ActionId"] = Id,
                ["ActionName"] = Name
            }))
            {
                _logger.LogInformation($"** {Name} **");


                if(IsRunning)
                {
                    _logger.LogWarning($"Action {Name} is already running");
                    return;
                }

                IsRunning = true;

                try
                {
                    await ExecuteAsync(stoppingToken);
                    _logger.LogInformation($"Action {Name} completed");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"Action {Name} was canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Action {Name} failed");
                    await _emailService.SendErrorEmail(ex, stoppingToken);
                    throw;
                }
                finally
                {
                    IsRunning = false;
                }
            }
        }

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
