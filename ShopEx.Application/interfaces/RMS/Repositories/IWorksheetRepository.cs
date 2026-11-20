using ShopEx.Domain.RMSEntity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface IWorksheetRepository
    {
        Task<Worksheet?> GetByIdAsync(int worksheetId, CancellationToken cancellationToken = default);
        Task<List<Worksheet>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddWorkSheetAsync(Worksheet worksheet, CancellationToken cancellationToken = default);
        Task AddLinesAsync(IEnumerable<Worksheet_PurchaseOrder> lines, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
