using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierStatementDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Code { get; set; }
        public string AccountNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }

       
        public decimal TotalInvoicesAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalReturnsAmount { get; set; }
        public decimal TotalRefundsAmount { get; set; }
        public decimal TotalPendingRefundsAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }

        public int UnpaidCount { get; set; }
        public int PartialCount { get; set; }
        public int PaidCount { get; set; }
        public int TotalInvoicesCount { get; set; }

        public int InvoicesPageNumber { get; set; } = 1;
        public int InvoicesPageSize { get; set; } = 10;
        public int InvoicesTotalPages { get; set; } = 1;
        public int InvoicesTotalCount { get; set; }

        public int ReturnsCount { get; set; }
        public int ReturnsPageNumber { get; set; } = 1;
        public int ReturnsPageSize { get; set; } = 10;
        public int ReturnsTotalPages { get; set; } = 1;
        public int ReturnsTotalCount { get; set; }

        public List<SupplierInvoiceStatementDto> Invoices { get; set; } = new();
        public List<SupplierReturnStatementDto> Returns { get; set; } = new();
    }
}
