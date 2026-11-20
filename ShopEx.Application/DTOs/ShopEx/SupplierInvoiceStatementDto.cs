using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierInvoiceStatementDto
    {
        public int ID { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; }
        public string PaymentType { get; set; }
        public DateTime? DueDate { get; set; }

        public List<SupplierInvoiceItemDto> Items { get; set; } = new();
        public List<SupplierPaymentDto> Payments { get; set; } = new();
    }
}
