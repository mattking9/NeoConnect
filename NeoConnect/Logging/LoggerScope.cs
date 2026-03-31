// <copyright file="LoggerScope.cs" company="Thomson Reuters">
// Copyright (c) Thomson Reuters. All Rights Reserved. Proprietary and Confidential information of Thomson Reuters. Disclosure, Use or Reproduction without the written authorization of Thomson Reuters is prohibited.
// </copyright>

namespace NeoConnect
{
    /// <summary>
    /// Represents a logging scope that can be disposed to end the scope.
    /// </summary>
    internal sealed class LoggerScope : IDisposable
    {
        private static readonly AsyncLocal<LoggerScope?> CurrentScope = new();
        private readonly LoggerScope? parent;
        private readonly object state;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerScope"/> class.
        /// </summary>
        /// <param name="state">The state object for this scope.</param>
        internal LoggerScope(object state)
        {
            this.state = state;
            this.parent = CurrentScope.Value;
            CurrentScope.Value = this;
        }

        /// <summary>
        /// Gets the current scope.
        /// </summary>
        internal static LoggerScope? Current => CurrentScope.Value;

        /// <summary>
        /// Gets all scope states from the current scope up through parent scopes.
        /// </summary>
        /// <returns>Dictionary containing all scope key-value pairs.</returns>
        internal static Dictionary<string, object> GetScopeDictionary()
        {
            var scopes = new Dictionary<string, object>();
            var current = Current;

            while (current != null)
            {
                if (current.state is IEnumerable<KeyValuePair<string, object>> scopeItems)
                {
                    foreach (var item in scopeItems)
                    {
                        // Earlier scopes take precedence (don't overwrite)
                        if (!scopes.ContainsKey(item.Key))
                        {
                            scopes[item.Key] = item.Value;
                        }
                    }
                }
                else if (current.state != null)
                {
                    // Handle non-dictionary scope states
                    var key = current.state.GetType().Name;
                    if (!scopes.ContainsKey(key))
                    {
                        scopes[key] = current.state.ToString() ?? string.Empty;
                    }
                }

                current = current.parent;
            }

            return scopes;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CurrentScope.Value = this.parent;
        }
    }
}