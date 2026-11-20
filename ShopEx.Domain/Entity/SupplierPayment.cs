using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierPayment
    {
        public int ID { get; set; }
        public int SupplierInvoiceID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? PaymentMethod { get; set; } 
        
        // Navigation properties
        public SupplierInvoice SupplierInvoice { get; set; }
    }
}
