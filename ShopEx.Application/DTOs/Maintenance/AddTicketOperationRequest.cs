using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class AddTicketOperationRequest
    {
        [Required(ErrorMessage = "اسم أو وصف عملية الإصلاح مطلوب.")]
        [StringLength(250, ErrorMessage = "اسم العملية لا يتعدى 250 حرف.")]
        public string OperationName { get; set; } = string.Empty; // عملت كذا (اسم الإصلاح / القطعة المستبدلة)

        [Required(ErrorMessage = "سعر / تكلفة العملية مطلوب.")]
        [Range(0, double.MaxValue, ErrorMessage = "سعر العملية لا يمكن أن يكون سالباً.")]
        public decimal Cost { get; set; }                          // سعرها كذا

        [StringLength(500, ErrorMessage = "الملاحظات لا تتعدى 500 حرف.")]
        public string? Notes { get; set; }
    }
}
