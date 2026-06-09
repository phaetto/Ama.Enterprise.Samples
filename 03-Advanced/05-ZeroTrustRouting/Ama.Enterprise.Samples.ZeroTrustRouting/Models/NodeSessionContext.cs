namespace Ama.Enterprise.Samples.ZeroTrustRouting.Models;

/// <summary>
/// Encapsulates the local node's identity claims used for P2P session authentication and zero-trust topology routing.
/// </summary>
/// <param name="Role">The role of the node (e.g., <c>user</c>, <c>admin</c>, or <c>server</c>).</param>
/// <param name="Region">The geographic region of the node (e.g., <c>EU</c> or <c>US</c>).</param>
public record NodeSessionContext(string Role, string Region);