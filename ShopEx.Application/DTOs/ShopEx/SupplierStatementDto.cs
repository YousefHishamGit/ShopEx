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
        public decimal TotalRemainingAmount { get; set; }

        public List<SupplierInvoiceStatementDto> Invoices { get; set; } = new();
        public List<SupplierReturnStatementDto> Returns { get; set; } = new();
    }
}
