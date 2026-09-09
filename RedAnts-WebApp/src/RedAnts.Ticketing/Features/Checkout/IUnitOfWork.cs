namespace RedAnts.Ticketing.Features.Checkout;

public interface IUnitOfWork
{
    Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default);

    Task RunAsync(Func<Task> work, CancellationToken cancellationToken = default);
}
