﻿
namespace NeoConnect
{
    public interface IHeatingService
    {
        List<string> GetChangesMade();
        Task Cleanup(CancellationToken stoppingToken);
        Task Init(CancellationToken stoppingToken);
        Task RunRecipeBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken);
        Task SetPreheatDurationBasedOnWeatherConditions(ForecastDay forecastToday, CancellationToken stoppingToken);
    }
}