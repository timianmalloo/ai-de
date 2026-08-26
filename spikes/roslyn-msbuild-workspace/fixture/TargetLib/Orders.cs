namespace TargetLib;

/// <summary>Ordinary source, so the spike can also check that real symbols come back.</summary>
public sealed class Order
{
    public Order(string id, Customer customer) => (Id, Customer) = (id, customer);

    public string Id { get; }

    public Customer Customer { get; }

    public decimal Total(IReadOnlyList<OrderLine> lines) => lines.Sum(line => line.Amount);
}

public sealed record Customer(string Id, string Name);

public sealed record OrderLine(string Sku, decimal Amount);
