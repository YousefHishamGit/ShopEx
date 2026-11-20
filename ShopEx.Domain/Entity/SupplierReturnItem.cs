using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierReturnItem
    {
        public int ID { get; set; }

        public int SupplierReturnID { get; set; }
        public SupplierReturn SupplierReturn { get; set; } = null!;

      
        public int? SupplierInvoiceItemID { get; set; }
        public SupplierInvoiceItem? SupplierInvoiceItem { get; set; }

        public int RMSItemID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Reason { get; set; }   

        public decimal TotalPrice => Quantity * UnitPrice;
    }   
}
