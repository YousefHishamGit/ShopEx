using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("TransactionEntry", Schema = "dbo")]
    public class TransactionEntry
    {
        public decimal Commission { get; set; }
        public decimal Cost { get; set; }
        public decimal FullPrice { get; set; }
        public int StoreID { get; set; }
        public int ID { get; set; }
        public int TransactionNumber { get; set; }
        public int ItemID { get; set; }
        public decimal Price { get; set; }
        public short PriceSource { get; set; }
        public double Quantity { get; set; }
        public int SalesRepID { get; set; }
        public bool Taxable { get; set; }
        public int DetailID { get; set; }

        [MaxLength(255)]
        public string Comment { get; set; } = string.Empty;

        [Timestamp]
        public byte[] DBTimeStamp { get; set; } = Array.Empty<byte>();

        public int AutoID { get; set; }
        public int DiscountReasonCodeID { get; set; }
        public int ReturnReasonCodeID { get; set; }
        public int TaxChangeReasonCodeID { get; set; }
        public decimal SalesTax { get; set; }
        public int QuantityDiscountID { get; set; }
        public int ItemType { get; set; }
        public double? ComputedQuantity { get; set; }
        public DateTime? TransactionTime { get; set; }
        public bool IsAddMoney { get; set; }
        public int VoucherID { get; set; }
    }
}
