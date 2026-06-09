namespace Ama.Enterprise.Samples.Common.Logging;

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ama.Enterprise.Samples.Common.UI;

/// <summary>
/// A logger that writes to the console using thread-safe locking.
/// </summary>
public sealed class LockedConsoleLogger : ILogger
{
    private readonly string categoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockedConsoleLogger"/> class.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    public LockedConsoleLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);
        this.categoryName = categoryName;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
        {
            return;
        }

        var shortCategory = categoryName.Split('.').LastOrDefault() ?? categoryName;
        var logLine = $"[{logLevel.ToString().ToUpperInvariant()}] {shortCategory}: {message}";
        
        if (exception is not null)
        {
            logLine += Environment.NewLine + exception;
        }

        ConsoleHelper.WriteLineLocked(logLine);
    }
}