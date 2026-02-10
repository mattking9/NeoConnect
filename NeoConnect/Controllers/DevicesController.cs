using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IHeatingService _heatingService;
        public DevicesController(IHeatingService heatingService)
        {
            _heatingService = heatingService;
        }

        [HttpGet(Name = "Get Devices")]
        public async Task<IEnumerable<Device>> Get([FromQuery] bool includeAdvancedData)
        {
            var devices = await _heatingService.GetDevices(includeAdvancedData, CancellationToken.None);
            return devices.OrderByDescending(d => d.IsHeating || d.IsPreheating || d.TimerOn).ThenBy(d => !d.IsThermostat);
        }
    }
}
