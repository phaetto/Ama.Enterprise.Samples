namespace Ama.Enterprise.Samples.ZeroTrustRouting.Services;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.P2p.Models.Core;
using Ama.Enterprise.P2p.Services.Core;
using Ama.Enterprise.Samples.ZeroTrustRouting;
using Ama.Enterprise.Samples.ZeroTrustRouting.Models;

/// <summary>
/// Generates local handshake tokens and validates incoming peer tokens to establish authenticated <see cref="SessionContext"/> claims.
/// </summary>
public sealed class RbacSessionTokenValidator : ISessionTokenValidator
{
    private readonly NodeSessionContext context;

    public RbacSessionTokenValidator(NodeSessionContext context)
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
    public Task<ReadOnlyMemory<byte>> GetLocalTokenBytesAsync(string meshId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meshId);

        // In production, this would be a signed JWT or cryptographic token.
        // For demonstration clarity, we encode "role|region" as UTF-8 bytes.
        var tokenPayload = $"{this.context.Role}|{this.context.Region}";
        return Task.FromResult<ReadOnlyMemory<byte>>(Encoding.UTF8.GetBytes(tokenPayload));
    }

    /// <inheritdoc />
    public Task<SessionContext?> ValidateTokenAsync(string meshId, ReadOnlyMemory<byte> tokenData, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(meshId);

        if (tokenData.IsEmpty)
        {
            return Task.FromResult<SessionContext?>(null);
        }

        var tokenString = Encoding.UTF8.GetString(tokenData.Span);
        var parts = tokenString.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return Task.FromResult<SessionContext?>(null);
        }

        var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { Constants.RoleClaimKey, parts[0] },
            { Constants.RegionClaimKey, parts[1] }
        };

        var session = new SessionContext(
            Guid.NewGuid().ToString(),
            claims,
            DateTimeOffset.UtcNow.AddHours(24));

        return Task.FromResult<SessionContext?>(session);
    }
}