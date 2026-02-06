using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IHeatingService _heatingService;
        public DevicesController(IHeatingService heatingService)
        {
            _heatingService = heatingService;
        }

        [HttpGet(Name = "Get Devices")]
        public async Task<IEnumerable<Device>> Get()
        {
            var devices = await _heatingService.GetDevices(CancellationToken.None);
            return devices.OrderByDescending(d => d.IsHeating || d.IsPreheating || d.TimerOn).ThenBy(d => !d.IsThermostat);
        }
    }
}
