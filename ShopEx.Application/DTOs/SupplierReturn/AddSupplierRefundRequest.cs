using System;

namespace ShopEx.Application.DTOs.SupplierReturn
{
    public class AddSupplierRefundRequest
    {
        public decimal Amount { get; set; }
        public DateTime? RefundDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
