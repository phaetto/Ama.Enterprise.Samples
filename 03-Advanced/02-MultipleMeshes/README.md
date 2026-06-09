# Advanced 02: Multiple Meshes ("Bridge / Gateway Node" Topology)

In complex enterprise or edge deployments, nodes are often separated into isolated network segments (e.g., an internal corporate LAN vs. an edge DMZ, or two distinct VPC subnets). 

`Ama.Enterprise` allows a single CRDT replica to bind to multiple P2P meshes simultaneously, acting as a Bridge / Gateway Node that seamlessly routes and converges CRDT state across isolated network zones without requiring custom forwarding logic.

---

## Architecture Overview

This sample defines two completely isolated UDP Multicast / TCP network zones:

1. Zone A (`mesh-alpha`): Uses UDP Multicast Port `8035`.
2. Zone B (`mesh-beta`): Uses UDP Multicast Port `8036`.

```text
+-----------------------+          +------------------------+          +-----------------------+
|      Node 1 (Alpha)   |          |    Node 3 (Bridge)     |          |      Node 2 (Beta)    |
|                       |          |                        |          |                       |
|  Replica: node-alpha  |          |  Replica: node-bridge  |          |  Replica: node-beta   |
|  Mesh: mesh-alpha     | <======> |  Meshes:               | <======> |  Mesh: mesh-beta      |
|  (UDP Port 8035)      |          |   - mesh-alpha (8035)  |          |  (UDP Port 8036)      |
|                       |          |   - mesh-beta  (8036)  |          |                       |
+-----------------------+          +------------------------+          +-----------------------+
```

When the Bridge node boots up, it registers both `mesh-alpha` and `mesh-beta` and links both meshes to its single local CRDT replica:

```csharp
// Bind mesh-alpha to the bridge replica
services.AddDistributedCrdtP2p("mesh-alpha", replicaId);
services.AddP2pMesh("mesh-alpha")...

// Bind mesh-beta to the exact same bridge replica
services.AddDistributedCrdtP2p("mesh-beta", replicaId);
services.AddP2pMesh("mesh-beta")...
```

Because both meshes feed operations into - and synchronize anti-entropy state from - the same underlying CRDT replica on the bridge node, state mutations in `mesh-alpha` automatically propagate into `mesh-beta` and vice-versa.

---

## Running the Sample

Open three separate terminal windows in the `03-Advanced/02-MultipleMeshes/Ama.Enterprise.Samples.MultipleMeshes` directory:

### Step 1: Start Node 1 in `alpha` mode (Zone A)
```bash
dotnet run -- alpha
```

### Step 2: Start Node 2 in `beta` mode (Zone B)
```bash
dotnet run -- beta
```

### Step 3: Verify Network Isolation
In Terminal 1 (`alpha`), set a feature flag:
```text
> set AlphaOnlyFlag true
```
And in Terminal 2 (`beta`), set another feature flag:
```text
> set BetaOnlyFlag false
```
Notice that Node 1 and Node 2 do not see each other's flags because `mesh-alpha` (port `8035`) and `mesh-beta` (port `8036`) are completely isolated meshes.

### Step 4: Start Node 3 in `bridge` mode (The "Aha!" Moment)
In Terminal 3, start the bridge node:
```bash
dotnet run -- bridge
```

Within a few seconds, as peer discovery and anti-entropy kick in:
1. Node 3 (`bridge`) discovers Node 1 on `mesh-alpha` and Node 2 on `mesh-beta`.
2. Both `AlphaOnlyFlag` and `BetaOnlyFlag` converge onto the Bridge replica.
3. The Bridge replica gossips and syncs the merged state back out to both `mesh-alpha` and `mesh-beta`.
4. All three terminals now display the exact same synchronized feature flags! Any subsequent `set` or `del` command on any node will flow across the bridge to all other nodes.

---
[⬅ Back to Advanced Overview](../README.md)