namespace NeoConnect
{
    public class ActionsService
    {
        private readonly IHeatingService _heatingService;
        private readonly ILogger<ActionsService> _logger;
        private readonly IWeatherService _weatherService;
        private readonly IEmailService _emailService;

        public ActionsService(IHeatingService heatingService, ILogger<ActionsService> logger, IWeatherService weatherService, IEmailService emailService)
        {
            _logger = logger;
            _weatherService = weatherService;
            _heatingService = heatingService;
            _emailService = emailService;
        }

        public async Task PerformActions(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Actions.");

            try
            {
                var forecast = await _weatherService.GetForecast(stoppingToken);

                await _heatingService.Init(stoppingToken);

                await _heatingService.BoostTowelRailWhenBathroomIsCold(stoppingToken);

                await _heatingService.ReduceSetTempWhenExternalTempIsWarm(forecast.ForecastDay[0], stoppingToken);                
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution Error. Aborting.");
                try
                {
                    await _emailService.SendErrorEmail(ex, stoppingToken);
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                await _heatingService.Cleanup(stoppingToken);
                _logger.LogInformation("Process Completed.");
            }
        }
    }
}
