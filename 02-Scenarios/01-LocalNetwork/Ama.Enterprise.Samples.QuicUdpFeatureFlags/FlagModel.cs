namespace Ama.Enterprise.Samples.QuicUdpFeatureFlags;

using System;

/// <summary>
/// DTO representing a feature flag's current state.
/// </summary>
public readonly record struct FlagModel(string Name, bool IsEnabled) : IEquatable<FlagModel>;