# QUIC/UDP Feature Flags Sample (Local Network)

This sample shows how to build a fast, secure, decentralized console application using QUIC transport and UDP multicast peer discovery.

While the [Beginner TCP/UDP Sample](../../../01-Beginner/README.md) focuses on the basics without encryption, this sample demonstrates how to lock down a local cluster with multi-layered security and modern QUIC networking.

## Why Use QUIC and UDP on a Local Network?

When you control your local network (such as an on-premise LAN, office network, or private VPN), you can use network features that cloud providers usually block:

1. Zero-config UDP discovery: Nodes broadcast their presence to a local multicast address (`239.255.0.1`). New nodes join the cluster automatically without needing central registries or hardcoded IP lists.
2. Modern QUIC transport: QUIC provides multiplexed streams over UDP with built-in TLS 1.3 encryption, reducing connection overhead and latency between peers.

## Key Features in This Sample

- QUIC transport (`.AddQuicTransport`): High-speed, stream-multiplexed peer-to-peer communication.
- UDP multicast discovery (`.AddUdpPeerDiscovery`): Automatic local peer detection on `239.255.0.1:54321`.
- Three layers of security:
  1. Transport mTLS: Validates the peer's X.509 transport certificate (`cluster.pfx`) during the QUIC connection handshake.
  2. Peer certificate authentication (`.AddCertificateAuthenticator`): Verifies node identity during the P2P protocol handshake (`auth.pfx`).
  3. Symmetric wire encryption (`.AddWireEncoder`): Encrypts all gossip and CRDT messages with an AES-GCM shared key (`encryption.key`).
- Real-time console UI (`IFeatureFlagStateObserver`): Automatically redraws the terminal screen whenever feature flags change on any node.

---

## Code Walkthrough

### 1. Configuring the Secure QUIC Mesh

In `Program.cs`, the mesh is configured with QUIC transport, UDP discovery, certificate authentication, and payload encryption:

```csharp
services.AddP2pMesh("feature-flags-quic-mesh")
    .AddGossipNetwork(options =>
    {
        options.GossipInterval = TimeSpan.FromMilliseconds(1500);
        options.Fanout = 3;
        options.DefaultTimeToLive = 3;
    })
    // 1. High-performance multiplexed transport using QUIC + mTLS
    .AddQuicTransport(options =>
    {
        options.ListenPort = currentPort;
        options.ListenHost = "127.0.0.1";
        options.ServerCertificate = clusterCert;
        options.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (cert is null) return false;
            var remoteThumbprint = cert.GetCertHashString();
            return string.Equals(remoteThumbprint, clusterCert.Thumbprint, StringComparison.OrdinalIgnoreCase);
        };
    })
    // 2. Zero-configuration peer discovery via UDP Multicast
    .AddUdpPeerDiscovery(options =>
    {
        options.MulticastAddress = "239.255.0.1";
        options.MulticastPort = 54321;
        options.DiscoveryInterval = TimeSpan.FromSeconds(1);
        options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
    })
    .AddUdpPeerHandshake(options =>
    {
        options.ListenPort = handshakePort;
    })
    // 3. Authenticate peers joining the cluster
    .AddCertificateAuthenticator(options =>
    {
        options.LocalCertificateBytes = authCertBytes;
        options.AllowedThumbprints.Add(authCert.Thumbprint);
        options.ValidateCertificateChain = false;
    })
    // 4. Encrypt all CRDT payloads sent over the wire
    .AddWireEncoder(options =>
    {
        options.IsEncryptionEnabled = true;
        options.EncryptionKeyBase64 = encryptionKey;
    });
```

### 2. Observing Cluster State Changes

The sample registers `ConsoleFeatureFlagObserver` (from `Ama.Enterprise.Samples.Common`) as an `IFeatureFlagStateObserver`. Whenever a flag is added, changed, or deleted on any node in the mesh, the observer tells the console app to refresh its display:

```csharp
services.AddSingleton<InteractiveConsoleApp>();
services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
services.AddFeatureFlags();
```

---

## Running the Sample

### Prerequisites
- .NET 10 SDK
- OS QUIC support:
  - Windows: Supported natively on Windows 11 and Windows Server 2022+.
  - Linux / macOS: Install `libmsquic` via your package manager (for example, `sudo apt install libmsquic` on Ubuntu/Debian).
- Sample certificates: The project includes pre-generated test files (`cluster.pfx`, `auth.pfx`, and `encryption.key`) that are copied to the output folder automatically on build.

### Start a Local Cluster

1. Open a terminal in the `02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags` folder.
2. Start the first node:
   ```bash
   dotnet run
   ```
3. Type `clone` and press Enter to launch a second node in a new terminal window.
4. Try changing flags from either window:
   - `set EnableNewCheckout true`
   - `del EnableNewCheckout`

Watch the state sync securely across all open terminals!

---
[⬅ Back to Local Network Scenarios](../README.md)