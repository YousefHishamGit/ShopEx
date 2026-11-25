using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ShopEx.Infrastructure.Persistence
{
    /// <summary>
    /// SQL Server application lock owned by the current transaction (released on commit / rollback).
    /// Serializes "read then write" sections such as payments, balance closings and document numbering.
    /// </summary>
    public static class SqlAppLock
    {
        public const int DefaultTimeoutMs = 10000;

        public static async Task AcquireAsync(DbContext db, string resource, CancellationToken ct = default, int timeoutMs = DefaultTimeoutMs)
        {
            if (db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("An application lock needs an open transaction.");

            var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
            await db.Database.ExecuteSqlRawAsync(
                "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout",
                new object[]
                {
                    result,
                    new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource },
                    new SqlParameter("@timeout", SqlDbType.Int) { Value = timeoutMs }
                },
                ct);

            // 0 = granted, 1 = granted after waiting; negative = timeout / cancelled / deadlock / error.
            if (result.Value is not int code || code < 0)
                throw new InvalidOperationException("النظام مشغول بعملية أخرى على نفس البيانات، حاول مرة أخرى بعد لحظات.");
        }
    }
}
