using System.Collections.Generic;

namespace ShopEx.Application.DTOs.RMS
{
    public class OpenWorksheetWithPurchasesRequest
    {
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseLineDto> Items { get; set; } = new();
    }
}

