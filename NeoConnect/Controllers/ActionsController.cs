using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActionsController : ControllerBase
    {        
        private readonly IEnumerable<IScheduledAction> _actions;

        public ActionsController(IEnumerable<IScheduledAction> actions)
        {
            _actions = actions;
        }

        [HttpPost(Name = "Perform Action")]
        public async Task<ActionResult> Post([FromQuery] string actionName)
        {
            var action = _actions.FirstOrDefault(a => a.Id == actionName);

            if(action == null)
            {
                return BadRequest($"Invalid action name.");
            }

            await action.Run(CancellationToken.None);

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            return Ok(_actions);
        }
    }
}
