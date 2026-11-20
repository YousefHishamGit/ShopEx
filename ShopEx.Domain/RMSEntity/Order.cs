using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("Order", Schema = "dbo")]
    public class Order
    {
        public int? StoreID { get; set; }

        [Key] 
        public int ID { get; set; }

        public bool? Closed { get; set; }

        public DateTime? Time { get; set; }

        public int? Type { get; set; }

        [MaxLength(255)]
        public string? Comment { get; set; }

        public int? CustomerID { get; set; }

        public int? ShipToID { get; set; }

        public bool? DepositOverride { get; set; }

        [Column(TypeName = "money")]
        public decimal? Deposit { get; set; }

        [Column(TypeName = "money")]
        public decimal? Tax { get; set; }

        [Column(TypeName = "money")]
        public decimal? Total { get; set; }

        public DateTime? LastUpdated { get; set; }

        public DateTime? ExpirationOrDueDate { get; set; }

        public bool? Taxable { get; set; }

        public int? SalesRepID { get; set; }

        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        [Column(TypeName = "money")]
        public decimal? ShippingChargeOnOrder { get; set; }

        public bool? ShippingChargeOverride { get; set; }

        public int? ShippingServiceID { get; set; }

        [MaxLength(255)]
        public string? ShippingTrackingNumber { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        public int? AutoId { get; set; }

        public int? ReasonCodeID { get; set; }

        public int? DefaultDiscountReasonCodeID { get; set; }

        public int? DefaultReturnReasonCodeID { get; set; }

        public int? DefaultTaxChangeReasonCodeID { get; set; }

        public int? ExchangeID { get; set; }

        public int? ChannelType { get; set; }

        public string? ShippingNotes { get; set; }
    }
}
