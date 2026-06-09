# HTTP Load Balancer Polling Feature Flags Sample (SaaS Backend Integration)

This sample demonstrates how to build a highly available, decentralized feature flag system using **HTTP Transports** and **HTTP Polling Peer Discovery**. It embeds the `Ama.Enterprise.FeatureFlags` and CRDT core engines directly into standard REST API endpoints, optimized for environments like Azure App Service.

## The Cloud Network Challenge

In cloud environments (like Azure App Service) where standard peer-to-peer protocols (like UDP Multicast) and even internal direct DNS resolution for instances might be restricted or unreliable, we must:
1. **Rely on standard protocols:** Replace raw TCP/UDP with standard HTTP transports that seamlessly traverse cloud load balancers and proxies.
2. **Use HTTP Polling discovery:** By pointing the P2P discovery mechanism to the service's own public or internal Load Balancer (or API Gateway), nodes continuously poll the endpoint. Because the Load Balancer routes traffic randomly (or round-robin) to the backend instances, nodes eventually discover all other active instances and their internal IPs to form a direct mesh.

## Architecture Highlights

This sample explicitly defines a Web Application that achieves the following:

- **ASP.NET Core Integrated Hosting:** Uses `AspNetCoreHostingMode.Integrated` to share Kestrel's existing network pipeline. The P2P gossip traffic operates over standard HTTP.
- **HTTP Polling Discovery:** Solves the cloud peer discovery problem seamlessly without requiring external centralized registries (like Redis or etcd).
- **VNet P2P Connectivity:** Leverages Azure App Service `WEBSITE_PRIVATE_PORTS` and VNet integration. Once discovered via the load balancer, nodes route CRDT synchronization traffic securely over direct private links.
- **CRDT Scoped Resolvers:** Leverages `DistributedCrdtScopeManager` to dynamically resolve isolated bounds (`IFeatureFlagClusterManager`) per replica instance safely inside ASP.NET Core Minimal APIs.
- **Native AOT & Zero Trust:** Fully compatible with `CreateSlimBuilder`, featuring strict wire encryption via AES-GCM and Node Authentication via X.509 Certificates.

## Code Breakdown

### 1. Mesh Configuration

Instead of UDP multicast, the cluster utilizes native ASP.NET extensions to wire up HTTP handlers and load balancer polling for peer discovery.

```csharp
builder.Services.AddP2pMesh("feature-flags-http-mesh")
    // Share Kestrel's HTTP Server for node-to-node transport over VNet
    .AddAspNetCoreTransport(options => {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.AdvertisedHost = advertisedHost; // e.g., VNet internal IP
        options.AdvertisedPort = privatePort;    // e.g., Azure Private Port
        options.StandaloneListenPort = privatePort;
    })
    // Discover peers by sending handshakes through the central Load Balancer
    .AddAspNetCorePeerDiscovery(options => {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.TargetHost = targetHost; // The public/internal Load Balancer URL
        options.TargetPort = targetPort;
    });
```

### 2. ASP.NET Pipeline Integration

You only need three map extensions in the ASP.NET pipeline to configure standard HTTP routing for internal node gossip, clustering messages, and the crucial HTTP polling discovery endpoint:

```csharp
// Map ASP.NET Core Integrated P2P routing seamlessly
app.MapP2pMeshEndpoints();
app.MapP2pMeshHandshakes();
app.MapP2pMeshDiscovery(); // Essential for HTTP Load Balancer Polling
```

### 3. Dependency Injection in Minimal APIs

Because CRDT scopes are topologically bounded to unique replicas (to prevent data corruption in multi-tenant environments), we do not inject `IFeatureFlagClusterManager` globally. Instead, we use the `DistributedCrdtScopeManager`:

```csharp
app.MapGet("/flags", (DistributedCrdtScopeManager scopeManager) =>
{
    var crdtScope = scopeManager.GetOrCreateScope(replicaId);
    var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

    // Retrieve eventually-consistent cluster flags
    var flags = clusterManager.GetFlags()
        .Select(f => new FlagModel(f.Key, f.Value.IsEnabled))
        .ToArray();

    return Results.Ok(flags);
});
```

## Infrastructure (Azure App Service)

The sample includes a Bicep template (`Infrastructure/main.bicep`) to easily provision the required Azure resources for this exact scenario:
* **Virtual Network:** Creates the `10.0.0.0/16` space and delegates a subnet to the App Service for secure, private node-to-node communication.
* **App Service Configuration:** Configured with `vnetRouteAllEnabled`, `vnetPrivatePortsCount: 1` (exposing a private communication port), and the necessary environment variables/application settings mapped (e.g., certificates, keys, connection strings).

## Running the Sample

### Development Mode

Ensure `cluster.cer` and `encryption.key` are present in the output directory, and Azurite (or a valid Table Storage Connection String) is available. You can start multiple instances on different ports and cross-reference them to simulate the load balancer routing:

1. Open your terminal in the `Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags` directory.
2. Run the first node:
   ```bash
   dotnet run --AdvertisedPort 5001 --TargetPort 5002
   ```
3. Run the second node (in a new terminal):
   ```bash
   dotnet run --AdvertisedPort 5002 --TargetPort 5001
   ```
4. Use the included `.http` file (`Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.http`) via Visual Studio 2022 or VS Code's REST Client to interact with the endpoints.

### Simulating a Cloud Mesh Environment

By default, when deployed to Azure App Service using the included Bicep template, the application will automatically read the `WEBSITE_PRIVATE_IP` and `WEBSITE_PRIVATE_PORTS` variables. It will periodically poll the main Azure App Service domain (which acts as a load balancer). As the load balancer distributes these polling requests across your scaled-out instances, they will discover each other's private VNet IPs and immediately establish direct, private P2P connections, bypassing the load balancer for all subsequent CRDT synchronization.

---
[⬅ Back to SaaS Backend Scenarios](../README.md)