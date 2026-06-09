namespace Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.Models;

public readonly record struct FlagSetResponse(string Name, bool IsEnabled);