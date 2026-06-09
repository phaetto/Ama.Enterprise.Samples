# Scenarios Overview

This folder organizes the `Ama.Enterprise` samples by the type of network environment you are targeting.

## Choose Your Scenario

### [01. Local Network (LAN / VPN)](./01-LocalNetwork/README.md)
Best for on-premise servers, local networks, or private VPNs where you control the network. Nodes find each other automatically with UDP multicast and sync directly over TCP or QUIC.

### [02. SaaS & Cloud Backends](./02-SassBackend/README.md)
Best for cloud platforms like Kubernetes, Azure App Service, AWS, or GCP where UDP multicast is blocked. Shows how to host the P2P mesh directly inside ASP.NET Core (Kestrel) over HTTP, discover peers via DNS or load balancer polling, and persist state to Azure Table Storage.

### [03. Connection Over the Public Internet](./03-ConnectionOverInternet/README.md)
Designed for global deployments and edge devices that need to connect across the public internet through firewalls and NATs using WebRTC and MQTT.

---
[⬅ Back to Main Documentation](../README.md)