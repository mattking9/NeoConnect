using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace NeoConnect
{
    /// <summary>
    /// Logger provider that creates InMemoryLogger instances and stores logs in memory.
    /// </summary>
    public class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> logs = new();
        private readonly int maxLogCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLoggerProvider"/> class.
        /// </summary>
        /// <param name="maxLogCount">Maximum number of logs to keep in memory (default: 1000).</param>
        public InMemoryLoggerProvider(int maxLogCount = 1000)
        {
            this.maxLogCount = maxLogCount;
        }

        /// <summary>
        /// Adds a log entry to the in-memory collection.
        /// </summary>
        /// <param name="logEntry">The log entry to add.</param>
        public void AddLog(LogEntry logEntry)
        {
            logs.Enqueue(logEntry);

            // Remove old logs if we exceed the max count
            while (logs.Count > maxLogCount)
            {
                logs.TryDequeue(out _);
            }
        }

        /// <summary>
        /// Gets all stored log entries.
        /// </summary>
        /// <returns>A list of log entries.</returns>
        public IReadOnlyList<LogEntry> GetLogs()
        {
            return logs.ToList();
        }

        /// <summary>
        /// Clears all stored log entries.
        /// </summary>
        public void ClearLogs()
        {
            logs.Clear();
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new InMemoryLogger(categoryName, this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            logs.Clear();
        }
    }
}
