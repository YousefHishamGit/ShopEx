using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Application.DTOs.Stock
{
    public class StockMovementResponse
    {
        public int ID { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int RMSItemID { get; set; }
        public string? ItemDescription { get; set; }
        /// <summary>True when the movement adds stock, false when it removes it.</summary>
        public bool IsIncoming { get; set; }
        public StockMovementType MovementType { get; set; }
        public string MovementTypeName => MovementType.ToString();
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime MovementDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
    }
}
