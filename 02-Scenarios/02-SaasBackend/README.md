# SaaS Backend Integration

When deploying applications to cloud platforms (like Azure App Service, AWS ECS/Fargate, Google Cloud Run, or Kubernetes), local networking features like UDP multicast are usually blocked.

These samples show how to run a decentralized P2P mesh inside standard cloud backends using regular HTTP traffic and cloud-native peer discovery.

## Key Concepts
- ASP.NET Core (Kestrel) integration: Runs P2P mesh traffic over standard HTTP/HTTPS alongside your regular REST API endpoints.
- Cloud-friendly peer discovery: Finds other running instances using DNS `A`/`AAAA` records (great for Kubernetes) or HTTP load balancer polling (great for Azure App Service).
- Durable state and Native AOT: Shows how to pair in-memory CRDT sync with cloud storage (such as Azure Table Storage) and Native AOT compilation.

## Included Samples

- [HTTP / DNS Feature Flags](./Ama.Enterprise.Samples.HttpDnsFeatureFlags/README.md): An ASP.NET Core Minimal API that syncs feature flags over HTTP and discovers peers via DNS lookups (ideal for Kubernetes Headless Services and Docker Compose).
- [HTTP Load Balancer Polling Feature Flags](./Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/README.md): Designed for Azure App Service. Discovers peers by polling the app's HTTP load balancer, syncs directly over a private VNet, and saves checkpoints to Azure Table Storage. Includes a full Bicep template for Azure deployment.

---
[⬅ Back to Scenarios Overview](../README.md)