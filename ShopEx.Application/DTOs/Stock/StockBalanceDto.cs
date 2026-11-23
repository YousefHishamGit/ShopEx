using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Stock
{
    public class StockBalanceResponse
    {
        public int ID { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int RMSItemID { get; set; }
        public string? ItemDescription { get; set; }
        public string? ItemLookupCode { get; set; }
        public decimal? RetailPrice { get; set; }
        public decimal? Cost { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }
        public decimal? ReorderLevel { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class PagedStockBalanceResponse
    {
        public List<StockBalanceResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        /// <summary>Warehouse-wide (not search/page filtered).</summary>
        public decimal TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        /// <summary>Warehouse-wide stock value at RMS retail price (quantity x price).</summary>
        public decimal TotalPrice { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
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
