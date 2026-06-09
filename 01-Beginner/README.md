# 01 - Beginner: Introduction to Distributed CRDTs with Feature Flags

Welcome to the first sample in the `Ama.Enterprise` repository! This tutorial walks you through building your first decentralized application in .NET.

To keep things simple, this sample uses the Feature Flags abstraction (`Ama.Enterprise.FeatureFlags`). It hides the math behind Conflict-free Replicated Data Types (CRDTs) so you can focus on the core concepts: setting up a Peer-to-Peer (P2P) network and watching state sync automatically across multiple nodes.

> Note: This beginner sample keeps data in memory only. It does not save state to a database or disk. If you stop all running nodes, the feature flags will reset.

## What You Will Learn
- How to enable the free Community license.
- How to configure a local CRDT replica and synchronization rules.
- How to set up a local P2P mesh network using TCP (for data transfer) and UDP (for automatic peer discovery).
- How to read, write, and react to feature flag changes across the cluster.

---

## Step-by-Step Walkthrough

### 1. Licensing
Before using `Ama.Enterprise` packages, register the license in your Dependency Injection (DI) container. For local development and eligible projects, use the Community license:

```csharp
// 1. Configure Open Source / Community Licensing.
services.ConfigureAmaCommunityLicense();
```

### 2. Configuring the CRDT Engine
Each node in the cluster needs a unique name (`replicaId`) so the CRDT engine can track changes cleanly. You also configure how often nodes sync with each other:
- `ActiveSyncEnabled`: Broadcasts changes to other nodes immediately when a flag is updated.
- `AntiEntropyIntervalSeconds`: Periodically checks with peers in the background to catch up on any missed updates.

```csharp
// 2. Register the CRDT replica with a unique ID and configure synchronization settings.
services.AddDistributedCrdtReplica(replicaId);

services.AddDistributedCrdtCore(options =>
{
    options.ActiveSyncEnabled = true;
    options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
    options.AntiEntropyIntervalSeconds = 5;
    options.AntiEntropyInitialDelaySeconds = 3;
});
```

### 3. Adding Feature Flags and Connecting the Mesh
Next, connect your CRDT replica to a named P2P mesh network and register the Feature Flags services.

To update the console screen automatically whenever another node changes a flag, we also register `ConsoleFeatureFlagObserver` as an `IFeatureFlagStateObserver`.

```csharp
// 3. Link the P2P mesh network to the CRDT replica.
services.AddDistributedCrdtP2p("feature-flags-internal-mesh", replicaId);

// 4. Add the Feature Flags services and register the console state observer via DI.
services.AddSingleton<CancellationTokenSource>();
services.AddSingleton<InteractiveConsoleApp>();
services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
services.AddFeatureFlags();
```

### 4. Configuring the P2P Network (TCP + UDP)
Now we tell the P2P mesh how to find other nodes and how to send messages:

- Gossip Network: Spreads updates across the cluster by forwarding messages to a few random peers (`Fanout`).
- TCP Transport: Opens a reliable local TCP port to send and receive CRDT state updates.
- UDP Peer Discovery: Broadcasts a lightweight "hello" packet over UDP multicast (`239.255.0.1`) so nodes on the same network find each other automatically without a central server.
- UDP Peer Handshake: Handles the initial greeting between two nodes before they start syncing over TCP.

```csharp
// 5. Configure the Mesh Network to use TCP transport and UDP discovery.
services.AddP2pMesh("feature-flags-internal-mesh")
    .AddGossipNetwork(options =>
    {
        options.GossipInterval = TimeSpan.FromMilliseconds(1500);
        options.Fanout = 3;
        options.DefaultTimeToLive = 3;
    })
    .AddTcpTransport(options =>
    {
        options.ListenPort = currentPort;
        options.ListenHost = "127.0.0.1";
    })
    .AddUdpPeerDiscovery(options =>
    {
        options.MulticastAddress = "239.255.0.1";
        options.MulticastPort = 8035;
        options.DiscoveryInterval = TimeSpan.FromSeconds(1);
        options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
    })
    .AddUdpPeerHandshake(options =>
    {
        options.ListenPort = handshakePort;
    });
```

### 5. Reading, Writing, and Observing State
Once the app starts, you use `IFeatureFlagClusterManager` to read and change flags. Because `Ama.Enterprise` supports multiple isolated replicas in the same process, you resolve the manager from the replica's scope:

```csharp
var scopeManager = provider.GetRequiredService<DistributedCrdtScopeManager>();
var crdtScope = scopeManager.GetOrCreateScope(replicaId);
var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();
```

#### Reacting to Changes
Whenever a flag changes (locally or from a remote peer), `Ama.Enterprise` calls `OnFeatureFlagsUpdatedAsync` on any registered `IFeatureFlagStateObserver`:

```csharp
public sealed class ConsoleFeatureFlagObserver : IFeatureFlagStateObserver
{
    private readonly InteractiveConsoleApp app;

    public ConsoleFeatureFlagObserver(InteractiveConsoleApp app)
    {
        this.app = app;
    }

    public Task OnFeatureFlagsUpdatedAsync(CancellationToken cancellationToken = default)
    {
        app.RequestRedraw();
        return Task.CompletedTask;
    }
}
```

#### Getting and Updating Flags
```csharp
// Get all flags currently in the cluster
var flags = await clusterManager.GetFlagsAsync();

// Turn a flag on or off (automatically syncs to all peers)
await clusterManager.SetFlagAsync("MyFeature", true, cancellationToken: token);

// Delete a flag across the cluster
await clusterManager.RemoveFlagAsync("MyFeature", token);
```

---

## How to Run the Sample

To see the P2P sync in action, run two or more nodes at the same time.

1. Open a terminal at the root of the repository.
2. Start the first node:
   ```bash
   dotnet run --project 01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags
   ```
3. Once the console menu appears, type `clone` and press Enter. This opens a new terminal window running a second node on the next available port. You can type `clone` as many times as you like!

### Console Commands
Type any of these commands into any node's terminal:
- `clone`: Starts a new node in a separate window that automatically joins the mesh.
- `set <name> <true|false>`: Creates or updates a flag (for example: `set DarkMode true`). Watch the other terminal windows update right away!
- `del <name>`: Deletes a flag across all nodes (for example: `del DarkMode`).
- `help`: Prints the list of commands.
- `exit`: Stops the current node cleanly.

---
[⬅ Back to Main Documentation](../README.md)