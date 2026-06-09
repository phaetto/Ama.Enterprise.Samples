namespace Ama.Enterprise.Samples.TcpUdpFeatureFlags.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Samples.Common.UI;

/// <summary>
/// Observer that notifies the interactive console UI to redraw when replicated feature flags are updated.
/// </summary>
public sealed class ConsoleFeatureFlagObserver : IFeatureFlagStateObserver
{
    private InteractiveConsoleApp? app;

    /// <summary>
    /// Attaches the interactive console application instance to trigger redraws on state updates.
    /// </summary>
    /// <param name="consoleApp">The interactive console application instance.</param>
    public void AttachApp(InteractiveConsoleApp consoleApp)
    {
        ArgumentNullException.ThrowIfNull(consoleApp);
        app = consoleApp;
    }

    /// <inheritdoc />
    public Task OnFeatureFlagsUpdatedAsync(CancellationToken cancellationToken = default)
    {
        app?.RequestRedraw();
        return Task.CompletedTask;
    }
}