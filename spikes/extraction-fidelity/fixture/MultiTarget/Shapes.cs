namespace MultiTarget;

/// <summary>Present in every target framework.</summary>
public sealed class Always
{
    public string Name { get; set; } = string.Empty;
}

#if NET10_0_OR_GREATER
/// <summary>Only compiled for net10.0. An extractor reading one framework will miss it in the other.</summary>
public sealed class ModernOnly
{
    public required string Value { get; init; }
}
#endif

#if NETSTANDARD2_0
/// <summary>Only compiled for netstandard2.0.</summary>
public sealed class LegacyOnly
{
    public string Value { get; set; } = string.Empty;
}
#endif

#if FEATURE_ALPHA
/// <summary>Gated on a DefineConstants value that only MSBuild evaluation knows about.</summary>
public sealed class AlphaGated
{
    public int Flag { get; set; }
}
#endif
