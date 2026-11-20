using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    public class Worksheet
    {
        public int ID { get; set; }
        public int? Style { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public int? Status { get; set; }
        public string? Notes { get; set; }
        public string? Title { get; set; } = "PO Planner from";
        public DateTime? FromDate { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        public ICollection<Worksheet_PurchaseOrder> Worksheet_PurchaseOrders { get; set; } = new List<Worksheet_PurchaseOrder>();


    }
}
