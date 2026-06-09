namespace Ama.Enterprise.Samples.Migration;

/// <summary>
/// Constants used across the Live CRDT Schema Migration showcase.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The P2P mesh network identifier for Legacy V1 nodes.
    /// </summary>
    public const string V1MeshName = "feature-flags-v1-mesh";

    /// <summary>
    /// The P2P mesh network identifier for Modern V2 nodes.
    /// </summary>
    public const string V2MeshName = "feature-flags-v2-mesh";

    /// <summary>
    /// The UDP multicast discovery port for the Legacy V1 mesh.
    /// </summary>
    public const int V1MulticastPort = 8035;

    /// <summary>
    /// The UDP multicast discovery port for the Modern V2 mesh.
    /// </summary>
    public const int V2MulticastPort = 8036;

    /// <summary>
    /// The shared multicast address used for local peer discovery.
    /// </summary>
    public const string MulticastAddress = "239.255.0.1";

    /// <summary>
    /// The loopback IP address used for local TCP transport binding.
    /// </summary>
    public const string LoopbackHost = "127.0.0.1";

    /// <summary>
    /// The document type alias registered for the breaking V2 feature flag state.
    /// </summary>
    public const string FeatureFlagV2DocumentType = "enterprise-feature-flags-v2";

    /// <summary>
    /// The serializable payload type alias for a V2 feature flag rule.
    /// </summary>
    public const string FeatureFlagV2PayloadType = "enterprise-feature-flag-rule-v2";

    /// <summary>
    /// The unique identifier for the live V1-to-V2 migration pipeline.
    /// </summary>
    public const string MigrationId = "feature-flags-v1-to-v2";
}