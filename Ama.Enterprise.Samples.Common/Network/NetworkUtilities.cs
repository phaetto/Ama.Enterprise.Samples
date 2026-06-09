namespace Ama.Enterprise.Samples.Common.Network;

using System.Linq;
using System.Net.NetworkInformation;

/// <summary>
/// Provides common networking utilities for the sample applications.
/// </summary>
public static class NetworkUtilities
{
    /// <summary>
    /// Evaluates the active TCP and UDP listeners and returns the next available port starting from the provided baseline.
    /// </summary>
    /// <param name="startingPort">The port number to begin searching from.</param>
    /// <returns>The next available port number.</returns>
    public static int GetNextAvailablePort(int startingPort)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        
        var activeTcpPorts = ipGlobalProperties.GetActiveTcpListeners().Select(l => l.Port);
        var activeUdpPorts = ipGlobalProperties.GetActiveUdpListeners().Select(l => l.Port);

        var activePorts = activeTcpPorts.Concat(activeUdpPorts).ToHashSet();

        var port = startingPort;
        while (activePorts.Contains(port))
        {
            port++;
        }

        return port;
    }
}