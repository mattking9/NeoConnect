// <copyright file="LogEntry.cs" company="Thomson Reuters">
// Copyright (c) Thomson Reuters. All Rights Reserved. Proprietary and Confidential information of Thomson Reuters. Disclosure, Use or Reproduction without the written authorization of Thomson Reuters is prohibited.
// </copyright>

namespace NeoConnect
{
    /// <summary>
    /// Represents a single log entry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp when the log was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level (Information, Warning, Error, etc.).
        /// </summary>
        public string LogLevel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category name (usually the class name that logged).
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exception details if an error was logged.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Gets or sets the scope information for this log entry.
        /// </summary>
        public Dictionary<string, object>? Scopes { get; set; }
    }
}
