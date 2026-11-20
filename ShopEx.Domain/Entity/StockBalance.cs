using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class StockBalance
    {
        public int ID { get; set; }

        public int WarehouseID { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public int RMSItemID { get; set; }

        public decimal QuantityOnHand { get; set; }
        public decimal AverageCost { get; set; }      
        public decimal? ReorderLevel { get; set; }    
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // concurrency

        public decimal TotalValue => QuantityOnHand * AverageCost;
        public bool IsLowStock => ReorderLevel.HasValue && QuantityOnHand <= ReorderLevel.Value;
    }

}
