namespace Ama.Enterprise.Samples.Common.UI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ama.Enterprise.Samples.Common.Network;

/// <summary>
/// A reusable console application engine supporting custom commands, state rendering, and cloning.
/// </summary>
public sealed class InteractiveConsoleApp
{
    private readonly ILogger logger;
    private readonly CancellationTokenSource cts;
    private readonly Dictionary<string, Func<string[], CancellationToken, Task>> commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> commandHelp = [];
    private readonly string appTitle;
    
    private Action? drawStateAction;
    private volatile bool needsRedraw;
    private int currentPort;
    private string? introText;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveConsoleApp"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    /// <param name="appTitle">The title to display in the menu.</param>
    /// <param name="cts">The global cancellation token source.</param>
    public InteractiveConsoleApp(ILogger logger, string appTitle, CancellationTokenSource cts)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(appTitle);
        ArgumentNullException.ThrowIfNull(cts);

        this.logger = logger;
        this.appTitle = appTitle;
        this.cts = cts;
        
        RegisterCommand("clone", "Spawns a new node process", (_, _) => { CloneProcess(); return Task.CompletedTask; });
        RegisterCommand("exit", "Shuts down the node", (_, _) => { this.cts.Cancel(); return Task.CompletedTask; });
        RegisterCommand("help", "Displays this menu", (_, _) => { DrawMenu(); return Task.CompletedTask; });
    }

    /// <summary>
    /// Registers a custom command to the console engine.
    /// </summary>
    /// <param name="command">The command string to trigger the action.</param>
    /// <param name="description">The description displayed in the menu help.</param>
    /// <param name="handler">The asynchronous handler to execute.</param>
    public void RegisterCommand(string command, string description, Func<string[], CancellationToken, Task> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(command);
        ArgumentException.ThrowIfNullOrEmpty(description);
        ArgumentNullException.ThrowIfNull(handler);

        commands[command] = handler;
        commandHelp.Add($" {command,-40} - {description}");
    }

    /// <summary>
    /// Sets the action used to draw the live state payload.
    /// </summary>
    /// <param name="render">The render action.</param>
    public void SetStateRenderer(Action render)
    {
        ArgumentNullException.ThrowIfNull(render);
        drawStateAction = render;
    }

    /// <summary>
    /// Sets the introductory text describing the application setup.
    /// </summary>
    /// <param name="text">The intro text to display in the menu.</param>
    public void SetIntroText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        introText = text;
    }

    /// <summary>
    /// Flags the UI to request a redraw during the next tick.
    /// </summary>
    public void RequestRedraw()
    {
        needsRedraw = true;
    }

    /// <summary>
    /// Sets the base port required for clone port calculations and display.
    /// </summary>
    /// <param name="port">The active process port.</param>
    public void SetCurrentPort(int port)
    {
        currentPort = port;
    }

    /// <summary>
    /// Starts the interactive console loop. Blocks until cancellation is requested.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Background UI rendering task to decouple Console I/O from hot operations
        _ = Task.Run(async () =>
        {
            try
            {
                using var uiTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
                while (await uiTimer.WaitForNextTickAsync(cts.Token).ConfigureAwait(false))
                {
                    if (needsRedraw)
                    {
                        needsRedraw = false;
                        if (drawStateAction is not null)
                        {
                            ConsoleHelper.LockConsoleAndDraw(drawStateAction);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Clean shutdown
            }
        }, cts.Token);

        DrawMenu();

        if (drawStateAction is not null)
        {
            ConsoleHelper.LockConsoleAndDraw(drawStateAction);
        }

        while (!cts.Token.IsCancellationRequested)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();

            try
            {
                if (commands.TryGetValue(command, out var handler))
                {
                    await handler(parts, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked("Unknown command. Type 'help' for available commands.");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing command '{Command}'.", command);
            }
        }
    }

    private void DrawMenu()
    {
        ConsoleHelper.LockConsoleAndDraw(() =>
        {
            Console.WriteLine("=================================================");
            Console.WriteLine($" {appTitle} - Node Port: {currentPort}");
            Console.WriteLine("=================================================");
            if (!string.IsNullOrWhiteSpace(introText))
            {
                Console.WriteLine($" {introText}");
                Console.WriteLine("=================================================");
            }
            Console.WriteLine("Commands:");
            foreach (var helpText in commandHelp)
            {
                Console.WriteLine(helpText);
            }
            Console.WriteLine("=================================================\n");
        });
    }

    private void CloneProcess()
    {
        var nextPort = NetworkUtilities.GetNextAvailablePort(currentPort + 1);
        var processPath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(processPath))
        {
            logger.LogWarning("Unable to determine process path for cloning.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = nextPort.ToString(),
            UseShellExecute = true
        });

        logger.LogInformation("Cloned new cluster node on port {NextPort}.", nextPort);
    }
}