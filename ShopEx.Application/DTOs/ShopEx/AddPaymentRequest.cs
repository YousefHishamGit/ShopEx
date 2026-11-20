using System;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class AddPaymentRequest
    {
        [Required(ErrorMessage = "Payment amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters.")]
        public string? PaymentMethod { get; set; }
    }
}
