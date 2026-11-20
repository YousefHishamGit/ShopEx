using System;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class SupplierPaymentResponse
    {
        public int ID { get; set; }
        public int SupplierInvoiceID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
    }
}

