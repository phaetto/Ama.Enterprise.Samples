# 03 - Multiple CRDT Replicas (Multi-Tenant / Multi-Environment Isolation)

This sample demonstrates how a single .NET process (such as a control plane, administration service, or multi-tenant gateway) can host multiple completely isolated CRDT replicas simultaneously (for example, `staging` and `prod` environments) by creating a dedicated `IServiceCollection` / `IHost` container for each replica.

In this showcase, every node hosts two logical replicas inside the same OS process:
- **Staging Replica** (`staging-node-{port}`) hosted in its own `IHost` container (`staging-tenant-mesh` on UDP multicast port `8035`)
- **Production Replica** (`prod-node-{port}`) hosted in its own `IHost` container (`prod-tenant-mesh` on UDP multicast port `8036`)

> **Important Architectural Note — Why Separate `IServiceCollection` Containers Are Required:**
> The P2P Gossip algorithm and CRDT synchronization pipeline are global to an `IServiceCollection`. Registering two replicas or two meshes inside the **same** `IServiceCollection` automatically connects and bridges them (which is the exact pattern used in [02 - Multiple Meshes](../02-MultipleMeshes/README.md) to build a cross-mesh bridge).
>
> To achieve true isolation between multiple replicas hosted in the same process, you must either:
> 1. **Create a separate `IServiceCollection` / `IHost` per isolated replica** (demonstrated in this sample).
> 2. **Partition a single shared P2P mesh** using session claims, `IMeshRoutingPolicy`, and `IScopeTopologyProvider` (demonstrated in [05 - Zero-Trust Routing](../05-ZeroTrustRouting/README.md)).

---

## Architecture Overview

```text
+-------------------------------------------------------------------------+
| Node 1 Process (Base Port 8100)                                         |
|                                                                         |
|  +---------------------------------+  +------------------------------+  |
|  | Staging IHost Container         |  | Production IHost Container   |  |
|  | Replica: staging-node-8100      |  | Replica: prod-node-8101      |  |
|  | Mesh: "staging-tenant-mesh"     |  | Mesh: "prod-tenant-mesh"     |  |
|  | TCP: 8100 | UDP Multicast: 8035 |  | TCP: 8101 | UDP Multicast:8036|  |
|  +----------------+----------------+  +--------------+---------------+  |
+-------------------|----------------------------------|------------------+
                    |                                  |
         Staging TCP/UDP Sync               Production TCP/UDP Sync
                    |                                  |
+-------------------|----------------------------------|------------------+
| Node 2 Process (Base Port 8110)                      |                  |
|  +----------------v----------------+  +--------------v---------------+  |
|  | Staging IHost Container         |  | Production IHost Container   |  |
|  | Replica: staging-node-8110      |  | Replica: prod-node-8111      |  |
|  | Mesh: "staging-tenant-mesh"     |  | Mesh: "prod-tenant-mesh"     |  |
|  | TCP: 8110 | UDP Multicast: 8035 |  | TCP: 8111 | UDP Multicast:8036|  |
|  +---------------------------------+  +------------------------------+  |
+-------------------------------------------------------------------------+
```

---

## Key Code Concepts

### 1. Building Separate `IHost` / `IServiceCollection` Containers per Replica
Because the Gossip network is global within a single `IServiceCollection`, each isolated replica is built inside its own `Host.CreateEmptyApplicationBuilder` container with its own ports and multicast discovery channel:

```csharp
using var stagingHost = BuildIsolatedReplicaHost(
    args,
    loggerProvider,
    app,
    Constants.StagingMeshName,
    stagingReplicaId,
    stagingTcpPort,
    stagingHandshakePort,
    Constants.StagingMulticastPort);

using var prodHost = BuildIsolatedReplicaHost(
    args,
    loggerProvider,
    app,
    Constants.ProductionMeshName,
    prodReplicaId,
    prodTcpPort,
    prodHandshakePort,
    Constants.ProductionMulticastPort);
```

### 2. Resolving Isolated `IFeatureFlagCrdtManager` Instances from Each Host
At runtime, you resolve `DistributedCrdtScopeManager` from each independent `IHost` container to obtain the `IFeatureFlagCrdtManager` for `staging` and `prod`:

```csharp
var stagingScope = stagingHost.Services
    .GetRequiredService<DistributedCrdtScopeManager>()
    .GetOrCreateScope(stagingReplicaId);

var prodScope = prodHost.Services
    .GetRequiredService<DistributedCrdtScopeManager>()
    .GetOrCreateScope(prodReplicaId);

var stagingManager = stagingScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
var prodManager = prodScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
```

---

## Running the Sample

Open two or more terminals in the `03-Advanced/03-MultipleReplicas/Ama.Enterprise.Samples.MultipleReplicas` directory and run:

### Terminal 1 (Node 1 starting at Port 8100)
```bash
dotnet run -- 8100
```

### Terminal 2 (Node 2 starting at Port 8110)
```bash
dotnet run -- 8110
```

---

## Interactive Commands

Once the nodes are running, try the following workflow in Terminal 1:

1. Enable a feature flag in the `staging` replica only:
   ```text
   set staging NewCheckoutFlow true
   ```
   Observe both terminals: `NewCheckoutFlow` immediately synchronizes to Terminal 2's `STAGING` replica over `staging-tenant-mesh`, while both nodes' `PRODUCTION` replicas remain untouched.

2. Enable a different flag directly in `prod`:
   ```text
   set prod LegacyBanner false
   ```
   Observe that `LegacyBanner` only appears in the `PRODUCTION` section of both terminals via `prod-tenant-mesh`.

3. Promote a tested flag from `staging` to `prod`:
   ```text
   promote NewCheckoutFlow
   ```
   The local node reads `NewCheckoutFlow` from its `staging` replica scope and writes it into its `prod` replica scope, which then automatically replicates to all other nodes' `PRODUCTION` replicas over `prod-tenant-mesh`.

4. Delete a flag from a specific scope:
   ```text
   del staging NewCheckoutFlow
   ```

---
[⬅ Back to Advanced Overview](../README.md)