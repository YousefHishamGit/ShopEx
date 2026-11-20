using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class ConsignmentSettlementItem
    {
        public int ID { get; set; }

        public int ConsignmentSettlementID { get; set; }
        public ConsignmentSettlement ConsignmentSettlement { get; set; } = null!;

        public int ConsignmentItemID { get; set; }
        public ConsignmentItem ConsignmentItem { get; set; } = null!;

        public decimal QuantitySold { get; set; }      
        public decimal QuantityReturned { get; set; }  

        public decimal SoldValue => QuantitySold * ConsignmentItem.UnitPrice;
    }
}
