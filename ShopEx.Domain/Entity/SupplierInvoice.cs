using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class SupplierInvoice
    {
        public int ID { get; set; }
        public int RMSSupplierID { get; set; }
        public DateTime InvoiceDate { get; set; }= DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public SupplierInvoiceStatus SupplierInvoiceStatus { get; set; } = SupplierInvoiceStatus.Unpaid;

        public SupplierPaymentType PaymentType { get; set; }
        public int? CreditDays { get; set; }
        public DateTime? DueDate { get; set; }

        public decimal PaidAmount => Payments.Sum(p => p.Amount);
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public ICollection<SupplierInvoiceItem> Items { get; set; } = new List<SupplierInvoiceItem>();
        public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
        public ICollection<SupplierReturn> Returns { get; set; } = new List<SupplierReturn>();
    }
}
