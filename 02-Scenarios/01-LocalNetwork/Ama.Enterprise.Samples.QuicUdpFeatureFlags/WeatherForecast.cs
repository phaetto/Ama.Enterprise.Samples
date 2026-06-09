namespace Ama.Enterprise.Samples.QuicUdpFeatureFlags;

using System;

/// <summary>
/// Represents a weather forecast sample data structure.
/// </summary>
public readonly record struct WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) : IEquatable<WeatherForecast>
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}