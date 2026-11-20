using System;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierReturnStatementDto
    {
        public int ID { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal PendingRefund { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SettlementType { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public string? Notes { get; set; }
    }
}
