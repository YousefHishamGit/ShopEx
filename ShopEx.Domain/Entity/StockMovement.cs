using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class StockMovement
    {
        public int ID { get; set; }

        public int WarehouseID { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public int RMSItemID { get; set; }

        public StockMovementType MovementType { get; set; }
        public decimal Quantity { get; set; }   
        public decimal UnitCost { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.Now;

       
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }

        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }

        public decimal TotalCost => Quantity * UnitCost;
    }
}
