# 09 - Horizontal CRDT Sharding (`Ama.Enterprise.Samples.Sharding`)

When managing large-scale distributed state (such as thousands of multi-tenant configurations, IoT device fleets, or user workspaces), storing every entity in a single monolithic CRDT document or replicating every document onto every node in the cluster quickly becomes a memory and network bottleneck.

`Ama.Enterprise` provides Horizontal CRDT Sharding over a Consistent Hash Ring:
- Per-Shard Dotted Version Vectors (`ShardVersionVectorMap`): Causal history and operation clocks are tracked independently per `ShardId` rather than globally across the entire cluster.
- Selective Local Memory Hosting: Each node only keeps active CRDT documents in RAM for shards assigned to it on the consistent hash ring (controlled by `ShardReplicationFactor` and `VirtualNodesPerReplica`).
- Transparent P2P RPC Proxying: Any node in the mesh can read (`GetDocumentStateAsync`) or mutate (`ApplyIntentAsync`) any document ID. If the target shard is hosted on another replica, `ICrdtDocumentOrchestrator` transparently routes the read or write intent over P2P RPC to the owning replica(s).
- Zero-Downtime Dynamic Rebalancing: When nodes join or leave the P2P mesh, shards are automatically promoted (loaded from checkpoints/anti-entropy) or demoted (checkpointed and unloaded from active RAM) as the consistent hash ring rebalances.

---

## Architecture Overview

```text
                    +---------------------------------------------------+
                    |        Consistent Hash Ring (64 vNodes/node)      |
                    +---------------------------------------------------+
                         /                    |                     \
                        v                     v                      v
            +-------------------+   +-------------------+   +-------------------+
            |    node-8190      |   |    node-8191      |   |    node-8192      |
            |-------------------|   |-------------------|   |-------------------|
            | Local Shards:     |   | Local Shards:     |   | Local Shards:     |
            | - tenant-shard-2  |   | - tenant-shard-1  |   | - tenant-shard-3  |
            | - tenant-shard-5  |   | - tenant-shard-4  |   | - tenant-shard-6  |
            +-------------------+   +-------------------+   +-------------------+
                      ^                       |
                      |   P2P RPC Proxy       |
                      +-----------------------+
               (e.g. `set tenant-2 Billing true` executed on node-8191
                is automatically routed to shard owner node-8190)
```

### 1. Defining a Custom `IShardIdProvider`
In `Services/TenantShardIdProvider.cs`, each `TenantFeatureFlagState` document is mapped to its own logical shard (`tenant-shard-{documentId}`):

```csharp
public sealed class TenantShardIdProvider : IShardIdProvider
{
    public string GetShardId<T>(CrdtDocument<T> document) where T : class
    {
        if (document.Data is TenantFeatureFlagState state && !string.IsNullOrWhiteSpace(state.Id))
        {
            return $"tenant-shard-{state.Id}";
        }

        return "default";
    }

    public string GetShardId(IComparable documentId, Type documentType)
    {
        if (documentType == typeof(TenantFeatureFlagState))
        {
            return $"tenant-shard-{documentId}";
        }

        return "default";
    }
    // ...
}
```

### 2. Enabling Sharding in Dependency Injection
In `Program.cs` and `Extensions/ShardingServiceCollectionExtensions.cs`:

```csharp
builder.Services.AddDistributedCrdtCore(options =>
{
    options.EnableSharding = true;
    options.ShardReplicationFactor = 1; // Set to 2 or 3 for multi-replica fault tolerance per shard
    options.VirtualNodesPerReplica = 64;
});

builder.Services.AddCrdt()
        .AddCrdtAotContext(new ShardingCrdtAotContext())
        .AddCrdtJsonTypeInfoResolver(ShardingJsonContext.Default)
        .AddCrdtSystemTextJson()
        .AddCrdtShardIdProvider<TenantShardIdProvider>();
```

---

## Step-by-Step Walkthrough

Open terminals in `03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding`.

### Step 1: Start Node 1 (`node-8190`)
In Terminal 1, run:

```bash
dotnet run -- 8190
```

Because `node-8190` is currently the only node in the cluster, it owns 100% of the consistent hash ring.

### Step 2: Seed 6 Sharded Tenant Documents
In the `node-8190` terminal, type:

```text
> seed
```

You will see all 6 documents (`tenant-1` through `tenant-6`) marked as `[LOCAL ]` on `node-8190` because it is the sole replica in the ring.

### Step 3: Scale Out by Starting Node 2 (`node-8191`)
Type `clone` in `node-8190` (or open a second terminal in `03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding` and run `dotnet run -- 8191`).

As soon as `node-8191` discovers `node-8190` over UDP multicast:
1. The consistent hash ring recalculates shard ownership across `[node-8190, node-8191]`.
2. Roughly half of the `tenant-shard-*` shards are promoted on `node-8191` and synced via shard-targeted anti-entropy.
3. Those transferred shards are demoted from active RAM on `node-8190` (changing from `[LOCAL ]` to `[REMOTE]` in `node-8190`'s console view).

### Step 4: Read and Mutate Remote Shards Transparently via P2P RPC
On `node-8190`, pick a tenant that is currently marked `[REMOTE]` (hosted on `node-8191`) and mutate or read it directly from `node-8190`:

```text
> set tenant-1 SuperFeature true
> read tenant-1
```

Even though `node-8190` does not host `tenant-shard-tenant-1` in local memory:
- `set tenant-1 SuperFeature true` is transparently forwarded via P2P RPC to `node-8191` and applied immediately.
- `read tenant-1` transparently queries `node-8191` over P2P RPC and displays the updated flags on `node-8190`.

### Step 5: Test Replication Factor (`RF = 2`)
You can also pass a custom `ShardReplicationFactor` as the second CLI argument (e.g. `RF = 2` across a 3-node cluster) so every shard is actively hosted on 2 distinct replicas for high availability:

```bash
dotnet run -- 8190 2
dotnet run -- 8191 2
dotnet run -- 8192 2
```

---
[⬅ Back to Advanced Overview](../README.md)
[Back to Advanced Overview](../README.md)