using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("[controller]")]
    public class SchedulesController : ControllerBase
    {
        private readonly IHeatingService _heatingService;
        public SchedulesController(IHeatingService heatingService)
        {
            _heatingService = heatingService;
        }

        [HttpGet(Name = "Get Schedules")]
        public async Task<IEnumerable<Schedule>> Get()
        {
            return await _heatingService.GetSchedules(CancellationToken.None);            
        }
    }
}
