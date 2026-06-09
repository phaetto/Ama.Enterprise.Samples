namespace Ama.Enterprise.Samples.MultipleReplicas;

internal static class Constants
{
    public const string StagingMeshName = "staging-tenant-mesh";
    public const string ProductionMeshName = "prod-tenant-mesh";

    public const string StagingScopeKey = "staging";
    public const string ProductionScopeKey = "prod";

    public const string LoopbackHost = "127.0.0.1";
    public const string MulticastAddress = "239.255.0.1";

    public const int DefaultListenPort = 8100;
    public const int StagingMulticastPort = 8035;
    public const int ProductionMulticastPort = 8036;
}