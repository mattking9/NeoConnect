using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly IHeatingService _heatingService;
        public HistoryController(IHeatingService heatingService)
        {
            _heatingService = heatingService;
        }       

        [HttpGet(Name = "Get Device History")]
        public async Task<List<DeviceHistory>> GetHistory([FromQuery] DateTime? date)
        {
            if (!date.HasValue)
            {
                date = DateTime.Today;
            }

            return await _heatingService.GetDeviceHistory(date.Value, CancellationToken.None);
        }
    }
}
