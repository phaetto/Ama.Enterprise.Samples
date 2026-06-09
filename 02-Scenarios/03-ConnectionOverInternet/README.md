# Connection Over the Public Internet

Connecting decentralized nodes across the public internet introduces significant networking challenges, primarily dealing with Network Address Translation (NAT), strict firewalls, and dynamic IP addresses.

## High-Level Idea
To build a globally distributed mesh or to connect edge nodes (like browsers, mobile, or IoT devices) directly to backend clusters, we need advanced transport and signaling mechanisms. The samples in this category demonstrate:
- **WebRTC Integration:** Establishing direct peer-to-peer data channels that can automatically traverse NATs using STUN/TURN servers.
- **Out-of-Band Signaling:** How nodes exchange connection parameters (SDP offers/answers) over an initial centralized relay (like WebSockets or MQTT) before upgrading to a direct, decentralized P2P link.
- **MQTT Broker Discovery:** Utilizing standard IoT brokers for both node discovery and message transport in highly decoupled geographic architectures.

*(Samples for this scenario will be added and linked here)*

---
[⬅ Back to Main Documentation](../README.md)