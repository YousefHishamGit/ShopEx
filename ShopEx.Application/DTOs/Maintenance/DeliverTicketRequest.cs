using ShopEx.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class DeliverTicketRequest
    {
        // تسليم الجهاز ودفع المبلغ المطلوب
        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المدفوع لا يمكن أن يكون سالباً.")]
        public decimal? PaymentAmount { get; set; } // إن تُرك فارغاً يسدد المتبقي تلقائياً

        public MaintenancePaymentMethod PaymentMethod { get; set; } = MaintenancePaymentMethod.Cash;

        [StringLength(1000, ErrorMessage = "ملاحظات التسليم لا تتعدى 1000 حرف.")]
        public string? DeliveryNotes { get; set; }
    }
}
