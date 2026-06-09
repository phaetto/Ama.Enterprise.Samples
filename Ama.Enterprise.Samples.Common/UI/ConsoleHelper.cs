namespace Ama.Enterprise.Samples.Common.UI;

using System;

/// <summary>
/// Provides thread-safe console operations.
/// </summary>
public static class ConsoleHelper
{
    private static readonly object ConsoleLock = new();

    /// <summary>
    /// Writes a line to the console ensuring thread-safe access.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteLineLocked(string message)
    {
        if (message is null)
        {
            return;
        }

        lock (ConsoleLock)
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Executes an action under the console lock to ensure thread safety for complex drawing.
    /// </summary>
    /// <param name="action">The drawing action to execute.</param>
    public static void LockConsoleAndDraw(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (ConsoleLock)
        {
            action();
        }
    }
}