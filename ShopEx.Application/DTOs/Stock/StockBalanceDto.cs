using System;

namespace ShopEx.Application.DTOs.Stock
{
    public class StockBalanceResponse
    {
        public int ID { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int RMSItemID { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }
        public decimal? ReorderLevel { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class StockAdjustmentRequest
    {
        public int WarehouseID { get; set; }
        public int RMSItemID { get; set; }
        /// <summary>
        /// Positive for adjustment in, negative for adjustment out
        /// </summary>
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }
    }
}
