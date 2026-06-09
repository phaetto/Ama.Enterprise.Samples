# HTTP/DNS Feature Flags Sample (SaaS Backend Integration)

This sample shows how to embed a decentralized feature flag cluster directly into an ASP.NET Core Minimal API using HTTP transport and DNS peer discovery.

## Solving the Cloud Networking Challenge

In cloud and container environments (like Kubernetes, AWS ECS, Azure Container Apps, or Docker Compose), UDP broadcast and multicast traffic are usually blocked.

To build a masterless P2P cluster in the cloud, this sample:
1. Uses standard HTTP: Sends P2P gossip and CRDT sync messages over regular HTTP/HTTPS requests that work seamlessly with cloud networks and proxies.
2. Discovers peers via DNS: Queries standard DNS `A` and `AAAA` records (such as a Kubernetes Headless Service) to find the IP addresses of all running instances.

## Key Features in This Sample

- ASP.NET Core integrated hosting (`AspNetCoreHostingMode.Integrated`): Shares Kestrel's HTTP pipeline so P2P mesh traffic and your public REST API run on the same port.
- DNS peer discovery (`.AddDnsPeerDiscovery`): Automatically discovers sibling pods or containers by resolving a configured hostname.
- Zero-trust security: Authenticates peers with an X.509 certificate (`cluster.cer`) and encrypts all P2P traffic using AES-GCM (`encryption.key`).
- Native AOT ready: Uses `WebApplication.CreateSlimBuilder(args)` and source-generated `System.Text.Json` (`AppJsonSerializerContext`) for fast startup, low memory usage, and zero reflection.

---

## Code Walkthrough

### 1. Configuring the HTTP + DNS Mesh

Instead of raw TCP sockets and UDP multicast, `Program.cs` wires the P2P mesh into ASP.NET Core and DNS:

```csharp
builder.Services.AddP2pMesh("feature-flags-http-mesh")
    .AddGossipNetwork(options =>
    {
        options.GossipInterval = TimeSpan.FromMilliseconds(1500);
        options.Fanout = 3;
        options.DefaultTimeToLive = 3;
    })
    // Share Kestrel's HTTP server for node-to-node transport
    .AddAspNetCoreTransport(options =>
    {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.AdvertisedHost = advertisedHost;
        options.AdvertisedPort = advertisedPort;
        options.UseHttps = useHttps;
    })
    // Handle peer handshakes over HTTP
    .AddAspNetCorePeerHandshake(options =>
    {
        options.HostingMode = AspNetCoreHostingMode.Integrated;
        options.UseHttps = useHttps;
    })
    // Discover peer IPs via DNS (A/AAAA records)
    .AddDnsPeerDiscovery(options =>
    {
        options.Hostname = discoveryHostname; // e.g., "my-headless-service.default.svc.cluster.local"
        options.TargetPort = discoveryPort;
    })
    // Authenticate nodes and encrypt payloads
    .AddCertificateAuthenticator(options =>
    {
        options.LocalCertificateBytes = certBytes;
        options.AllowedThumbprints.Add(clusterCert.Thumbprint);
        options.ValidateCertificateChain = false;
    })
    .AddWireEncoder(options =>
    {
        options.IsEncryptionEnabled = true;
        options.EncryptionKeyBase64 = encryptionKey;
    });
```

### 2. Mapping P2P Endpoints in ASP.NET Core

Two extension methods add the internal P2P gossip and handshake routes to your web application:

```csharp
app.MapP2pMeshEndpoints();
app.MapP2pMeshHandshakes();
```

### 3. Reading and Updating Flags in Minimal APIs

Inside your API endpoints, use `DistributedCrdtScopeManager` to resolve `IFeatureFlagClusterManager` for the current node's replica:

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

## Running the Sample

### Local Development

1. Open a terminal in the `02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags` folder.
2. Start the web API:
   ```bash
   dotnet run
   ```
3. Open `Ama.Enterprise.Samples.HttpDnsFeatureFlags.http` in Visual Studio or VS Code (with the REST Client extension) to test the endpoints:
   - `GET /weatherforecast` — Sample business endpoint.
   - `GET /flags` — Lists all synchronized feature flags.
   - `POST /flags/{name}?isEnabled=true` — Creates or updates a feature flag.
   - `DELETE /flags/{name}` — Removes a feature flag.

### Deploying to Kubernetes or Docker Compose

By default, the sample looks up `localhost:5290`. When deploying to Kubernetes or Docker Compose:
1. Create a Headless Service (`clusterIP: None`) for your deployment.
2. Set `DiscoveryHostname` (via environment variables or `appsettings.json`) to your headless service DNS name.
3. Each pod will periodically resolve the DNS name, find the IP addresses of all other pods, and sync feature flags directly over HTTP.

---
[⬅ Back to SaaS Backend Scenarios](../README.md)