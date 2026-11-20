using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("OrderEntry", Schema = "dbo")]
    public class OrderEntry
    {
        [Column(TypeName = "money")]
        public decimal? Cost { get; set; }

        public int? StoreID { get; set; }

        [Key] 
        public int ID { get; set; }

        public int? OrderID { get; set; }

        public int? ItemID { get; set; }

        [Column(TypeName = "money")]
        public decimal? FullPrice { get; set; }

        public short? PriceSource { get; set; }

        [Column(TypeName = "money")]
        public decimal? Price { get; set; }

        public double? QuantityOnOrder { get; set; }

        public int? SalesRepID { get; set; }

        public int? Taxable { get; set; }

        public int? DetailID { get; set; }

        [MaxLength(30)]
        public string? Description { get; set; }

        public double? QuantityRTD { get; set; }

        public DateTime? LastUpdated { get; set; }

        [MaxLength(255)]
        public string? Comment { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        public int? AutoID { get; set; }

        public int? DiscountReasonCodeID { get; set; }

        public int? ReturnReasonCodeID { get; set; }

        public int? TaxChangeReasonCodeID { get; set; }

        public DateTime? TransactionTime { get; set; }

        public bool? IsAddMoney { get; set; }

        public int? VoucherID { get; set; }
    }
}
