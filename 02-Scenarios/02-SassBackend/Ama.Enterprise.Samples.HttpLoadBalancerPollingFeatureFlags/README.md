# HTTP Load Balancer Polling Feature Flags Sample (SaaS Backend Integration)

This sample shows how to run a highly available, decentralized feature flag cluster in Azure App Service (or similar PaaS environments) using HTTP transport, HTTP load balancer polling discovery, and Azure Table Storage.

## Solving the Managed PaaS Challenge

In managed cloud platforms like Azure App Service, instances cannot use UDP multicast, and you typically do not have a custom internal DNS server listing every scaled-out instance.

To form a P2P mesh automatically when you scale out your web app, this sample uses two techniques:
1. Discovery via the load balancer: Every node periodically sends a discovery request to the app's main load balancer URL (`TargetHost`). Because the load balancer distributes traffic across all healthy instances, nodes quickly meet all of their peers and learn their private VNet IP addresses.
2. Direct VNet sync: Once two nodes discover each other, they bypass the public load balancer and sync CRDT updates directly over Azure's private Virtual Network (`WEBSITE_PRIVATE_IP` and `WEBSITE_PRIVATE_PORTS`).

## Key Features in This Sample

- HTTP polling peer discovery (`.AddAspNetCorePeerDiscovery`): Finds sibling instances through a standard HTTP load balancer without needing Redis, etcd, or ZooKeeper.
- Durable storage with Azure Table Storage (`.AddDistributedCrdtTableStorage`): Persists CRDT checkpoints and journals to Azure Table Storage (`FeatureFlagsDistributedCrdtStorage`) so state survives full cluster restarts.
- High-speed MessagePack serialization (`.AddCrdtMessagePack`): Uses AOT-friendly, source-generated MessagePack resolvers for compact binary storage and network payloads.
- Zero-trust security: Protects all P2P traffic with X.509 certificate authentication (`cluster.cer`) and AES-GCM wire encryption (`encryption.key`).
- Native AOT ready: Built with `WebApplication.CreateSlimBuilder(args)` and source-generated `System.Text.Json`.

---

## Code Walkthrough

### 1. Configuring Table Storage, Mesh, and MessagePack

In `Program.cs`, the node configures durable Table Storage, sets up HTTP transport on its private VNet port, and points peer discovery at the load balancer:

```csharp
// Persist CRDT state and journals to Azure Table Storage
builder.Services.AddDistributedCrdtTableStorage(options =>
{
    options.TableName = "FeatureFlagsDistributedCrdtStorage";
    options.ConnectionString = dataStorageConnectionString;
    options.UseBinarySerialization = true;
});

builder.Services.AddP2pMesh("feature-flags-http-mesh")
    .AddGossipNetwork(options =>
    {
        options.GossipInterval = TimeSpan.FromMilliseconds(1500);
        options.Fanout = 3;
        options.DefaultTimeToLive = 3;
    })
    // Listen on the private VNet IP/Port for direct node-to-node sync
    .AddAspNetCoreTransport(options =>
    {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.AdvertisedHost = advertisedHost; // Private VNet IP
        options.AdvertisedPort = privatePort;    // Private port (WEBSITE_PRIVATE_PORTS)
        options.UseHttps = false;
        options.StandaloneListenPort = privatePort;
    })
    // Poll the central Load Balancer to discover other instances
    .AddAspNetCorePeerDiscovery(options =>
    {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.TargetHost = targetHost; // Load Balancer hostname
        options.TargetPort = targetPort;
        options.TargetUseHttps = useHttps;
    })
    .AddCertificateAuthenticator(options => { /* X.509 cert config */ })
    .AddWireEncoder(options => { /* AES-GCM encryption config */ });

// Use source-generated MessagePack for fast binary serialization
builder.Services.AddCrdtMessagePack(
    Ama_Enterprise_CRDT_MessagePack_MessagePackResolver.Instance,
    Ama_Enterprise_Samples_HttpLoadBalancerPollingFeatureFlags_MessagePackResolver.Instance
);
```

### 2. Mapping P2P and Discovery Endpoints

Along with the standard P2P transport and handshake endpoints, call `MapP2pMeshDiscovery()` so the node can respond to discovery polls coming through the load balancer:

```csharp
app.MapP2pMeshEndpoints();
app.MapP2pMeshHandshakes();
app.MapP2pMeshDiscovery(); // Responds to HTTP Load Balancer polling requests
```

### 3. Minimal API Endpoints

The REST endpoints use `DistributedCrdtScopeManager` to read, create, and delete flags asynchronously:

```csharp
app.MapGet("/flags", async (DistributedCrdtScopeManager scopeManager) =>
{
    var crdtScope = scopeManager.GetOrCreateScope(replicaId);
    var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

    var flags = (await clusterManager.GetFlagsAsync())
        .Select(f => new FlagModel(f.Key, f.Value.IsEnabled))
        .ToArray();

    return TypedResults.Ok(flags);
});
```

---

## Running Locally

### Prerequisites
- .NET 10 SDK
- Azurite (Azure Storage Emulator): Make sure Azurite is running locally (for example, via Visual Studio or `npx azurite`), as the sample defaults to `UseDevelopmentStorage=true` in Development mode.

### Start Two Local Nodes

1. Open a terminal in `02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags`.
2. Start Node 1 (listening on port `5001` and polling Node 2 on `5002`):
   ```bash
   dotnet run -- --AdvertisedPort 5001 --WEBSITE_PRIVATE_PORTS 5001 --TargetPort 5002 --ReplicaId node-1
   ```
3. Open a second terminal and start Node 2 (listening on port `5002` and polling Node 1 on `5001`):
   ```bash
   dotnet run -- --AdvertisedPort 5002 --WEBSITE_PRIVATE_PORTS 5002 --TargetPort 5001 --ReplicaId node-2
   ```
4. Use `Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.http` to create, view, or delete feature flags and watch them sync between both nodes and Azure Table Storage.

---

## Deploying to Azure App Service

This project includes ready-to-use Infrastructure-as-Code and CI/CD automation:
- Bicep template (`Infrastructure/main.bicep`): Provisions the Azure App Service Plan, Web App, Virtual Network (`10.0.0.0/16`) with `vnetPrivatePortsCount: 1`, and the `FeatureFlagsDistributedCrdtStorage` Azure Storage Table.
- GitHub Actions workflow (`.github/workflows/deploy-http-polling-feature-flags.yml`): A manual `workflow_dispatch` pipeline that deploys the Bicep infrastructure, builds the app, and publishes it to Azure.

---
[⬅ Back to SaaS Backend Scenarios](../README.md)