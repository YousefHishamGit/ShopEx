using System;

namespace ShopEx.Application.DTOs.Consignment
{
    public class AddConsignmentPaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class ConsignmentPaymentResponse
    {
        public int ID { get; set; }
        public int ConsignmentID { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
