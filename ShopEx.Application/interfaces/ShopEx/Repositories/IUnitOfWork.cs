using System;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default);

        /// <summary>
        /// Exclusive lock on <paramref name="resource"/> held until the current transaction ends.
        /// Call it first inside <see cref="ExecuteInTransactionAsync"/>, then (re)read the data you validate,
        /// so two requests on the same record (double click, two cashiers) run one after the other.
        /// Throws <see cref="InvalidOperationException"/> if the lock is not granted in time.
        /// </summary>
        Task AcquireLockAsync(string resource, CancellationToken ct = default);
    }
}
