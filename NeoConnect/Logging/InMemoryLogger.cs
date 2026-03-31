// <copyright file="InMemoryLogger.cs" company="Thomson Reuters">
// Copyright (c) Thomson Reuters. All Rights Reserved. Proprietary and Confidential information of Thomson Reuters. Disclosure, Use or Reproduction without the written authorization of Thomson Reuters is prohibited.
// </copyright>

namespace NeoConnect
{
    /// <summary>
    /// Logger implementation that stores logs in memory.
    /// </summary>
    public class InMemoryLogger : ILogger
    {
        private readonly string categoryName;
        private readonly InMemoryLoggerProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <param name="provider">The logger provider.</param>
        public InMemoryLogger(string categoryName, InMemoryLoggerProvider provider)
        {
            this.categoryName = categoryName;
            this.provider = provider;
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return new LoggerScope(state);
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var scopes = LoggerScope.GetScopeDictionary();

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                LogLevel = logLevel.ToString(),
                Category = categoryName,
                Message = formatter(state, exception),
                Exception = exception?.ToString(),
                Scopes = scopes.Count > 0 ? scopes : null,
            };

            provider.AddLog(logEntry);
        }
    }
}
