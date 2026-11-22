using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierInvoiceItemDto
    {
        public int ID { get; set; }
        public int RMSItemID { get; set; }
        public string? ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
