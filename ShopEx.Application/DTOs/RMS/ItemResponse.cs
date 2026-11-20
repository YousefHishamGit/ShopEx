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
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceA { get; set; }
        public decimal? Cost { get; set; }
        public int? SupplierID { get; set; }
    }
}
