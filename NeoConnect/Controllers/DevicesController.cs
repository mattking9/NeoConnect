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

        [HttpGet(Name = "Get All Devices")]
        public async Task<IEnumerable<Device>> GetAllDevices([FromQuery] bool includeAdvancedData)
        {
            var devices = await _heatingService.GetDevices(includeAdvancedData, CancellationToken.None);
            return devices.OrderByDescending(d => d.IsHeating || d.IsPreheating || d.TimerOn).ThenBy(d => !d.IsThermostat);
        }

        [HttpPut("temperature", Name = "Set Device Temperature")]
        public async Task<IActionResult> SetTemperature([FromBody] Device device)
        {
            await _heatingService.SetTemperature(device.ZoneName, device.SetTemp, CancellationToken.None);
            return Ok();
        }

        [HttpGet("history", Name = "Get Device History")]
        public async Task<IEnumerable<DeviceHistory>> GetHistory([FromQuery] DateTime? date)
        {
            if (!date.HasValue)
            {
                date = DateTime.Today;
            }

            return await _heatingService.GetDeviceHistory(date.Value);
        }

        [HttpGet("setup", Name = "Setup")] // Use GET so that we can call directly from a browser
        public async Task<IActionResult> Setup()
        {
            await _heatingService.RefreshDeviceList(CancellationToken.None);
            return Ok();
        }
    }
}
