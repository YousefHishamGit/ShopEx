using ShopEx.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class CreateMaintenanceTicketRequest
    {
        // المرحلة الأولى: استلام من العميل
        [StringLength(150, ErrorMessage = "اسم المستلم لا يتعدى 150 حرف.")]
        public string? ReceivedBy { get; set; } // اسم الموظف أو الفني المستلم للجهاز

        [Required(ErrorMessage = "اسم العميل مطلوب.")]
        [StringLength(150, ErrorMessage = "اسم العميل لا يتعدى 150 حرف.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم هاتف العميل مطلوب.")]
        [StringLength(25, ErrorMessage = "رقم الهاتف لا يتعدى 25 حرف.")]
        public string CustomerPhone { get; set; } = string.Empty;

        // بيانات الجهاز
        [Required(ErrorMessage = "اسم / نوع الجهاز مطلوب.")]
        [StringLength(150, ErrorMessage = "اسم الجهاز لا يتعدى 150 حرف.")]
        public string DeviceName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(1000)]
        public string? IssueDescription { get; set; } // كلام العميل أو وصف العطل

        [StringLength(500)]
        public string? Accessories { get; set; } // الملحقات (شاحن، جراب، ...)

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "التكلفة التقديرية لا يمكن أن تكون سالبة.")]
        public decimal ExpectedCost { get; set; }

        // دفعة مقدمة / عربون عند الاستلام إن وجد
        [Range(0, double.MaxValue, ErrorMessage = "العربون لا يمكن أن يكون سالباً.")]
        public decimal? Deposit { get; set; }

        public MaintenancePaymentMethod? DepositPaymentMethod { get; set; }
    }
}
