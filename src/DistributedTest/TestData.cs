namespace DistributedTest;

public sealed record TestData
{
    public long Id { get; init; }

    public string Value { get; init; } = null!;

    public int Number { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}