# Ama.Enterprise Samples

Welcome to the official samples repository for `Ama.Enterprise`!

This repository contains practical, interactive .NET applications that show how to build decentralized, masterless Peer-to-Peer (P2P) systems using the `Ama.Enterprise` toolkit.

## What is Ama.Enterprise?

`Ama.Enterprise` combines Peer-to-Peer (P2P) mesh networking with Conflict-free Replicated Data Types (CRDTs). It lets multiple instances of your application automatically discover each other, share state in real time, and stay eventually consistent without needing a central coordinator or message broker.

Key capabilities include:
- Masterless P2P mesh: Built-in gossip and anti-entropy background synchronization.
- Flexible transports and discovery: Works over TCP, QUIC, UDP multicast, HTTP/ASP.NET Core (Kestrel), DNS, WebRTC, and MQTT.
- Zero-trust security: Supports mTLS, X.509 certificate authentication, session-based routing, and AES-GCM payload encryption.
- Native AOT ready: Built for high performance with zero runtime reflection, source-generated `System.Text.Json`, and MessagePack serialization.

---

## Where to Start

### 1. New to Ama.Enterprise?
Start with the [01 - Beginner Tutorial (TCP/UDP Feature Flags)](./01-Beginner/README.md). It walks you step by step through starting a local cluster where you can type `clone` in the terminal to spawn new nodes and watch feature flags sync live across windows.

### 2. Explore by Network Scenario
Once you know the basics, explore the [02 - Scenarios](./02-Scenarios/README.md) folder to see how `Ama.Enterprise` works in different environments:

1. [Local Network (LAN / VPN)](./02-Scenarios/01-LocalNetwork/README.md)
   - Automatic peer discovery using UDP multicast.
   - Direct peer-to-peer communication over TCP and QUIC (with mTLS and wire encryption).
2. [SaaS & Cloud Backends](./02-Scenarios/02-SaasBackend/README.md)
   - Integrates directly into ASP.NET Core Minimal APIs over standard HTTP.
   - Cloud-friendly peer discovery using DNS for Kubernetes or HTTP load balancer polling for Azure App Service.
   - Includes durable state persistence with Azure Table Storage and an Azure Bicep deployment template.
3. [Connection Over the Public Internet](./02-Scenarios/03-ConnectionOverInternet/README.md)
   - Overview of NAT traversal, WebRTC, and MQTT for connecting nodes across the open internet.

### 3. Deep Dive into Advanced Architecture
Ready to build custom models, multi-tenant meshes, or zero-downtime migrations? Check out the [03 - Advanced](./03-Advanced/README.md) samples:

1. [01. Custom CRDT (`Ama.Enterprise.Samples.CustomCrdt`)](./03-Advanced/01-CustomCrdt/README.md) - Build a distributed chat application using priority queues, OR-Sets, and approval quorums.
2. [02. Multiple Meshes (`Ama.Enterprise.Samples.MultipleMeshes`)](./03-Advanced/02-MultipleMeshes/README.md) - Bridge two isolated P2P network segments using a single gateway replica.
3. [03. Multiple Replicas (`Ama.Enterprise.Samples.MultipleReplicas`)](./03-Advanced/03-MultipleReplicas/README.md) - Host isolated staging and production replicas over a single shared P2P mesh.
4. [04. Persistence (`Ama.Enterprise.Samples.Persistence`)](./03-Advanced/04-Persistence/README.md) - Persist CRDT snapshots, version vectors, and journals to SQLite or Azure Table Storage.
5. [05. Zero-Trust Routing (`Ama.Enterprise.Samples.ZeroTrustRouting`)](./03-Advanced/05-ZeroTrustRouting/README.md) - Enforce RBAC role isolation and directional geo-fencing across a shared mesh.
6. [06. Monitoring (`Ama.Enterprise.Samples.Monitoring`)](./03-Advanced/06-Monitoring/README.md) - Inspect cluster health, metrics, and distributed diagnostics.
7. [07. Serialization (`Ama.Enterprise.Samples.Serialization`)](./03-Advanced/07-Serialization/README.md) - Compare uncompressed JSON, Brotli-compressed JSON, and binary MessagePack serialization.
8. [08. Live Schema Migration (`Ama.Enterprise.Samples.Migration`)](./03-Advanced/08-Migration/README.md) - Migrate live CRDT state between breaking V1 and V2 schemas without cluster downtime.
9. [09. Sharding (`Ama.Enterprise.Samples.Sharding`)](./03-Advanced/09-Sharding/README.md) - Partition large CRDT datasets across logical shards.

---

## Quick Start

### Prerequisites
- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0)

### Build and Run the Beginner Sample

1. Clone this repository and build the solution:
   ```bash
   dotnet build
   ```
2. Run the beginner interactive console app:
   ```bash
   dotnet run --project 01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags
   ```
3. In the console prompt, type `clone` and press Enter to launch a second node in a new window, then type `set MyFeature true` to see instant P2P synchronization.

---

## License

The sample code in this repository is licensed under the MIT License.

The underlying `Ama.Enterprise` NuGet packages use a dual-license model:
- Community License (Free): Free for individuals, open-source projects, non-profits, and companies with under $1M USD in annual gross revenue/funding.
- Enterprise License (Paid): For organizations exceeding the $1M USD threshold.

See the main [Ama.Enterprise repository](https://github.com/phaetto/Ama.Enterprise) for full licensing details.