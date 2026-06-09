namespace Ama.Enterprise.Samples.QuicUdpFeatureFlags;

using System;

/// <summary>
/// DTO representing the response returned after successfully removing a feature flag.
/// </summary>
public readonly record struct FlagDeleteResponse(string Name, bool Success) : IEquatable<FlagDeleteResponse>;