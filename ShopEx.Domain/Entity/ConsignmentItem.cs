using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class ConsignmentItem
    {
        public int ID { get; set; }

        public int ConsignmentID { get; set; }
        public Consignment Consignment { get; set; } = null!;

        public int RMSItemID { get; set; }
        public decimal QuantityIssued { get; set; }   
        public decimal UnitPrice { get; set; }        
        public decimal UnitCost { get; set; }         

        public ICollection<ConsignmentSettlementItem> SettlementItems { get; set; } = new List<ConsignmentSettlementItem>();

        public decimal QuantitySold => SettlementItems.Sum(s => s.QuantitySold);
        public decimal QuantityReturned => SettlementItems.Sum(s => s.QuantityReturned);
        public decimal QuantityOutstanding => QuantityIssued - QuantitySold - QuantityReturned;

        public decimal IssuedValue => QuantityIssued * UnitPrice;
        public decimal SoldValue => QuantitySold * UnitPrice;
        public decimal OutstandingValue => QuantityOutstanding * UnitPrice;
        public decimal Profit => QuantitySold * (UnitPrice - UnitCost);
    }
}
