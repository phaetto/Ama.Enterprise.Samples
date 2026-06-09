# SaaS Backend Integration

When deploying distributed systems to Cloud and Software-as-a-Service (SaaS) providers (like Azure, AWS, or GCP), traditional network capabilities such as UDP multicast are often blocked or restricted by Virtual Private Cloud (VPC) rules.

## High-Level Idea
To achieve a decentralized P2P mesh within restricted cloud environments, we rely on standard protocols and infrastructure that cloud providers support natively. The samples in this category demonstrate:
- **ASP.NET Core (Kestrel) Integration:** Binding the P2P mesh directly into your existing web host pipelines over standard HTTP/TCP ports.
- **Cloud-Friendly Discovery:** Using alternative discovery mechanisms such as active HTTP polling, central registries, or DNS A/AAAA records instead of UDP.
- **Seamless Coexistence:** Running CRDT synchronization safely as background workers alongside your standard REST APIs.

## Included Samples

- [**HTTP / DNS Feature Flags Sample**](./Ama.Enterprise.Samples.HttpDnsFeatureFlags/README.md): Demonstrates how to build an ASP.NET Core minimal API running masterless feature flags driven by native HTTP transports and DNS peer discovery.
- [**HTTP Load Balancer Polling Feature Flags Sample**](./Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/README.md): Demonstrates how to build a highly available feature flag system in environments like Azure App Service using HTTP polling for discovery and VNet integration for private mesh communication.

---
[⬅ Back to Scenarios Overview](../README.md)