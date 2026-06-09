namespace Ama.Enterprise.Samples.Serialization;

/// <summary>
/// Constants used across the CRDT Serialization showcase.
/// </summary>
internal static class Constants
{
    public const string MeshName = "feature-flags-serialization-mesh";
    public const string MulticastAddress = "239.255.0.1";
    public const int MulticastPort = 8035;
    public const int DefaultListenPort = 8100;

    public const string ModeJson = "json";
    public const string ModeBrotli = "brotli";
    public const string ModeMessagePack = "msgpack";
}