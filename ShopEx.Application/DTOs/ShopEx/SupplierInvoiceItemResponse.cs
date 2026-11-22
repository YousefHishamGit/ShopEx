namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierInvoiceItemResponse
    {
        public int ID { get; set; }
        public int SupplierInvoiceID { get; set; }
        public int RMSItemID { get; set; }
        public string? ItemDescription { get; set; }
        public string? ItemLookupCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

