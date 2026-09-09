using Umbraco.Cms.Core.Scoping;

namespace RedAnts.Ticketing.Features.Checkout.Infrastructure;

public sealed class ScopedUnitOfWork(ICoreScopeProvider scopeProvider, ILogger<ScopedUnitOfWork> logger) : IUnitOfWork
{
    private const string RolledBack = "Die Änderung wurde vollständig zurückgerollt und nicht gespeichert.";

    public async Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default)
    {
        using var scope = scopeProvider.CreateCoreScope();
        var result = await work();
        cancellationToken.ThrowIfCancellationRequested();
        if (scope.Complete()) return result;

        logger.LogError("A nested scope did not complete, so the unit of work was rolled back.");
        throw new DomainException(RolledBack);
    }

    public async Task RunAsync(Func<Task> work, CancellationToken cancellationToken = default) =>
        await RunAsync<object?>(async () =>
        {
            await work();
            return null;
        }, cancellationToken);
}
