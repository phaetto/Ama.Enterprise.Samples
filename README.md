# Ama.Enterprise Samples

Welcome to the official samples repository for `Ama.Enterprise`. This repository contains various interactive console applications and showcases demonstrating how to build decentralized, masterless Peer-to-Peer (P2P) distributed systems using the `Ama.Enterprise` toolkit in .NET 10.

## Overview

`Ama.Enterprise` combines robust networking meshes with Conflict-free Replicated Data Types (CRDTs) to guarantee high availability, fault tolerance, and eventual consistency across nodes without requiring centralized databases or brokers. 

These samples are designed to be Native AOT ready and showcase various transport mechanisms, discovery protocols, and orchestration techniques.

## Use Cases

The samples are divided into different scenarios to help you understand how to use the toolkit in various network environments:

### 1. Local Network

Demonstrates fundamental P2P mesh formation on a Local Area Network (LAN).
- Shows how nodes automatically discover peers using UDP Multicast.
- Establishes direct peer-to-peer communication using standard TCP and UDP transports.
- Features highly interactive console applications where you can dynamically spawn new nodes that instantly discover and sync with the primary node.

*See the **[Local Network](./02-Scenarios/01-LocalNetwork/README.md)** for detailed documentation.*

### 2. SaaS Backend Integration

Demonstrates how to bind P2P meshes directly into existing managed web services.
- Shows integration with ASP.NET Core (Kestrel) and background workers.
- Ideal for architectures that require a hybrid of traditional centralized APIs and decentralized background sync mechanisms.

*See the **[SaaS Backend Integration](./02-Scenarios/02-SassBackend/README.md)** for detailed documentation.*

### 3. Connection Over Internet

Advanced samples demonstrating how to traverse NATs and connect nodes securely across the public internet.
- Explores out-of-band signaling and data channels (e.g., WebRTC).
- Perfect for understanding how to connect direct browser-to-server or globally distributed nodes.

*See the **[Connection Over Internet](./02-Scenarios/03-ConnectionOverInternet/README.md)** for detailed documentation.*

## Getting Started

If you are new to `Ama.Enterprise`, we highly recommend starting with the **[Beginner Tutorial](./01-Beginner/README.md)**.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Running a Sample

Most samples are interactive console applications. To get the best experience, we recommend running multiple instances of a sample to see the masterless P2P discovery and CRDT synchronization in action.

1. Clone the repository and navigate to the desired sample folder.
2. Build the solution:
   ```bash
   dotnet build
   ```
3. Run the primary node (Beginner feature flags sample):
   ```bash
   cd 01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags
   dotnet run
   ```
4. *Tip: While the interactive console app is running, try typing commands like `clone` to automatically spawn a brand-new node process on a new port that instantly discovers and syncs with your primary node.*

## About Ama.Enterprise

`Ama.Enterprise` is an enterprise-grade toolkit offering:
- **Masterless P2P Mesh Networking:** Gossip and Anti-Entropy protocols.
- **Transport Agnostic:** Support for TCP, UDP, ASP.NET Core (Kestrel), WebRTC, and MQTT.
- **Distributed CRDT Orchestrator:** Manage lifecycles, version vectors, and synchronization of distributed documents automatically.
- **Native AOT Ready:** Zero runtime reflection, strict source-generated `System.Text.Json` contexts, and highly optimized MessagePack serialization.

## License

The samples in this repository are open-source under MIT. However, the core `Ama.Enterprise` packages they consume operate under a sustainable, revenue-capped dual license system (Community & Enterprise).

- **Community License (Free):** For individuals, startups, and non-profits with less than $1M USD in annual gross revenue/funding.
- **Enterprise License (Paid):** Required for organizations exceeding the $1M USD threshold.

For detailed licensing information, please refer to the main `Ama.Enterprise` repository documentation.