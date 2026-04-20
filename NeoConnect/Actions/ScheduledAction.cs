
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
        public bool TestMode { get; set; }

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
                ["ScheduledActionName"] = Name
            }))
            {
                _logger.LogInformation($"{Name} Action Starting");


                if(IsRunning)
                {
                    _logger.LogWarning($"{Name} Action is already running");
                    return;
                }

                IsRunning = true;

                try
                {
                    await ExecuteAsync(stoppingToken);
                    _logger.LogInformation($"{Name} Action completed");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning($"{Name} Action was canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{Name} Action failed");
                    await _emailService.SendErrorEmail(ex, stoppingToken);
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
