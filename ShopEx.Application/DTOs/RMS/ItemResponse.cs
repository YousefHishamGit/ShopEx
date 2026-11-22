using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.DTOs.RMS
{
    public class ItemResponse
    {
        public int ID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ItemLookupCode { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceA { get; set; }
        public decimal? Cost { get; set; }
        public decimal? LastCost { get; set; }
        public double? Quantity { get; set; }
        public int? SupplierID { get; set; }
    }
}
