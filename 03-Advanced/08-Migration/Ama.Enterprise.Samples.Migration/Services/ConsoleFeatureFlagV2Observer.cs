namespace Ama.Enterprise.Samples.Migration.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.Migration.Models;

/// <summary>
/// Observes state changes on V2 feature flag documents and triggers a console UI refresh.
/// </summary>
public sealed class ConsoleFeatureFlagV2Observer : IDocumentStateObserver<FeatureFlagStateV2>
{
    private readonly InteractiveConsoleApp app;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleFeatureFlagV2Observer"/> class.
    /// </summary>
    /// <param name="app">The interactive console application.</param>
    public ConsoleFeatureFlagV2Observer(InteractiveConsoleApp app)
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
}