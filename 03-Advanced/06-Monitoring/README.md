# 06 - Cluster Monitoring & Diagnostics

Observability is essential when running decentralized Peer-to-Peer (P2P) clusters in production. Because there is no central database or broker to inspect, `Ama.Enterprise` exposes built-in hooks, health indicators, and metrics so you can monitor each replica's view of the cluster in real time.

## Key Concepts
- Peer connectivity and session tracking: Inspect active peers, connection health, and authenticated session claims across every P2P mesh.
- CRDT convergence and Dotted Version Vectors (DVVs): Track causal progress across shards, monitor background anti-entropy synchronization cycles, and detect lagging peers.
- Memory and journal footprints: Measure live document memory usage, operation journal sizes, and checkpoint timings.

(The interactive monitoring sample for this section is coming soon!)

---
[⬅ Back to Advanced Overview](../README.md)