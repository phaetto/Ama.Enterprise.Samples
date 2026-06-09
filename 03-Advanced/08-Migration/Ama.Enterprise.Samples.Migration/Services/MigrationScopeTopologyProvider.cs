namespace Ama.Enterprise.Samples.Migration.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Services;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.P2p.Models.Core;
using Ama.Enterprise.P2p.Services.Core;
using Ama.Enterprise.Samples.Migration.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Partitioned topology provider for the Live Migration Mediator node that isolates V1 and V2 CRDT scopes
/// so V1 replicas only communicate with V1 mesh peers and V2 replicas only communicate with V2 mesh peers.
/// </summary>
public sealed class MigrationScopeTopologyProvider : IScopeTopologyProvider
{
    private readonly ReplicaContext replicaContext;
    private readonly IPeerRegistry peerRegistry;
    private readonly IDictionary<string, string> replicaToMeshMap;
    private readonly ILogger<MigrationScopeTopologyProvider> logger;

    public MigrationScopeTopologyProvider(
        ReplicaContext replicaContext,
        IPeerRegistry peerRegistry,
        IEnumerable<ReplicaMeshMapping> mappings,
        ILogger<MigrationScopeTopologyProvider> logger)
    {
        this.replicaContext = replicaContext ?? throw new ArgumentNullException(nameof(replicaContext));
        this.peerRegistry = peerRegistry ?? throw new ArgumentNullException(nameof(peerRegistry));
        ArgumentNullException.ThrowIfNull(mappings);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.replicaToMeshMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (mapping is not null &&
                !string.IsNullOrWhiteSpace(mapping.ReplicaId) &&
                !string.IsNullOrWhiteSpace(mapping.MeshName))
            {
                this.replicaToMeshMap[mapping.ReplicaId] = mapping.MeshName;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsPeerExpectedAsync(string peerNetworkId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(peerNetworkId) || !Guid.TryParse(peerNetworkId, out var peerGuid))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.replicaContext.ReplicaId) ||
            !this.replicaToMeshMap.TryGetValue(this.replicaContext.ReplicaId, out var expectedMeshName))
        {
            return false;
        }

        try
        {
            var activePeers = await this.peerRegistry.GetPeersByStatusAsync(expectedMeshName, PeerStatus.Active, cancellationToken).ConfigureAwait(false);
            return activePeers.Any(peer => peer.Id.Value == peerGuid);
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Failed to verify whether peer '{PeerNetworkId}' belongs to mesh '{MeshName}' for replica '{ReplicaId}'.",
                peerNetworkId,
                expectedMeshName,
                this.replicaContext.ReplicaId);
            return false;
        }
    }
}