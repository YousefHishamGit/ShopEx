using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierRefund
    {
        public int ID { get; set; }

        public int SupplierReturnID { get; set; }
        public SupplierReturn SupplierReturn { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime RefundDate { get; set; } = DateTime.Now;
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

}
