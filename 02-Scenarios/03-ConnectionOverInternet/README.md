# Connection Over the Public Internet

Connecting decentralized nodes across the public internet comes with extra networking challenges—especially NAT (Network Address Translation), firewalls, and changing IP addresses.

## Key Concepts
To connect nodes across different networks or link edge devices (like browsers, mobile apps, or IoT devices) directly to a cluster, you need NAT traversal and external signaling:
- WebRTC data channels: Establishes direct, encrypted peer-to-peer connections across the internet using STUN/TURN servers to punch through NATs and firewalls.
- Out-of-band signaling: Uses a lightweight relay (such as WebSockets or MQTT) so two nodes can exchange initial connection details (SDP offers/answers) before switching to a direct P2P connection.
- MQTT broker discovery and transport: Uses standard MQTT brokers to discover peers and relay messages across geographically separated networks.

(Samples for this scenario are coming soon!)

---
[⬅ Back to Scenarios Overview](../README.md)