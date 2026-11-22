using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierInvoiceResponse
    {
        public int ID { get; set; }
        public int RMSSupplierID { get; set; }
        public string? RMSSupplierName { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public SupplierInvoiceStatus SupplierInvoiceStatus { get; set; }
        public string StatusName => SupplierInvoiceStatus.ToString();
        public SupplierPaymentType PaymentType { get; set; }
        public string PaymentTypeName => PaymentType.ToString();
        public int? CreditDays { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public List<SupplierInvoiceItemResponse> Items { get; set; } = new();
        public List<SupplierPaymentResponse> Payments { get; set; } = new();
    }
}
