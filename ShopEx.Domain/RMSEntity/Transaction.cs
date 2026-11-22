using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.RMSEntity
{
    public class Transaction
    {
        public int ShipToID { get; set; }
        public int StoreID { get; set; }
        public int TransactionNumber { get; set; }
        public int BatchNumber { get; set; }
        public DateTime Time { get; set; }
        public int CustomerID { get; set; }
        public int CashierID { get; set; }
        public decimal Total { get; set; }
        public decimal SalesTax { get; set; }

        [MaxLength(255)]
        public string Comment { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Timestamp]
        public byte[]? DBTimeStamp { get; set; }

        public int AutoID { get; set; }
        public int Status { get; set; }
        public int ExchangeID { get; set; }
        public int ChannelType { get; set; }
        public int RecallID { get; set; }
        public int RecallType { get; set; }
    }
}
