using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.SupplierReturn
{
    public class CreateSupplierReturnRequest
    {
        public int RMSSupplierID { get; set; }
        public int? SupplierInvoiceID { get; set; }
        public int WarehouseID { get; set; }
        public DateTime? ReturnDate { get; set; }
        public SupplierReturnSettlementType SettlementType { get; set; } = SupplierReturnSettlementType.CreditToAccount;
        public string? Notes { get; set; }

        /// <summary>Create and confirm in one atomic step (stock deducted immediately).</summary>
        public bool ConfirmImmediately { get; set; }

        public List<CreateSupplierReturnItemRequest> Items { get; set; } = new();
    }

    public class CreateSupplierReturnItemRequest
    {
        public int? SupplierInvoiceItemID { get; set; }
        public int RMSItemID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Reason { get; set; }
    }
}
