using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class ConsignmentPayment
    {
        public int ID { get; set; }

        public int ConsignmentID { get; set; }
        public Consignment Consignment { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
