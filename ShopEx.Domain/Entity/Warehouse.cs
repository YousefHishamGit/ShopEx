using System.Collections.Generic;

namespace ShopEx.Domain.Entity
{
    public class Warehouse
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<StockBalance> Balances { get; set; } = new List<StockBalance>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<SupplierReturn> SupplierReturns { get; set; } = new List<SupplierReturn>();
    }
}
