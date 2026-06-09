namespace Ama.Enterprise.Samples.Common.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// A provider for the <see cref="LockedConsoleLogger"/>.
/// </summary>
public sealed class LockedConsoleLoggerProvider : ILoggerProvider
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);
        return new LockedConsoleLogger(categoryName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }
}