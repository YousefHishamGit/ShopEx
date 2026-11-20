using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.SupplierReturn
{
    public class SupplierReturnResponse
    {
        public int ID { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int RMSSupplierID { get; set; }
        public int? SupplierInvoiceID { get; set; }
        public string? InvoiceNumber { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public SupplierReturnStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public SupplierReturnSettlementType SettlementType { get; set; }
        public string SettlementTypeName => SettlementType.ToString();
        public string? Notes { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal PendingRefund { get; set; }

        public List<SupplierReturnItemResponse> Items { get; set; } = new();
        public List<SupplierRefundResponse> Refunds { get; set; } = new();
    }

    public class SupplierReturnSummaryResponse
    {
        public int ID { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int RMSSupplierID { get; set; }
        public int? SupplierInvoiceID { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public SupplierReturnStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public SupplierReturnSettlementType SettlementType { get; set; }
        public string SettlementTypeName => SettlementType.ToString();
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal PendingRefund { get; set; }
    }

    public class SupplierReturnItemResponse
    {
        public int ID { get; set; }
        public int SupplierReturnID { get; set; }
        public int? SupplierInvoiceItemID { get; set; }
        public int RMSItemID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Reason { get; set; }
    }

    public class SupplierRefundResponse
    {
        public int ID { get; set; }
        public int SupplierReturnID { get; set; }
        public decimal Amount { get; set; }
        public DateTime RefundDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
