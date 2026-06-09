# 04 - Custom CRDT Persistence (SQLite Storage)

By default, `Ama.Enterprise.CRDT.Distributed` stores CRDT documents, Dotted Version Vectors (DVVs), and the operation journal in memory. In production environments, you need nodes to survive process restarts, recover from crashes without losing local mutations, and participate in delta anti-entropy synchronization using a durable operation log.

This sample demonstrates how to implement and register a custom, Native AOT-compatible SQLite storage provider using `IDistributedCrdtStorage`.

---

## Architecture Overview

To persist distributed CRDT state, you implement `IDistributedCrdtStorage` (which also inherits from `ICrdtOperationJournal`) and register it into the Dependency Injection container using `AddDistributedCrdtStorage<TStorage>()`.

```text
+-------------------------------------------------------------------------+
|                        Node Process (node-8100)                         |
|                                                                         |
|  +---------------------------+        +------------------------------+  |
|  | IFeatureFlagCrdtManager   | -----> | Distributed CRDT Orchestrator|  |
|  +---------------------------+        +--------------+---------------+  |
|                                                      |                  |
|                                                      v                  |
|                                       +------------------------------+  |
|                                       |      SqliteCrdtStorage       |  |
|                                       |  (IDistributedCrdtStorage)   |  |
|                                       +--------------+---------------+  |
|                                                      |                  |
+------------------------------------------------------|------------------+
                                                       |
                                                       v
                                        +------------------------------+
                                        |  crdt_storage_node-8100.db   |
                                        |  - Documents (Snapshots)     |
                                        |  - GlobalDvv (Version Vector)|
                                        |  - ClusterState (Membership) |
                                        |  - Journal (Operation Log)   |
                                        +------------------------------+
```

### What `SqliteCrdtStorage` Persists

Our custom `SqliteCrdtStorage` creates a per-replica SQLite database (`crdt_storage_<replicaId>.db`) in WAL (`Write-Ahead Logging`) mode with four tables:

1. `Documents`: Stores serialized `CrdtDocument<TState>` snapshots (both data and CRDT metadata) keyed by `ShardId` and `DocumentId`.
2. `GlobalDvv`: Stores the `ShardVersionVectorMap` for the replica, tracking causal history and seen dots across all shards.
3. `ClusterState`: Stores `ClusterStateSnapshotDto` so the replica remembers known peer progress and avoids amnesia after restarting.
4. `Journal`: Stores `JournaledOperation` entries indexed by `(ShardId, ShardClock)` so that when peers reconnect after an outage, missing delta operations can be streamed directly from SQLite via `GetOperationsByRangeAsync` or `GetOperationsByDotsAsync`.

### Single-Writer / Multiple-Reader Pattern

SQLite supports unlimited concurrent readers in WAL mode, but strictly one writer at a time. To maximize asynchronous throughput and eliminate `SQLITE_BUSY` (`database is locked`) errors under heavy write loads, `SqliteCrdtStorage`:
- Maintains a single dedicated `SqliteConnection` for write transactions guarded by an asynchronous `SemaphoreSlim(1, 1)`.
- Opens lightweight pooled connections on demand for concurrent read queries (`LoadDocumentAsync`, `GetOperationsByRangeAsync`, etc.).

---

## Key Code Walkthrough

### 1. Configuring Checkpointing and WAL Buffering (`Program.cs`)

In `Program.cs`, we configure `AddDistributedCrdtCore` to checkpoint state periodically and buffer journal writes in memory before flushing batches to SQLite:

```csharp
builder.Services.AddDistributedCrdtCore(options =>
{
    options.ActiveSyncEnabled = true;
    options.CheckpointIntervalSeconds = 5;
    options.AntiEntropyIntervalSeconds = 5;
    options.AntiEntropyInitialDelaySeconds = 2;
    options.JournalMode = CrdtJournalMode.InMemoryBuffer;
    options.WalChannelCapacity = 10000;
    options.WalMaxBatchSize = 1000;
    options.WalDirectoryPath = AppContext.BaseDirectory;
});
```

### 2. Registering Custom Storage (`Program.cs`)

Registering the custom storage implementation takes a single line of code:

```csharp
builder.Services.AddDistributedCrdtStorage<SqliteCrdtStorage>();
```

Each replica scope resolved via `DistributedCrdtScopeManager` receives its own scoped `SqliteCrdtStorage` instance injected with that replica's `ReplicaContext` and `ICrdtSerializer`.

---

## Built-in Azure Table Storage Provider (`Ama.Enterprise.CRDT.Distributed.TableStorage`)

If you are deploying to Azure or using Azurite locally, you do not need to write a custom `IDistributedCrdtStorage` implementation from scratch. An official, out-of-the-box Azure Table Storage provider is available via the `Ama.Enterprise.CRDT.Distributed.TableStorage` NuGet package.

### Installation

```bash
dotnet add package Ama.Enterprise.CRDT.Distributed.TableStorage
```

### Registration

Instead of calling `AddDistributedCrdtStorage<TStorage>()`, use the `AddDistributedCrdtTableStorage` extension method from `Ama.Enterprise.CRDT.Distributed.TableStorage.Extensions`:

```csharp
using Ama.Enterprise.CRDT.Distributed.TableStorage.Extensions;

builder.Services.AddDistributedCrdtTableStorage(options =>
{
    options.ConnectionString = "UseDevelopmentStorage=true";
    options.UseBinarySerialization = true;
});
```

This automatically registers the Table Storage implementation for storing CRDT document snapshots, Dotted Version Vectors (DVVs), cluster membership state, and the operation journal.

---

## Running the Sample

### Step 1: Start Node 1 on Port `8100`

Open a terminal and run the project on port `8100`:

```bash
cd 03-Advanced/04-Persistence/Ama.Enterprise.Samples.Persistence
dotnet run -- 8100
```

### Step 2: Mutate State

In the interactive console, create a few feature flags:

```text
> set DarkMode true
> set BetaCheckout false
```

Wait a few seconds (or type `exit` which flushes pending checkpoints on graceful shutdown). Notice that `crdt_storage_node-8100.db` is created in the working directory.

### Step 3: Stop and Restart Node 1

Stop the application (`exit` or `Ctrl+C`), then start it again on the exact same port (`8100`) so it reuses the `node-8100` replica identity:

```bash
dotnet run -- 8100
```

Even with no other peers running on the network, `node-8100` immediately restores `DarkMode: ENABLED` and `BetaCheckout: DISABLED` from its local SQLite database!

### Step 4: Spawn a Second Node and Verify Sync

While `node-8100` is running, type `clone` (or run `dotnet run -- 8101` in a second terminal). The new node (`node-8101`) will automatically discover `node-8100`, synchronize the persisted flags via anti-entropy, and persist them to its own `crdt_storage_node-8101.db` file.

---
[⬅ Back to Advanced Overview](../README.md)