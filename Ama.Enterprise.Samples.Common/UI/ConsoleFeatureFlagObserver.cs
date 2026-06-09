namespace Ama.Enterprise.Samples.Common.UI;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.FeatureFlags.Services;

/// <summary>
/// Observer that notifies the interactive console UI to redraw when replicated feature flags are updated.
/// </summary>
public sealed class ConsoleFeatureFlagObserver : IFeatureFlagStateObserver
{
    private readonly InteractiveConsoleApp app;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFeatureFlagObserver"/> class.
    /// </summary>
    /// <param name="app">The interactive console application instance.</param>
    public ConsoleFeatureFlagObserver(InteractiveConsoleApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        this.app = app;
    }

    /// <inheritdoc />
    public Task OnFeatureFlagsUpdatedAsync(CancellationToken cancellationToken = default)
    {
        app.RequestRedraw();
        return Task.CompletedTask;
    }
}