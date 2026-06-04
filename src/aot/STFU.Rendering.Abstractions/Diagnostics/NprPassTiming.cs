namespace STFU.Rendering.Abstractions.Diagnostics;

public sealed record NprPassTiming(
    string Name,
    double Milliseconds,
    string? Notes = null);
