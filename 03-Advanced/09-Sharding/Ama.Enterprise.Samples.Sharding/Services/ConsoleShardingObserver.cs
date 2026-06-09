namespace Ama.Enterprise.Samples.Sharding.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.Sharding.Models;

/// <summary>
/// Observer that triggers a console UI redraw whenever a sharded <see cref="TenantFeatureFlagState"/> document
/// mutates or when the P2P consistent hash ring topology rebalances shards across nodes.
/// </summary>
public sealed class ConsoleShardingObserver : IDocumentStateObserver<TenantFeatureFlagState>, ICrdtOrchestratorObserver
{
    private readonly InteractiveConsoleApp app;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleShardingObserver"/> class.
    /// </summary>
    /// <param name="app">The interactive console application instance.</param>
    public ConsoleShardingObserver(InteractiveConsoleApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        this.app = app;
    }

    /// <inheritdoc />
    public Task OnStateChangedAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        app.RequestRedraw();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnTopologyChangedAsync(CancellationToken cancellationToken = default)
    {
        app.RequestRedraw();
        return Task.CompletedTask;
    }
}