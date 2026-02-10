using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActionsController : ControllerBase
    {
        private readonly IHeatingService _heatingService;
        private readonly IWeatherService _weatherService;

        private const string BATHROOM_BOOST = "bathroom_boost";
        private const string GLOBAL_HOLD = "global_hold";
        private const string DATA_COLLECTION = "data_collection";

        public ActionsController(IHeatingService heatingService, IWeatherService weatherService)
        {
            _heatingService = heatingService;
            _weatherService = weatherService;
        }

        [HttpPost(Name = "Perform Action")]
        public async Task<ActionResult> Post([FromQuery] string actionName)
        {
            switch (actionName)
            {
                case BATHROOM_BOOST:
                    await _heatingService.BoostTowelRailWhenBathroomIsCold(CancellationToken.None);
                    return Ok();
                case GLOBAL_HOLD:
                    {
                        var forecast = await _weatherService.GetForecast(CancellationToken.None);
                        await _heatingService.ReduceSetTempWhenExternalTempIsWarm(forecast.ForecastDay[0], CancellationToken.None);
                    }
                    return Ok();
                case DATA_COLLECTION:
                    await _heatingService.LogDeviceStatuses(CancellationToken.None);
                    return Ok();
                default:
                    return BadRequest($"Invalid action name. Valid values are {BATHROOM_BOOST}, {GLOBAL_HOLD}, {DATA_COLLECTION}");
            };
        }
    }
}
