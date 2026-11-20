using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.RMS.Service;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Domain.RMSEntity;
using ShopEx.Domain.RMSEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.RMS
{
    public class WorksheetService : IWorksheetService
    {
        private readonly IWorksheetRepository _repo;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUnitOfWork _shopUnitOfWork;

        private const int PoPlannerStyle = 340;
        private const int StatusOpen = (int)RMSWorksheetStatus.PendingApproval; // 1
        private const int StatusApproved = (int)RMSWorksheetStatus.Approved; // 2

        public WorksheetService(
            IWorksheetRepository worksheetRepository,
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IUnitOfWork shopUnitOfWork)
        {
            _repo = worksheetRepository ?? throw new ArgumentNullException(nameof(worksheetRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _shopUnitOfWork = shopUnitOfWork ?? throw new ArgumentNullException(nameof(shopUnitOfWork));
        }

        public async Task<int> OpenWorksheetAsync(OpenWorksheetDto dto, CancellationToken cancellationToken = default)
        {
            var ws = CreateWorksheet(dto);
            await _repo.AddWorkSheetAsync(ws, cancellationToken);
            await _repo.SaveAsync(cancellationToken);
            return ws.ID;
        }

        public async Task AddPurchasesAsync(int worksheetId, List<PurchaseLineDto> items, CancellationToken cancellationToken = default)
        {
            if (worksheetId <= 0)
                throw new ArgumentException("Invalid worksheet ID.", nameof(worksheetId));

            if (items == null || items.Count == 0)
                throw new ArgumentException("At least one purchase order item is required.", nameof(items));

            var ws = await _repo.GetByIdAsync(worksheetId, cancellationToken);
            if (ws == null)
                throw new KeyNotFoundException($"Worksheet with ID {worksheetId} was not found.");

            if (ws.Status != StatusOpen)
                throw new InvalidOperationException($"Cannot add purchase orders to worksheet {worksheetId} because its status is {ws.Status} (only open worksheets with status 1 can be modified).");

            var lines = MapLines(worksheetId, items);
            await _repo.AddLinesAsync(lines, cancellationToken);
            await _repo.SaveAsync(cancellationToken);

            // Deduct stock from default warehouse
            await DeductStockForLinesAsync(worksheetId, ws.Title, items, cancellationToken);
        }

        public async Task<int> OpenWorksheetWithPurchasesAsync(OpenWorksheetDto dto, List<PurchaseLineDto> items, CancellationToken cancellationToken = default)
        {
            var ws = CreateWorksheet(dto);
            if (items != null && items.Count > 0)
            {
                ws.Worksheet_PurchaseOrders = MapLines(0, items).ToList();
            }

            await _repo.AddWorkSheetAsync(ws, cancellationToken);
            await _repo.SaveAsync(cancellationToken);

            if (items != null && items.Count > 0)
            {
                // Deduct stock from default warehouse
                await DeductStockForLinesAsync(ws.ID, ws.Title, items, cancellationToken);
            }

            return ws.ID;
        }

        public async Task ApproveWorksheetAsync(int worksheetId, CancellationToken cancellationToken = default)
        {
            if (worksheetId <= 0)
                throw new ArgumentException("Invalid worksheet ID.", nameof(worksheetId));

            var ws = await _repo.GetByIdAsync(worksheetId, cancellationToken);
            if (ws == null)
                throw new KeyNotFoundException($"Worksheet with ID {worksheetId} was not found.");

            if (ws.Status != StatusOpen)
                throw new InvalidOperationException($"Worksheet {worksheetId} cannot be approved because its status is {ws.Status} (only pending worksheets with status 1 can be approved).");

            ws.Status = StatusApproved; // Status converts to 2
            await _repo.SaveAsync(cancellationToken);
        }

        public async Task<WorksheetResponse?> GetByIdAsync(int worksheetId, CancellationToken cancellationToken = default)
        {
            if (worksheetId <= 0)
                return null;

            var ws = await _repo.GetByIdAsync(worksheetId, cancellationToken);
            if (ws == null)
                return null;

            return MapToResponse(ws);
        }

        public async Task<List<WorksheetResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = await _repo.GetAllAsync(cancellationToken);
            return list.Select(MapToResponse).ToList();
        }

        private async Task DeductStockForLinesAsync(int worksheetId, string? title, List<PurchaseLineDto> items, CancellationToken cancellationToken)
        {
            if (items == null || items.Count == 0) return;

            var defaultWh = await _warehouseRepository.GetDefaultAsync(cancellationToken);
            if (defaultWh == null)
                throw new InvalidOperationException("لم يتم العثور على المخزن الافتراضي لصرف الأصناف.");

            foreach (var item in items)
            {
                var balance = await _stockRepository.GetBalanceAsync(defaultWh.ID, item.ItemId, cancellationToken);
                var requestedQty = (decimal)item.Qty;

                if (balance == null || balance.QuantityOnHand < requestedQty)
                {
                    var available = balance?.QuantityOnHand ?? 0;
                    throw new InvalidOperationException(
                        $"الرصيد المتاح في المخزن ({defaultWh.Name}) للصنف {item.ItemId} هو ({available})، وهو غير كافٍ للكمية المطلوبة في ورقة العمل ({requestedQty}).");
                }

                balance.QuantityOnHand -= requestedQty;
                balance.LastUpdatedAt = DateTime.Now;
                _stockRepository.UpdateBalance(balance);

                var movement = new StockMovement
                {
                    WarehouseID = defaultWh.ID,
                    RMSItemID = item.ItemId,
                    MovementType = StockMovementType.TransferOut,
                    Quantity = requestedQty,
                    UnitCost = item.Price > 0 ? (decimal)item.Price : balance.AverageCost,
                    MovementDate = DateTime.Now,
                    ReferenceType = "RMSWorksheet",
                    ReferenceID = worksheetId,
                    Notes = $"صرف ورقة عمل رقم {worksheetId} - {title}",
                    CreatedBy = "System"
                };
                await _stockRepository.AddMovementAsync(movement, cancellationToken);
            }

            await _shopUnitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static WorksheetResponse MapToResponse(Worksheet ws)
        {
            return new WorksheetResponse
            {
                ID = ws.ID,
                Style = ws.Style,
                EffectiveDate = ws.EffectiveDate,
                Status = ws.Status,
                StatusName = ws.Status.HasValue && Enum.IsDefined(typeof(RMSWorksheetStatus), ws.Status.Value)
                    ? ((RMSWorksheetStatus)ws.Status.Value).ToString()
                    : ws.Status?.ToString(),
                Notes = ws.Notes,
                Title = ws.Title,
                FromDate = ws.FromDate,
                PurchaseOrders = ws.Worksheet_PurchaseOrders?.Select(po => new WorksheetPurchaseOrderResponse
                {
                    ID = po.ID,
                    WorksheetID = po.WorksheetID,
                    ItemDescription = po.ItemDescription,
                    QuantityOrdered = po.QuantityOrdered,
                    ItemID = po.ItemID,
                    DetailID = po.DetailID,
                    OrderNumber = po.OrderNumber,
                    Price = po.Price,
                    StoreID = po.StoreID,
                    TaxRate = po.TaxRate
                }).ToList() ?? new List<WorksheetPurchaseOrderResponse>()
            };
        }

        private static Worksheet CreateWorksheet(OpenWorksheetDto dto) => new()
        {
            Style = PoPlannerStyle,
            EffectiveDate = DateTime.Now,
            FromDate = DateTime.Now,
            Status = StatusOpen, // Status is 1 (PendingApproval)
            Title = string.IsNullOrWhiteSpace(dto?.Title) ? "PO Planner from" : dto.Title,
            Notes = dto?.Notes
        };

        private static IEnumerable<Worksheet_PurchaseOrder> MapLines(int worksheetId, List<PurchaseLineDto> items) =>
            items.Select(i => new Worksheet_PurchaseOrder
            {
                WorksheetID = worksheetId > 0 ? worksheetId : null,
                ItemID = i.ItemId,
                ItemDescription = i.Description,
                QuantityOrdered = (float)i.Qty,
                Price = i.Price,
                StoreID = i.StoreId,
                TaxRate = (float)i.TaxRate,
                DetailID = 0,
                OrderNumber = string.Empty
            });
    }
}
