using ShopEx.Domain.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class AddTicketPaymentRequest
    {
        [Required(ErrorMessage = "المبلغ مطلوب.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر.")]
        public decimal Amount { get; set; }

        public MaintenancePaymentMethod PaymentMethod { get; set; } = MaintenancePaymentMethod.Cash;

        [StringLength(500, ErrorMessage = "الملاحظات لا تتعدى 500 حرف.")]
        public string? Notes { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}
