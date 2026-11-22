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
            // Stock is NOT touched here: it is deducted only when the worksheet is approved/closed.
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
            return ws.ID;
        }

        public async Task<int> CreateApprovedTransferAsync(TransferWorksheetRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.StoreId <= 0)
                throw new ArgumentException("يجب اختيار الفرع المستلم.");
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("يجب إضافة صنف واحد على الأقل.");
            if (request.Items.Any(i => i.ItemId <= 0 || i.Qty <= 0))
                throw new ArgumentException("يجب أن تكون كل الأصناف صالحة والكميات أكبر من صفر.");

            var defaultWh = await _warehouseRepository.GetDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("لم يتم العثور على المخزن الافتراضي لصرف الأصناف.");

            var items = request.Items.Select(i => { i.StoreId = request.StoreId; return i; }).ToList();
            var ws = CreateWorksheet(new OpenWorksheetDto { Title = request.Title, Notes = request.Notes });
            ws.Status = StatusApproved;
            ws.Worksheet_PurchaseOrders = MapLines(0, items).ToList();
            var lines = ws.Worksheet_PurchaseOrders.ToList();

            // Check stock for every item before writing anything.
            var balances = await ValidateStockAsync(defaultWh, lines, cancellationToken);

            await _repo.AddWorkSheetAsync(ws, cancellationToken);
            await _repo.SaveAsync(cancellationToken);

            try
            {
                await DeductStockAsync(defaultWh, ws.ID, ws.Title, lines, balances, cancellationToken);
            }
            catch
            {
                ws.Status = StatusOpen;
                await _repo.SaveAsync(cancellationToken);
                throw;
            }

            return ws.ID;
        }

        public async Task ApproveWorksheetAsync(int worksheetId, int? destinationStoreId = null, CancellationToken cancellationToken = default)
        {
            if (worksheetId <= 0)
                throw new ArgumentException("Invalid worksheet ID.", nameof(worksheetId));

            var ws = await _repo.GetByIdAsync(worksheetId, cancellationToken);
            if (ws == null)
                throw new KeyNotFoundException($"Worksheet with ID {worksheetId} was not found.");

            if (ws.Status != StatusOpen)
                throw new InvalidOperationException($"Worksheet {worksheetId} cannot be approved because its status is {ws.Status} (only pending worksheets with status 1 can be approved).");

            var lines = ws.Worksheet_PurchaseOrders?.ToList() ?? new List<Worksheet_PurchaseOrder>();
            if (lines.Count == 0)
                throw new InvalidOperationException("لا يمكن اعتماد ورقة عمل بدون بنود.");

            // A destination branch chosen at closing time applies to every line.
            if (destinationStoreId.HasValue)
            {
                if (destinationStoreId.Value <= 0)
                    throw new ArgumentException("Invalid destination store ID.", nameof(destinationStoreId));
                foreach (var line in lines)
                    line.StoreID = destinationStoreId.Value;
            }

            if (lines.Any(l => !l.StoreID.HasValue || l.StoreID.Value <= 0))
                throw new InvalidOperationException("يجب تحديد الفرع المستلم لجميع البنود قبل الاعتماد.");

            var defaultWh = await _warehouseRepository.GetDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("لم يتم العثور على المخزن الافتراضي لصرف الأصناف.");

            // Validate stock for ALL lines (aggregated per item) before changing anything.
            var balances = await ValidateStockAsync(defaultWh, lines, cancellationToken);

            ws.Status = StatusApproved;
            await _repo.SaveAsync(cancellationToken);

            try
            {
                await DeductStockAsync(defaultWh, worksheetId, ws.Title, lines, balances, cancellationToken);
            }
            catch
            {
                // Keep the worksheet open if stock could not be issued, so approval can be retried.
                ws.Status = StatusOpen;
                await _repo.SaveAsync(cancellationToken);
                throw;
            }
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

        private async Task<Dictionary<int, StockBalance>> ValidateStockAsync(Warehouse wh, List<Worksheet_PurchaseOrder> lines, CancellationToken ct)
        {
            var balances = new Dictionary<int, StockBalance>();
            foreach (var group in lines.GroupBy(l => l.ItemID ?? 0))
            {
                var requestedQty = (decimal)group.Sum(l => l.QuantityOrdered ?? 0);
                var balance = group.Key > 0 ? await _stockRepository.GetBalanceAsync(wh.ID, group.Key, ct) : null;

                if (balance == null || balance.QuantityOnHand < requestedQty)
                {
                    var available = balance?.QuantityOnHand ?? 0;
                    throw new InvalidOperationException(
                        $"الرصيد المتاح في المخزن ({wh.Name}) للصنف {group.Key} هو ({available})، وهو غير كافٍ للكمية المطلوبة في ورقة العمل ({requestedQty}).");
                }
                balances[group.Key] = balance;
            }
            return balances;
        }

        private async Task DeductStockAsync(Warehouse wh, int worksheetId, string? title, List<Worksheet_PurchaseOrder> lines,
            Dictionary<int, StockBalance> balances, CancellationToken ct)
        {
            foreach (var line in lines)
            {
                var balance = balances[line.ItemID ?? 0];
                var qty = (decimal)(line.QuantityOrdered ?? 0);

                balance.QuantityOnHand -= qty;
                balance.LastUpdatedAt = DateTime.Now;
                _stockRepository.UpdateBalance(balance);

                await _stockRepository.AddMovementAsync(new StockMovement
                {
                    WarehouseID = wh.ID,
                    RMSItemID = line.ItemID!.Value,
                    MovementType = StockMovementType.TransferOut,
                    Quantity = qty,
                    UnitCost = line.Price.HasValue && line.Price.Value > 0 ? line.Price.Value : balance.AverageCost,
                    MovementDate = DateTime.Now,
                    ReferenceType = "RMSWorksheet",
                    ReferenceID = worksheetId,
                    Notes = $"صرف ورقة عمل رقم {worksheetId} - {title} إلى الفرع رقم {line.StoreID}",
                    CreatedBy = "System"
                }, ct);
            }

            await _shopUnitOfWork.SaveChangesAsync(ct);
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
