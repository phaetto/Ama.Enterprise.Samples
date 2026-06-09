namespace Ama.Enterprise.Samples.QuicUdpFeatureFlags;

using System;

/// <summary>
/// DTO representing the response returned after successfully setting a feature flag.
/// </summary>
public readonly record struct FlagSetResponse(string Name, bool IsEnabled) : IEquatable<FlagSetResponse>;