# Ama.Enterprise Samples

Welcome to the official samples repository for `Ama.Enterprise`!

This repository contains practical, runnable .NET 10 applications that show how to build decentralized, masterless Peer-to-Peer (P2P) systems using the `Ama.Enterprise` toolkit.

## What is Ama.Enterprise?

`Ama.Enterprise` combines Peer-to-Peer (P2P) mesh networking with Conflict-free Replicated Data Types (CRDTs). It lets multiple instances of your application automatically discover each other, share state in real time, and stay eventually consistent—without needing a central coordinator or message broker.

Key capabilities include:
- Masterless P2P mesh: Built-in gossip and anti-entropy background synchronization.
- Flexible transports and discovery: Works over TCP, QUIC, UDP multicast, HTTP/ASP.NET Core (Kestrel), DNS, WebRTC, and MQTT.
- Zero-trust security: Supports mTLS, X.509 certificate authentication, and AES-GCM payload encryption.
- Native AOT ready: Built for high performance with zero runtime reflection, source-generated `System.Text.Json`, and MessagePack serialization.

---

## Where to Start

### New to Ama.Enterprise?
Start with the [01 - Beginner Tutorial (TCP/UDP Feature Flags)](./01-Beginner/README.md). It walks you step by step through starting a local cluster where you can type `clone` in the terminal to spawn new nodes and watch feature flags sync live across windows.

### Explore by Network Scenario
Once you know the basics, explore the [02 - Scenarios](./02-Scenarios/README.md) folder to see how `Ama.Enterprise` works in different environments:

1. [Local Network (LAN / VPN)](./02-Scenarios/01-LocalNetwork/README.md)
   - Automatic peer discovery using UDP multicast.
   - Direct peer-to-peer communication over TCP and QUIC (with mTLS and wire encryption).
2. [SaaS & Cloud Backends](./02-Scenarios/02-SassBackend/README.md)
   - Integrates directly into ASP.NET Core Minimal APIs over standard HTTP.
   - Cloud-friendly peer discovery using DNS (A/AAAA records) for Kubernetes or HTTP load balancer polling for Azure App Service.
   - Includes durable state persistence with Azure Table Storage and an Azure Bicep deployment template.
3. [Connection Over the Public Internet](./02-Scenarios/03-ConnectionOverInternet/README.md)
   - Overview of NAT traversal, WebRTC, and MQTT for connecting nodes across the open internet.

---

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build and Run the Beginner Sample

1. Clone this repository and build the solution:
   ```bash
   dotnet build
   ```
2. Run the beginner interactive console app:
   ```bash
   dotnet run --project 01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags
   ```
3. In the console prompt, type `clone` and press Enter to launch a second node in a new window, then type `set MyFeature true` to see instant P2P synchronization!

---

## License

The sample code in this repository is licensed under the MIT License.

The underlying `Ama.Enterprise` NuGet packages use a dual-license model:
- Community License (Free): Free for individuals, open-source projects, non-profits, and companies with under $1M USD in annual gross revenue/funding.
- Enterprise License (Paid): For organizations exceeding the $1M USD threshold.

See the main [Ama.Enterprise repository](https://github.com/phaetto/Ama.Enterprise) for full licensing details.