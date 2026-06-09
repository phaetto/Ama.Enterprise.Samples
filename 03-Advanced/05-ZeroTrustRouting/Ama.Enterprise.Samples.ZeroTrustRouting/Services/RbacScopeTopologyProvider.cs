namespace Ama.Enterprise.Samples.ZeroTrustRouting.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.P2p.Models.Core;
using Ama.Enterprise.P2p.Services.Core;
using Ama.Enterprise.Samples.ZeroTrustRouting;
using Ama.Enterprise.Samples.ZeroTrustRouting.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aligns background CRDT Anti-Entropy and Dotted Version Vector (DVV) tracking with the active session topology.
/// </summary>
public sealed class RbacScopeTopologyProvider : IScopeTopologyProvider
{
    private readonly IPeerSessionRegistry sessionRegistry;
    private readonly NodeSessionContext context;
    private readonly ILogger<RbacScopeTopologyProvider> logger;

    public RbacScopeTopologyProvider(
        IPeerSessionRegistry sessionRegistry,
        NodeSessionContext context,
        ILogger<RbacScopeTopologyProvider> logger)
    {
        this.sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(context.Role))
        {
            throw new ArgumentException("Node role cannot be null or whitespace.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.Region))
        {
            throw new ArgumentException("Node region cannot be null or whitespace.", nameof(context));
        }

        this.context = context;
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsPeerExpectedAsync(string peerNetworkId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(peerNetworkId) || !Guid.TryParse(peerNetworkId, out var peerGuid))
        {
            return false;
        }

        var peerId = new PeerId(peerGuid);

        SessionContext? session;
        try
        {
            session = await this.sessionRegistry.GetSessionAsync(Constants.MeshName, peerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to resolve session context for peer '{PeerId}'.", peerId);
            return false;
        }

        if (!session.HasValue || session.Value.Claims is null)
        {
            return false;
        }

        var claims = session.Value.Claims;
        if (!claims.TryGetValue(Constants.RoleClaimKey, out var remoteRole) ||
            !claims.TryGetValue(Constants.RegionClaimKey, out var remoteRegion))
        {
            return false;
        }

        // Determine if this local node expects to pull/receive state from the remote peer during Anti-Entropy.
        return RbacMeshRoutingPolicy.IsDataFlowAllowed(
            sourceRole: remoteRole,
            sourceRegion: remoteRegion,
            targetRole: this.context.Role,
            targetRegion: this.context.Region);
    }
}