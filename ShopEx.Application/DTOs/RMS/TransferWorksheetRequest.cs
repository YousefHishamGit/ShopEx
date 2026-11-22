using System.Collections.Generic;

namespace ShopEx.Application.DTOs.RMS
{
    /// <summary>Creates an already-approved (status 2) worksheet sending the items to one branch.</summary>
    public class TransferWorksheetRequest
    {
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public int StoreId { get; set; }
        public List<PurchaseLineDto> Items { get; set; } = new();
    }
}
