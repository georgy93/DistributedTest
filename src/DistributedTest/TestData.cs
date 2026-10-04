namespace DistributedTest;

public sealed class TestData
{
    public long Id { get; set; }

    public string Value { get; set; } = null!;

    public int Number { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}