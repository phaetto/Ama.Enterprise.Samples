namespace Ama.Enterprise.Samples.Migration.Models;

using System;

/// <summary>
/// Maps a local CRDT replica scope identifier to its dedicated P2P mesh network identifier.
/// </summary>
public sealed class ReplicaMeshMapping : IEquatable<ReplicaMeshMapping>
{
    /// <summary>
    /// Gets the CRDT replica scope identifier (e.g., <c>node-v1-8100</c> or <c>node-v2-8100</c>).
    /// </summary>
    public string ReplicaId { get; }

    /// <summary>
    /// Gets the P2P mesh network identifier bound to this replica scope.
    /// </summary>
    public string MeshName { get; }

    public ReplicaMeshMapping(string replicaId, string meshName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(meshName);

        this.ReplicaId = replicaId;
        this.MeshName = meshName;
    }

    public bool Equals(ReplicaMeshMapping? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(this.ReplicaId, other.ReplicaId, StringComparison.Ordinal) &&
               string.Equals(this.MeshName, other.MeshName, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) =>
        obj is ReplicaMeshMapping other && this.Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(this.ReplicaId),
            StringComparer.Ordinal.GetHashCode(this.MeshName));
}