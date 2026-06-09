namespace Ama.Enterprise.Samples.CustomCrdt.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Observer that notifies the <see cref="InteractiveConsoleApp"/> to redraw whenever a <see cref="ChatMessageState"/> document changes.
/// </summary>
public sealed class ConsoleChatStateObserver : IDocumentStateObserver<ChatMessageState>
{
    private readonly InteractiveConsoleApp app;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleChatStateObserver"/> class.
    /// </summary>
    /// <param name="app">The interactive console application instance.</param>
    public ConsoleChatStateObserver(InteractiveConsoleApp app)
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