namespace ShopEx.Application.DTOs.RMS
{
    public class ApproveWorksheetRequest
    {
        /// <summary>Optional branch (RMS StoreID) that receives all lines; overrides per-line stores.</summary>
        public int? DestinationStoreId { get; set; }
    }
}
