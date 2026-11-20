using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("Worksheet_PurchaseOrder", Schema = "dbo")]
    public class Worksheet_PurchaseOrder
    {
        public int ID { get; set; }
        public int? WorksheetID { get; set; }
        public string? ItemDescription { get; set; }
        public double? QuantityOrdered { get; set; }
        public int? ItemID { get; set; }
        public int? DetailID { get; set; }

        
        public string OrderNumber { get; set; } = string.Empty;

        public decimal? Price { get; set; }
        public int? StoreID { get; set; }
        public double? TaxRate { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        public Worksheet Worksheet { get; set; }
        }
}
