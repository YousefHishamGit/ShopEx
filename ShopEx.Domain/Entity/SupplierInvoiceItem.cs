using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierInvoiceItem
    {
        public int ID { get; set; }
        public int SupplierInvoiceID { get; set; }
        public int RMSItemID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        // Navigation properties
        public SupplierInvoice SupplierInvoice { get; set; } = null!;
        public ICollection<SupplierReturnItem> ReturnItems { get; set; } = new List<SupplierReturnItem>();
        
    }
}
