namespace Ama.Enterprise.Samples.ZeroTrustRouting;

/// <summary>
/// Global constants for the Session Management and Zero-Trust Topology showcase.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The shared P2P mesh identifier used by all nodes on the local network.
    /// </summary>
    public const string MeshName = "session-feature-flags-mesh";

    /// <summary>
    /// The UDP multicast address used for local peer discovery.
    /// </summary>
    public const string MulticastAddress = "239.255.0.1";

    /// <summary>
    /// The UDP multicast port used for local peer discovery.
    /// </summary>
    public const int MulticastPort = 8035;

    /// <summary>
    /// The default starting TCP port for nodes when none is specified.
    /// </summary>
    public const int DefaultStartingPort = 8100;

    /// <summary>
    /// Session claim key representing the node's role.
    /// </summary>
    public const string RoleClaimKey = "role";

    /// <summary>
    /// Session claim key representing the node's geographic region.
    /// </summary>
    public const string RegionClaimKey = "region";

    /// <summary>
    /// Server role that can route and store both admin and user data within permitted region rules.
    /// </summary>
    public const string ServerRole = "server";

    /// <summary>
    /// Default role used when none is specified via CLI arguments.
    /// </summary>
    public const string DefaultRole = "user";

    /// <summary>
    /// Default geographic region used when none is specified via CLI arguments.
    /// </summary>
    public const string DefaultRegion = "EU";
}