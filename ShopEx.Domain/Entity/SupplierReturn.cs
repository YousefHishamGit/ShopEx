using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierReturn
    {
        public int ID { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int RMSSupplierID { get; set; }

        
        public int? SupplierInvoiceID { get; set; }
        public SupplierInvoice? SupplierInvoice { get; set; }

        
        public int WarehouseID { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public SupplierReturnStatus Status { get; set; } = SupplierReturnStatus.Draft;
        public SupplierReturnSettlementType SettlementType { get; set; } = SupplierReturnSettlementType.CreditToAccount;
        public string? Notes { get; set; }

        public ICollection<SupplierReturnItem> Items { get; set; } = new List<SupplierReturnItem>();
        public ICollection<SupplierRefund> Refunds { get; set; } = new List<SupplierRefund>();

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
        public decimal RefundedAmount => Refunds.Sum(r => r.Amount);
        public decimal PendingRefund =>
            SettlementType == SupplierReturnSettlementType.CashRefund ? TotalAmount - RefundedAmount : 0;
    }
}
