namespace HostileProject;

/// <summary>Ordinary source, so the project is a real C# project a user would plausibly open.</summary>
public sealed class Invoice
{
    public string Number { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
