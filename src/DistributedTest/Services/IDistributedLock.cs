namespace DistributedTest.Services;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string lockNamespace, CancellationToken cancellationToken);
}