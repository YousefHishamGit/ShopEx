using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    public class Batch
    {
        public decimal CustomerDepositMade { get; set; }
        public decimal CustomerDepositRedeemed { get; set; }
        public DateTime LastUpdated { get; set; }
        public int StoreID { get; set; }
        public int BatchNumber { get; set; }
        public short Status { get; set; }
        public int RegisterID { get; set; }
        public DateTime OpeningTime { get; set; }
        public DateTime ClosingTime { get; set; }
        public decimal OpeningTotal { get; set; }
        public decimal ClosingTotal { get; set; }
        public decimal Sales { get; set; }
        public decimal Returns { get; set; }
        public decimal Tax { get; set; }
        public decimal SalesPlusTax { get; set; }
        public decimal Commission { get; set; }
        public decimal PaidOut { get; set; }
        public decimal Dropped { get; set; }
        public decimal PaidOnAccount { get; set; }
        public decimal PaidToAccount { get; set; }
        public int CustomerCount { get; set; }
        public int NoSalesCount { get; set; }
        public int AbortedTransCount { get; set; }
        public decimal TotalTendered { get; set; }
        public decimal TotalChange { get; set; }
        public decimal Discounts { get; set; }
        public decimal CostOfGoods { get; set; }
        public decimal LayawayPaid { get; set; }
        public decimal LayawayClosed { get; set; }
        public decimal Shipping { get; set; }

        [Timestamp]
        public byte[] DBTimeStamp { get; set; } = Array.Empty<byte>();

        public decimal TenderRoundingError { get; set; }
        public int AutoID { get; set; }
        public bool ExportedFromHQ { get; set; }
        public decimal DebitSurcharge { get; set; }
        public decimal CashBackSurcharge { get; set; }
        public decimal Vouchers { get; set; }
    }
}
