using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class ConsignmentSettlement
    {
        public int ID { get; set; }

        public int ConsignmentID { get; set; }
        public Consignment Consignment { get; set; } = null!;

        public DateTime SettlementDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }

        public ICollection<ConsignmentSettlementItem> Items { get; set; } = new List<ConsignmentSettlementItem>();

       
        public decimal AmountDue => Items.Sum(i => i.SoldValue);
    }
}
