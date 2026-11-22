using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ShopContext _db;

        public UnitOfWork(ShopContext db)
        {
            _db = db;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return _db.SaveChangesAsync(ct);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction != null)
            {
                await work();
                return;
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await work();
            await tx.CommitAsync(ct);
        }
    }
}
