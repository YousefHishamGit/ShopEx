using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.RMS
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class CustomerSummaryDto
    {
        public int ID { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal AccountBalance { get; set; }
        public int OpenInvoicesCount { get; set; }
        public decimal TotalOpenInvoicesAmount { get; set; }
        public int? OldestOpenInvoiceDays { get; set; }
    }

    public class CustomerOpenInvoiceDto
    {
        public int InvoiceID { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int DaysOld { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int? StoreID { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Deposit { get; set; }
        public decimal RemainingAmount { get; set; }
        public string? Comment { get; set; }
        public string? ReferenceNumber { get; set; }
        public int ItemsCount { get; set; }
    }

    public class CustomerStatementDto
    {
        public int CustomerID { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal AccountBalance { get; set; }
        public int? MinDaysOldFilter { get; set; }
        public int TotalOpenInvoicesCount { get; set; }
        public decimal TotalOpenInvoicesAmount { get; set; }
        public decimal TotalOpenInvoicesRemaining { get; set; }
        public List<CustomerOpenInvoiceDto> OpenInvoices { get; set; } = new();
    }

    public class CustomerInvoiceItemDto
    {
        public int EntryID { get; set; }
        public int? ItemID { get; set; }
        public string? ItemLookupCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? FullPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CustomerInvoiceDetailDto
    {
        public int InvoiceID { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int DaysOld { get; set; }
        public int CustomerID { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int? StoreID { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal Deposit { get; set; }
        public decimal RemainingAmount { get; set; }
        public string? Comment { get; set; }
        public string? ReferenceNumber { get; set; }
        public List<CustomerInvoiceItemDto> Items { get; set; } = new();
    }
}
