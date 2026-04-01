using Microsoft.AspNetCore.Mvc;

namespace NeoConnect
{
    /// <summary>
    /// Controller for retrieving application logs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly InMemoryLoggerProvider loggerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsController"/> class.
        /// </summary>
        /// <param name="loggerProvider">The in-memory logger provider.</param>
        public LogsController(InMemoryLoggerProvider loggerProvider)
        {
            this.loggerProvider = loggerProvider;
        }

        /// <summary>
        /// Gets all log entries stored in memory.
        /// </summary>
        /// <param name="level">Optional filter by log level (e.g., Information, Warning, Error).</param>
        /// <param name="count">Maximum number of most recent logs to return (default: all).</param>
        /// <param name="actionId">Optional filter by ActionId scope.</param>
        /// <param name="actionName">Optional filter by ActionName scope.</param>
        /// <returns>A list of log entries.</returns>
        [HttpGet]
        public ActionResult<IEnumerable<LogEntry>> GetLogs(
            [FromQuery] string? level = null, 
            [FromQuery] int? count = null,
            [FromQuery] string? actionName = null)
        {
            var logs = loggerProvider.GetLogs();

            if (!string.IsNullOrEmpty(level))
            {
                logs = logs.Where(l => l.LogLevel.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(actionName))
            {
                logs = logs.Where(l => 
                    l.Scopes != null && 
                    l.Scopes.ContainsKey("ScheduledActionName") && 
                    l.Scopes["ScheduledActionName"].ToString() == actionName).ToList();
            }

            if (count.HasValue && count.Value > 0)
            {
                logs = logs.OrderBy(l => l.Timestamp).Take(count.Value).ToList();
            }

            return Ok(logs);
        }

        /// <summary>
        /// Gets a list of distinct action names from log scopes.
        /// </summary>
        /// <returns>A list of unique action names.</returns>
        [HttpGet("action-names")]
        public ActionResult<IEnumerable<string>> GetActionNames()
        {
            var actionNames = loggerProvider.GetLogs()
                .Where(l => l.Scopes != null && l.Scopes.ContainsKey("ScheduledActionName"))
                .Select(l => l.Scopes["ScheduledActionName"].ToString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            return Ok(actionNames);
        }

        /// <summary>
        /// Clears all log entries from memory.
        /// </summary>
        /// <returns>A success message.</returns>
        [HttpDelete]
        public ActionResult ClearLogs()
        {
            loggerProvider.ClearLogs();
            return Ok(new { message = "Logs cleared successfully" });
        }
    }
}
