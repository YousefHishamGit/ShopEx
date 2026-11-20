using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    [Table("Customer", Schema = "dbo")]
    public class Customer
    {
        [MaxLength(20)]
        public string? AccountNumber { get; set; }

        public int? AccountTypeID { get; set; }

        [MaxLength(50)]
        public string? Address2 { get; set; }

        public bool? AssessFinanceCharges { get; set; }

        [MaxLength(50)]
        public string? Company { get; set; }

        [MaxLength(20)]
        public string? Country { get; set; }

        public DateTime? CustomDate1 { get; set; }
        public DateTime? CustomDate2 { get; set; }
        public DateTime? CustomDate3 { get; set; }
        public DateTime? CustomDate4 { get; set; }
        public DateTime? CustomDate5 { get; set; }

        public double? CustomNumber1 { get; set; }
        public double? CustomNumber2 { get; set; }
        public double? CustomNumber3 { get; set; }
        public double? CustomNumber4 { get; set; }
        public double? CustomNumber5 { get; set; }

        [MaxLength(30)]
        public string? CustomText1 { get; set; }
        [MaxLength(30)]
        public string? CustomText2 { get; set; }
        [MaxLength(30)]
        public string? CustomText3 { get; set; }
        [MaxLength(30)]
        public string? CustomText4 { get; set; }
        [MaxLength(30)]
        public string? CustomText5 { get; set; }

        public bool? GlobalCustomer { get; set; }
        public int? HQID { get; set; }

        public DateTime? LastStartingDate { get; set; }
        public DateTime? LastClosingDate { get; set; }
        public DateTime? LastUpdated { get; set; }

        public bool? LimitPurchase { get; set; }

        [Column(TypeName = "money")]
        public decimal? LastClosingBalance { get; set; }

        public int? PrimaryShipToID { get; set; }

        [MaxLength(20)]
        public string? State { get; set; }

        public int? StoreID { get; set; }

        [Key]
        public int ID { get; set; }

        public bool? LayawayCustomer { get; set; }
        public bool? Employee { get; set; }

        [MaxLength(30)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(50)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(15)]
        public string? Zip { get; set; }

        [Column(TypeName = "money")]
        public decimal? AccountBalance { get; set; }

        [Column(TypeName = "money")]
        public decimal? CreditLimit { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalSales { get; set; }

        public DateTime? AccountOpened { get; set; }
        public DateTime? LastVisit { get; set; }

        public int? TotalVisits { get; set; }

        [Column(TypeName = "money")]
        public decimal? TotalSavings { get; set; }

        public float? CurrentDiscount { get; set; }

        public short? PriceLevel { get; set; }

        public bool? TaxExempt { get; set; }

        [MaxLength(20)]
        public string? Title { get; set; }

        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        [MaxLength(20)]
        public string? TaxNumber { get; set; }

        [MaxLength(50)]
        public string? PictureName { get; set; }

        public int? DefaultShippingServiceID { get; set; }

        public int? AutoID { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(30)]
        public string? FaxNumber { get; set; }

        public int? CashierID { get; set; }
        public int? SalesRepID { get; set; }

        [Column(TypeName = "money")]
        public decimal? Vouchers { get; set; }

        public string? Notes { get; set; }
    }
}
