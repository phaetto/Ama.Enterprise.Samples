# HTTP/DNS Feature Flags Sample (SaaS Backend Integration)

This sample demonstrates how to build a decentralized, cluster-aware ASP.NET Core application using **HTTP Transports** and **DNS Peer Discovery**. It embeds the `Ama.Enterprise.FeatureFlags` and CRDT core engines directly into standard REST API endpoints.

## The Cloud Network Challenge

In cloud environments (like Azure App Services, AWS Fargate, Google Cloud Run, or Kubernetes), underlying virtual networks heavily restrict or completely drop UDP multicast and broadcast traffic. 

To create a masterless peer-to-peer (P2P) cluster in these environments, we must:
1. **Rely on standard protocols:** Replace raw TCP/UDP with standard HTTP transports that seamlessly traverse cloud load balancers, proxies, and service meshes.
2. **Use reliable discovery:** Replace UDP broadcast discovery with DNS resolution (A/AAAA records).

## Architecture Highlights

This sample explicitly defines a Web Application that achieves the following:

- **ASP.NET Core Integrated Hosting:** Uses `AspNetCoreHostingMode.Integrated` to share Kestrel's existing network pipeline. The P2P gossip traffic operates over standard HTTP on the same ports as your business endpoints.
- **DNS Peer Discovery:** Resolves peers via standard DNS lookups. In a real-world SaaS deployment, providing the hostname of a Kubernetes headless service or an internal DNS zone will automatically discover all active pods/nodes in the mesh using their A/AAAA records.
- **CRDT Scoped Resolvers:** Leverages `DistributedCrdtScopeManager` to dynamically resolve isolated bounds (`IFeatureFlagClusterManager`) per replica instance safely inside ASP.NET Core Minimal APIs.

## Code Breakdown

### 1. Mesh Configuration

Instead of UDP multicast and raw TCP, the cluster utilizes native ASP.NET extensions to wire up HTTP handlers and DNS polling.

```csharp
builder.Services.AddP2pMesh("feature-flags-http-mesh")
    .AddGossipNetwork(...)
    // Share Kestrel's HTTP Server for node-to-node transport
    .AddAspNetCoreTransport(options => {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.AdvertisedHost = "localhost";
        options.AdvertisedPort = 5290;
    })
    // Integrate peer handshake verifications securely via HTTP
    .AddAspNetCorePeerHandshake(options => {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
    })
    // Discover peers via DNS resolution (A/AAAA) instead of UDP Multicast
    .AddDnsPeerDiscovery(options => {
        options.Hostname = "localhost"; // In K8s: "my-service.default.svc.cluster.local"
        options.TargetPort = 5290;
    });
```

### 2. ASP.NET Pipeline Integration

You only need two map extensions in the ASP.NET pipeline to configure standard HTTP routing for internal node gossip and clustering messages:

```csharp
// Map ASP.NET Core Integrated P2P Handshakes routing seamlessly
app.MapP2pMeshEndpoints();
app.MapP2pMeshHandshakes();
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

## Running the Sample

### Development Mode

1. Open your terminal in the `Ama.Enterprise.Samples.HttpDnsFeatureFlags` directory.
2. Run the application:
   ```bash
   dotnet run
   ```
3. Use the included `.http` file (`Ama.Enterprise.Samples.HttpDnsFeatureFlags.http`) via Visual Studio 2022 or VS Code's REST Client to interact with the endpoints:
   - `GET /weatherforecast`
   - `GET /flags`
   - `POST /flags/{name}?isEnabled=true`
   - `DELETE /flags/{name}`

### Simulating a Cloud Mesh Environment

By default, this sample binds and discovers on `localhost:5290`. If you deploy this to a containerized orchestrator like Docker Compose or Kubernetes, the DNS discovery configuration (`Hostname = "my-headless-service"`) will trigger the node to periodically query DNS A/AAAA records, finding the IP addresses of all sibling nodes and integrating them into the mesh automatically over HTTP!

---
[⬅ Back to SaaS Backend Scenarios](../README.md)