# 05 - Zero-Trust Routing & Session-Based Topology Partitioning

In many enterprise deployments, nodes share the same physical network or discovery mechanism (such as UDP multicast, DNS, or a load balancer), yet not every peer should be allowed to exchange every piece of data.

For example:
- Role-Based Access Control (RBAC): `admin` replicas and `user` replicas may run on the same local network, but `admin` configuration state must never leak to `user` nodes.
- Directional Geo-Fencing / Data Sovereignty (e.g., GDPR): Global state created in `US` may be permitted to replicate into `EU` nodes, while data created inside `EU` must remain strictly within `EU` boundaries and never flow back to `US`.

This sample demonstrates how to combine P2P Session Authentication, Zero-Trust Mesh Routing Policies, and CRDT Scope Topology Providers over standard TCP/UDP to dynamically partition a shared mesh based on peer identity claims.

---

## Architecture Overview

Even though all nodes in this sample discover each other on the exact same UDP multicast group (`239.255.0.1:8035`), data synchronization is governed by three cooperating layers:

```text
+-----------------------------------------------------------------------+
| 1. P2P Handshake: ISessionTokenValidator (RbacSessionTokenValidator)  |
|    Exchanges tokens during UDP handshake and populates SessionContext |
|    claims: { "role": "user|admin|server", "region": "EU|US" }         |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
| 2. Real-Time Gossip: IMeshRoutingPolicy (RbacMeshRoutingPolicy)       |
|    Intercepts every inbound and outbound gossip message. Drops live   |
|    broadcasts if role/region rules prohibit data flow.                |
+-----------------------------------------------------------------------+
                                   |
                                   v
+-----------------------------------------------------------------------+
| 3. Background Sync: IScopeTopologyProvider (RbacScopeTopologyProvider)|
|    Queries IPeerSessionRegistry so the CRDT Anti-Entropy worker and   |
|    Dotted Version Vector (DVV) only pull missing state from peers     |
|    that are legally permitted to send data to this node.              |
+-----------------------------------------------------------------------+
```

### Why Both `IMeshRoutingPolicy` and `IScopeTopologyProvider`?

- `IMeshRoutingPolicy` operates at the P2P transport and gossip layer. It prevents live delta operations from being broadcast to or accepted from unauthorized peers in real time.
- `IScopeTopologyProvider` operates at the CRDT orchestration layer. Distributed CRDTs periodically run background Anti-Entropy (comparing Dotted Version Vectors to heal missed operations). By implementing `IScopeTopologyProvider`, the CRDT engine knows which peers belong to this node's logical partition and avoids requesting state from peers outside its authorization boundary.

---

## Key Components in This Sample

### 1. `RbacSessionTokenValidator` (`ISessionTokenValidator`)
During the P2P handshake, each node calls `GetLocalTokenBytesAsync` to present its identity token and `ValidateTokenAsync` to verify the remote peer's token and construct a `SessionContext` containing verified claims:

```csharp
var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { Constants.RoleClaimKey, parts[0] },
    { Constants.RegionClaimKey, parts[1] }
};

var session = new SessionContext(
    Guid.NewGuid().ToString(),
    claims,
    DateTimeOffset.UtcNow.AddHours(24));
```

### 2. `RbacMeshRoutingPolicy` (`IMeshRoutingPolicy`)
Enforces two rules on every inbound (`EvaluateInboundAsync`) and outbound (`EvaluateOutboundAsync`) gossip message:
1. RBAC Isolation: `admin` nodes and `user` nodes cannot exchange data directly. Nodes with the `server` role act as neutral hubs that can communicate with both `admin` and `user` roles.
2. Directional Geo-Fencing: Data can flow from `US -> EU` (and within the same region), but `EU -> US` is blocked so `EU` data never leaves the `EU` region.

### 3. `RbacScopeTopologyProvider` (`IScopeTopologyProvider`)
Injects `IPeerSessionRegistry` to look up a peer's authenticated `SessionContext` by its `PeerId` and applies the exact same `IsDataFlowAllowed` rule when the CRDT Anti-Entropy worker asks `IsPeerExpectedAsync(peerNetworkId)`.

### 4. Wiring It Up in DI (`Program.cs`)

```csharp
// Replace the default CRDT topology provider with the session-aware implementation
builder.Services.Replace(ServiceDescriptor.Scoped<IScopeTopologyProvider, RbacScopeTopologyProvider>());

// Enable Session Authentication and Zero-Trust Routing on the P2P mesh
builder.Services.AddP2pMesh(Constants.MeshName)
    .AddSessionAuthentication<RbacSessionTokenValidator>(options =>
    {
        options.RequireSession = true;
    })
    .EnableZeroTrustRouting()
    .AddRoutingPolicy<RbacMeshRoutingPolicy>()
    // ... TCP transport & UDP discovery ...
```

---

## Step-by-Step Walkthrough

You can pass `<port> <role> <region>` on the command line, or use the built-in `spawn <role> <region>` command inside any running node's console.

### Scenario A: Testing RBAC Isolation (`user` vs `admin` in `EU`)

1. Start a `user` node in `EU` on port `8100`:

   ```bash
   cd 03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting
   dotnet run -- 8100 user EU
   ```

2. From inside that terminal, spawn an `admin` node in `EU` and another `user` node in `EU`:

   ```text
   > spawn admin EU
   > spawn user EU
   ```

   (Or run `dotnet run -- 8101 admin EU` and `dotnet run -- 8102 user EU` in separate terminals.)

3. On the first `user EU` node (`8100`), set a feature flag:

   ```text
   > set UserDashboard true
   ```

   - Result: The second `user EU` node (`8102`) immediately receives `UserDashboard: ENABLED`.
   - Isolation: The `admin EU` node (`8101`) remains completely empty, even though it discovered both user nodes over UDP multicast!

4. On the `admin EU` node (`8101`), set an admin flag:

   ```text
   > set AdminAuditLog true
   ```

   - Result: `AdminAuditLog` stays strictly on `admin` nodes and never leaks to the `user` nodes.

---

### Scenario B: Testing Directional Geo-Fencing (`US -> EU` Allowed, `EU -> US` Blocked)

1. From your `user EU` node (`8100`), spawn a `user US` node:

   ```text
   > spawn user US
   ```

2. On the `user US` node, create a global flag:

   ```text
   > set UsGlobalBanner true
   ```

   - Result: Because `US -> EU` data flow is permitted, both the `user US` node and your `user EU` nodes synchronize `UsGlobalBanner: ENABLED`.

3. Note that `UserDashboard` (which was created earlier on the `user EU` node) was never sent to the `user US` node!

4. Create another flag on the `user EU` node:

   ```text
   > set EuGdprConsent true
   ```

   - Result: `EuGdprConsent` synchronizes across all `user EU` nodes, but is blocked from flowing to the `user US` node.

---
[⬅ Back to Advanced Overview](../README.md)