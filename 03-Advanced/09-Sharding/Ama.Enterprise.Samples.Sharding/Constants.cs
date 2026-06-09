namespace Ama.Enterprise.Samples.Sharding;

/// <summary>
/// Global constants for the Horizontal CRDT Sharding sample.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The P2P mesh network name shared by all sharded nodes.
    /// </summary>
    public const string MeshName = "sharding-internal-mesh";

    /// <summary>
    /// The UDP multicast address used for automatic LAN peer discovery.
    /// </summary>
    public const string MulticastAddress = "239.255.0.1";

    /// <summary>
    /// The UDP multicast port used for automatic LAN peer discovery.
    /// </summary>
    public const int MulticastPort = 8045;

    /// <summary>
    /// The default starting TCP port for the first node.
    /// </summary>
    public const int DefaultListenPort = 8190;

    /// <summary>
    /// The default shard replication factor across the consistent hash ring.
    /// </summary>
    public const int DefaultReplicationFactor = 1;

    /// <summary>
    /// The document type alias for tenant feature flag documents.
    /// </summary>
    public const string TenantFlagsDocType = "tenant-flags";

    /// <summary>
    /// The shard ID prefix assigned to tenant documents.
    /// </summary>
    public const string TenantShardPrefix = "tenant-shard-";
}