using System.Collections.Generic;

namespace ShopEx.Application.DTOs.ShopEx
{
    /// <summary>Paged invoice list plus totals over the WHOLE filtered set (not only the current page).</summary>
    public class SupplierInvoicesPage
    {
        public List<SupplierInvoiceSummaryResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;

        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    /// <summary>Lightweight account totals for a supplier, computed without loading invoice rows.</summary>
    public class SupplierAccountSummary
    {
        public decimal TotalInvoices { get; set; }
        public decimal TotalPaid { get; set; }
        public int InvoicesCount { get; set; }
        public int UnpaidCount { get; set; }
        public int PartialCount { get; set; }
        public int PaidCount { get; set; }

        // Confirmed returns only
        public decimal TotalReturns { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalPendingRefunds { get; set; }
        public int ReturnsCount { get; set; }
    }
}
