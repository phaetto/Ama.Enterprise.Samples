# QUIC/UDP Feature Flags Sample (Local Network)

This sample demonstrates how to build a highly secure, decentralized, cluster-aware console application using **QUIC Transports** and **UDP Peer Discovery**. It embeds the `Ama.Enterprise.FeatureFlags` and CRDT core engines directly into an interactive console application.

## The Local Network Advantage

In controlled local environments or custom overlay networks (like VPNs or direct fiber connections), we can leverage powerful network primitives that are often blocked in public clouds:

1. **UDP Multicast Discovery:** Replace static endpoints and central registries with zero-configuration peer discovery by broadcasting presence directly to the local subnet.
2. **High-Performance Transports:** Utilize QUIC for next-generation and highly efficient peer-to-peer data synchronization.

## Architecture Highlights

This sample explicitly defines a Console Application that achieves the following:

- **QUIC Transport Integration:** Uses `.AddQuicTransport()` to negotiate fast streams supported by modern OSs.
- **UDP Multicast Peer Discovery:** Resolves peers automatically via UDP multicast packets on `239.255.0.1`.
- **Advanced Security Boundaries:** 
  - **mTLS:** Transport-level validation using X509 certificates to ensure only nodes with trusted thumbprints can connect.
  - **Certificate Authentication:** Application-level handshakes using separate authentication certificates.
  - **Wire Encryption:** Symmetric payload encryption wrapping the core synchronization messages.

## Code Breakdown

### 1. Mesh Configuration

The cluster utilizes native extensions to wire up QUIC and UDP configurations alongside deep security requirements.

```csharp
services.AddP2pMesh("feature-flags-quic-mesh")
    .AddGossipNetwork(options => { ... })
    // High performance multiplexed transport using QUIC (mTLS)
    .AddQuicTransport(options => {
        options.ListenPort = currentPort;
        options.ServerCertificate = clusterCert;
        options.RemoteCertificateValidationCallback = ...; // Validate Thumbprints
    })
    // Zero-conf discovery via UDP Multicast
    .AddUdpPeerDiscovery(options => {
        options.MulticastAddress = "239.255.0.1";
        options.MulticastPort = 54321;
    })
    // Secure cluster joining handshakes
    .AddCertificateAuthenticator(options => {
        options.LocalCertificateBytes = authCertBytes;
        options.AllowedThumbprints.Add(authCert.Thumbprint);
    })
    // Encrypt the actual CRDT payloads over the wire
    .AddWireEncoder(options => {
        options.IsEncryptionEnabled = true;
        options.EncryptionKeyBase64 = encryptionKey;
    });
```

### 2. Interactive Console Application

Unlike the Web API examples, this application uses a custom `InteractiveConsoleApp` boilerplate to display real-time, eventually-consistent cluster updates as nodes connect, disconnect, and share state changes.

```csharp
// Redraw the console UI when the state changes.
clusterManager.StateChanged += (sender, eventArgs) =>
{
    app.RequestRedraw();
};
```

## Running the Sample

### Prerequisites

QUIC requires native OS support (e.g., `libmsquic` on Linux, native support on Windows 11/Windows Server 2022).

### Development Mode

1. Open your terminal in the `Ama.Enterprise.Samples.QuicUdpFeatureFlags` directory.
2. Start the first node:
   ```bash
   dotnet run
   ```
3. Use `clone` - spawns a new node process to join the mesh network.
4. Type commands in the interactive prompt to mutate state:
   - `set MyFlag true`
   - `del MyFlag`
   
Watch the changes instantly synchronize across terminal windows!

---
[⬅ Back to Local Network Scenarios](../README.md)