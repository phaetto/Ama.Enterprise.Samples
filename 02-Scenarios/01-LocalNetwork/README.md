# Local Network P2P Connectivity

These scenarios are mostly suited in networks you have full control with. Generally, in SaaS providers like Azure and AWS, network capabilities are limited.

When you completely control the network though, you can have a very fast and cheap discovery of nodes using UDP.

## High-Level Idea
In a controlled Local Area Network (LAN), you can utilize broadcast or multicast capabilities. The samples in this category demonstrate:
- **UDP Multicast Discovery:** Nodes automatically find each other by broadcasting their presence on a shared UDP multicast address without needing a central registry.
- **Direct TCP/UDP Transports:** Once discovered, nodes establish highly efficient, direct peer-to-peer connections using raw TCP streams or UDP datagrams to synchronize CRDT states.

### Samples
- [**Ama.Enterprise.Samples.TcpUdpFeatureFlags**](../../01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Program.cs): A beginner-friendly showcase demonstrating how to form a masterless P2P mesh cluster over local TCP and UDP connections with automatic interactive node discovery, synchronizing feature flags out of the box.
- [**Ama.Enterprise.Samples.QuicUdpFeatureFlags**](./Ama.Enterprise.Samples.QuicUdpFeatureFlags/README.md): A showcase demonstrating a highly secure, masterless P2P mesh cluster using QUIC transport and UDP discovery. It highlights advanced security features like mTLS transport validation, Certificate Authentication, and Symmetric Wire Encryption.

---
[⬅ Back to Main Documentation](../README.md)