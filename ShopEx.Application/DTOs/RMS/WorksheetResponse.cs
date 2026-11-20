using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.RMS
{
    public class WorksheetResponse
    {
        public int ID { get; set; }
        public int? Style { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public string? Notes { get; set; }
        public string? Title { get; set; }
        public DateTime? FromDate { get; set; }
        public List<WorksheetPurchaseOrderResponse> PurchaseOrders { get; set; } = new();
    }

    public class WorksheetPurchaseOrderResponse
    {
        public int ID { get; set; }
        public int? WorksheetID { get; set; }
        public string? ItemDescription { get; set; }
        public double? QuantityOrdered { get; set; }
        public int? ItemID { get; set; }
        public int? DetailID { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public int? StoreID { get; set; }
        public double? TaxRate { get; set; }
    }
}

