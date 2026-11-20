using ShopEx.Application.DTOs.RMS;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Service
{
    public interface IWorksheetService
    {
        Task<int> OpenWorksheetAsync(OpenWorksheetDto dto, CancellationToken cancellationToken = default);
        Task AddPurchasesAsync(int worksheetId, List<PurchaseLineDto> items, CancellationToken cancellationToken = default);
        Task<int> OpenWorksheetWithPurchasesAsync(OpenWorksheetDto dto, List<PurchaseLineDto> items, CancellationToken cancellationToken = default);
        Task ApproveWorksheetAsync(int worksheetId, CancellationToken cancellationToken = default);
        Task<WorksheetResponse?> GetByIdAsync(int worksheetId, CancellationToken cancellationToken = default);
        Task<List<WorksheetResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
