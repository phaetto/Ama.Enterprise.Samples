# Local Network P2P Connectivity

These samples show how to build fast, masterless P2P clusters on a Local Area Network (LAN) or private VPN where you control the network settings.

Unlike public cloud environments (which often block broadcast traffic), a local network lets nodes discover each other directly using UDP multicast without any central server or registry.

## Key Concepts
- UDP multicast discovery: Nodes announce themselves on a shared multicast address (`239.255.0.1`) so peers find each other automatically with zero configuration.
- Direct TCP and QUIC transports: Once discovered, nodes connect directly to each other over TCP or QUIC to sync CRDT state in real time.

## Included Samples

- [TCP/UDP Feature Flags (Beginner)](../../01-Beginner/README.md): A great starting point that shows how to form a local P2P mesh over TCP and UDP with automatic node discovery and interactive console commands.
- [QUIC/UDP Feature Flags](./Ama.Enterprise.Samples.QuicUdpFeatureFlags/README.md): Shows how to run a high-performance P2P mesh over QUIC and UDP discovery, secured with mTLS, X.509 certificate authentication, and AES-GCM wire encryption.

---
[⬅ Back to Scenarios Overview](../README.md)