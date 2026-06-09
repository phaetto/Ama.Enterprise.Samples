# 01 - Beginner: Introduction to Distributed CRDTs with Feature Flags

Welcome to the first sample in the Ama.Enterprise repository! This sample serves as your introduction to building decentralized applications using our framework. 

To keep things simple, we are using the **Feature Flags** abstraction. This hides the underlying complexity of Conflict-free Replicated Data Types (CRDTs), allowing you to focus on the core concepts: setting up a Peer-to-Peer (P2P) network and observing state synchronization across a cluster. For the same reason, this sample uses strictly in-memory storage. The cluster does not persist any changes to the filesystem or a database. When all nodes are shut down, the feature flags state will be completely reset.

## What You Will Learn
- Bootstrapping the application and applying the open-source license.
- Configuring a decentralized CRDT feature flag service.
- Setting up a robust P2P Mesh Network using TCP (for transport) and UDP (for discovery).
- Reading, writing, and listening to distributed state changes.

---

## Step-by-Step Walkthrough

### 1. Bootstrapping & Licensing
Before using Ama.Enterprise packages, you must apply the license. For this sample, we configure the Open Source license early in the dependency injection setup.

```csharp
// 1. Configure Open Source Licensing
services.ConfigureAmaOpenSourceLicense();
```

### 2. Adding Distributed Feature Flags
Instead of building a CRDT from scratch, we inject the Feature Flags service. We provide a unique `replicaId` for the node and configure the synchronization rules:
- **ActiveSyncEnabled**: Ensures real-time replication whenever a local change is made.
- **AntiEntropyIntervalSeconds**: Periodically compares state with peers to ensure consistency across nodes that might have missed a transient message.

```csharp
// 2. Add Feature Flags Services with Distributed CRDT Options
services.AddFeatureFlags(replicaId, options =>
{
    options.Crdt.ActiveSyncEnabled = true;
    options.Crdt.CheckpointIntervalSeconds = 3600;
    options.Crdt.AntiEntropyIntervalSeconds = 1;
});
```

### 3. Configuring the Mesh Network
This is the heart of the decentralized application. The P2P network needs to know how to discover peers and how to transport data. We configure multiple layers:

- **Gossip Network**: Controls how messages fan out to the cluster.
- **TCP Transport**: The reliable channel for sending state updates directly between nodes.
- **UDP Peer Discovery**: Uses multicast (`239.255.0.1`) so nodes automatically find each other on the local network without a central server or static IP configurations.
- **UDP Peer Handshake**: A dedicated port for establishing the initial connection before upgrading to the reliable TCP transport layer.

```csharp
// 3. Configure Mesh Network to support the internal Cluster
services.AddP2pMesh("feature-flags-internal-mesh")
        .AddGossipNetwork(options => { /* Fanout and TTL config */ })
        .AddTcpTransport(options => { /* ListenPort config */ })
        .AddUdpPeerDiscovery(options => { /* Multicast address & interval config */ })
        .AddUdpPeerHandshake(options => { /* Handshake port config */ });
```

### 4. Interacting with the Cluster
Once the services are running, you interact with the state using the `IFeatureFlagClusterManager`. Because Ama.Enterprise supports multi-tenancy, we extract the specific scope for our replica ID.

**Listening to State Changes:**
The cluster manager provides an event that fires whenever the local state is updated by a remote peer.
```csharp
clusterManager.StateChanged += (sender, eventArgs) => {
    // Redraw UI when a remote peer changes a flag
    app.RequestRedraw(); 
};
```

**Getting and Setting Flags:**
```csharp
// Get all flags across the cluster
var flags = clusterManager.GetFlags();

// Add or update a flag (automatically broadcasts to peers)
await clusterManager.SetFlagAsync("MyFeature", true, cancellationToken);

// Remove a flag across the cluster
await clusterManager.RemoveFlagAsync("MyFeature", cancellationToken);
```

---

## How to Run the Sample

To see the distributed mesh in action, you need to run multiple instances of this application. Thanks to the built-in console engine, spinning up a local cluster is incredibly easy.

1. Open your terminal and navigate to the repository root.
2. Start the primary node:
   ```bash
   dotnet run --project 01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags
   ```
3. Once the application is running, type **`clone`** in the console and hit Enter. This will automatically spawn a new instance of the application in a new terminal window on the next available port. You can do this multiple times to instantly build up your cluster!

### Available Commands
In any of the active terminals, you can interact with the app using the following commands:
- **`clone`**: Spawns a new node process to join the mesh network.
- **`set <flagName> <true|false>`**: Add or update a flag (e.g., `set BetaFeature true`). Watch as the other terminals instantly update to reflect the new state.
- **`del <flagName>`**: Removes a flag entirely (e.g., `del BetaFeature`). The flag will be removed across all connected nodes.
- **`help`**: Re-displays the command menu.
- **`exit`**: Gracefully shuts down the node.

Notice that there is no central database or server! The state is synchronized entirely through the P2P mesh network using CRDTs.

---
[⬅ Back to Main Documentation](../README.md)