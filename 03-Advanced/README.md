# Advanced Architecture & CRDT Capabilities

Once you understand the fundamentals of setting up a Peer-to-Peer (P2P) mesh and synchronizing built-in state, this folder shows how to build custom CRDT domain models, partition topologies, persist state, and evolve schemas in production.

## Explore the Advanced Samples

### [01. Creating a Custom CRDT (`Ama.Enterprise.Samples.CustomCrdt`)](./01-CustomCrdt/README.md)
Shows how to design your own Native AOT-ready CRDT document models and custom `ICrdtDocumentOrchestrator` managers. Builds a distributed chat messenger using Priority Queues (`[CrdtPriorityQueueStrategy]`), Observed-Remove Sets (`[CrdtOrSetStrategy]`), and Approval Quorums (`[CrdtApprovalQuorum]`).

### [02. Multiple Meshes (`Ama.Enterprise.Samples.MultipleMeshes`)](./02-MultipleMeshes/README.md)
Demonstrates the Bridge / Gateway Node topology where a single CRDT replica connects to two isolated P2P network zones (`mesh-alpha` and `mesh-beta`) at the same time, automatically routing and converging state between them.

### [03. Multiple Replicas (`Ama.Enterprise.Samples.MultipleReplicas`)](./03-MultipleReplicas/README.md)
Shows how to host multiple isolated CRDT replicas (such as `staging` and `prod` environments or separate tenants) inside a single .NET process using `ScopeKey` and `DistributedCrdtScopeManager`, all sharing the same underlying TCP/UDP mesh.

### [04. Custom CRDT Persistence (`Ama.Enterprise.Samples.Persistence`)](./04-Persistence/README.md)
Walks through implementing a custom `IDistributedCrdtStorage` provider backed by SQLite in WAL mode to persist document snapshots, Dotted Version Vectors (DVVs), and the operation journal across restarts. Also covers the built-in `Ama.Enterprise.CRDT.Distributed.TableStorage` package.

### [05. Zero-Trust Routing (`Ama.Enterprise.Samples.ZeroTrustRouting`)](./05-ZeroTrustRouting/README.md)
Demonstrates how to combine `ISessionTokenValidator`, `IMeshRoutingPolicy`, and `IScopeTopologyProvider` to enforce Role-Based Access Control (RBAC) and directional geo-fencing (`US -> EU` allowed, `EU -> US` blocked) over a shared discovery network.

### [06. Cluster Monitoring & Diagnostics (`Ama.Enterprise.Samples.Monitoring`)](./06-Monitoring/README.md)
Covers observability concepts for decentralized clusters, including peer connectivity health, Dotted Version Vector convergence tracking, and memory/journal diagnostics.

### [07. CRDT Serialization (`Ama.Enterprise.Samples.Serialization`)](./07-Serialization/README.md)
Shows how to configure and compare the three built-in CRDT serialization modes: uncompressed `System.Text.Json`, Brotli-compressed `System.Text.Json`, and binary `MessagePack` (`Ama.Enterprise.CRDT.MessagePack`).

### [08. Live CRDT Schema Migration (`Ama.Enterprise.Samples.Migration`)](./08-Migration/README.md)
Demonstrates how to run a Live Migration Mediator (`ICrdtDocumentMigrator<TSource, TTarget>`) to translate state in real time between a Legacy V1 cluster and a Modern V2 cluster with breaking schema changes and zero downtime.

### [09. Horizontal CRDT Sharding (`Ama.Enterprise.Samples.Sharding`)](./09-Sharding/README.md)
Explains how to partition large CRDT datasets across logical shards with independent per-shard Dotted Version Vectors (`ShardVersionVectorMap`) and targeted anti-entropy synchronization.

---
[⬅ Back to Main Documentation](../README.md)