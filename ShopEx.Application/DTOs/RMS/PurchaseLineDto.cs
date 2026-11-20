namespace ShopEx.Application.DTOs.RMS
{
    public class PurchaseLineDto
    {
        public int ItemId { get; set; }
        public string? Description { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public int StoreId { get; set; }
        public decimal TaxRate { get; set; }
    }
}
