namespace Ama.Enterprise.Samples.ZeroTrustRouting.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.P2p.Models.Core;
using Ama.Enterprise.P2p.Services.Core;
using Ama.Enterprise.Samples.ZeroTrustRouting;
using Ama.Enterprise.Samples.ZeroTrustRouting.Models;

/// <summary>
/// Evaluates real-time P2P gossip messages against peer session claims to enforce RBAC and directional geo-fencing.
/// </summary>
public sealed class RbacMeshRoutingPolicy : IMeshRoutingPolicy
{
    private readonly NodeSessionContext context;

    public RbacMeshRoutingPolicy(NodeSessionContext context)
    {
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
    public Task<bool> EvaluateInboundAsync(string meshId, SessionContext session, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meshId);

        if (session.Claims is null ||
            !session.Claims.TryGetValue(Constants.RoleClaimKey, out var remoteRole) ||
            !session.Claims.TryGetValue(Constants.RegionClaimKey, out var remoteRegion))
        {
            return Task.FromResult(false);
        }

        // Inbound flow: Remote peer is sending data to this local node.
        var allowed = IsDataFlowAllowed(
            sourceRole: remoteRole,
            sourceRegion: remoteRegion,
            targetRole: this.context.Role,
            targetRegion: this.context.Region);

        return Task.FromResult(allowed);
    }

    /// <inheritdoc />
    public Task<bool> EvaluateOutboundAsync(string meshId, SessionContext session, IMeshMessage message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meshId);
        ArgumentNullException.ThrowIfNull(message);

        if (session.Claims is null ||
            !session.Claims.TryGetValue(Constants.RoleClaimKey, out var remoteRole) ||
            !session.Claims.TryGetValue(Constants.RegionClaimKey, out var remoteRegion))
        {
            return Task.FromResult(false);
        }

        // Outbound flow: This local node is sending data to the remote peer.
        var allowed = IsDataFlowAllowed(
            sourceRole: this.context.Role,
            sourceRegion: this.context.Region,
            targetRole: remoteRole,
            targetRegion: remoteRegion);

        return Task.FromResult(allowed);
    }

    /// <summary>
    /// Evaluates whether data is permitted to flow from a source node to a target node based on Role and Region claims.
    /// </summary>
    public static bool IsDataFlowAllowed(string sourceRole, string sourceRegion, string targetRole, string targetRegion)
    {
        if (string.IsNullOrWhiteSpace(sourceRole) ||
            string.IsNullOrWhiteSpace(sourceRegion) ||
            string.IsNullOrWhiteSpace(targetRole) ||
            string.IsNullOrWhiteSpace(targetRegion))
        {
            return false;
        }

        var isSourceServer = string.Equals(sourceRole, Constants.ServerRole, StringComparison.OrdinalIgnoreCase);
        var isTargetServer = string.Equals(targetRole, Constants.ServerRole, StringComparison.OrdinalIgnoreCase);

        // Rule 1 (RBAC): Admin and User roles are isolated from each other.
        // Nodes with the 'server' role act as neutral hubs and can exchange with both.
        if (!isSourceServer && !isTargetServer && !string.Equals(sourceRole, targetRole, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Rule 2 (Directional Geo-Fencing): Data can flow from US -> EU, but EU data must never leave EU.
        if (string.Equals(targetRegion, "US", StringComparison.OrdinalIgnoreCase))
        {
            // US nodes only accept data originating from US.
            return string.Equals(sourceRegion, "US", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(targetRegion, "EU", StringComparison.OrdinalIgnoreCase))
        {
            // EU nodes accept data from both EU and US.
            return string.Equals(sourceRegion, "EU", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sourceRegion, "US", StringComparison.OrdinalIgnoreCase);
        }

        // Default fallback for any other custom regions: strict same-region isolation.
        return string.Equals(sourceRegion, targetRegion, StringComparison.OrdinalIgnoreCase);
    }
}