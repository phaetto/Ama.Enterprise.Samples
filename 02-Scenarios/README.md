# Scenarios Overview

This folder contains different architectural scenarios demonstrating how to build and deploy `Ama.Enterprise` nodes across various network environments.

## Available Scenarios

### [01. Local Network](./01-LocalNetwork/README.md)
Demonstrates fundamental P2P mesh formation on a Local Area Network (LAN). Nodes automatically discover peers using UDP Multicast and establish direct communication using standard TCP and UDP transports.

### [02. SaaS Backend Integration](./02-SassBackend/README.md)
Focuses on deploying nodes in cloud and SaaS environments where UDP multicast is often restricted. Demonstrates integration with ASP.NET Core (Kestrel), central registries, and safe co-existence with standard REST/GraphQL APIs.

### [03. Connection Over Internet](./03-ConnectionOverInternet/README.md)
Advanced networking samples for global internet deployments. Covers NAT traversal, WebRTC out-of-band signaling, and connecting distributed nodes securely across the public web.

---
[⬅ Back to Main Documentation](../README.md)