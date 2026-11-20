using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class CompleteRepairRequest
    {
        [StringLength(1000, ErrorMessage = "ملاحظات الإصلاح لا تتعدى 1000 حرف.")]
        public string? RepairNotes { get; set; }

        // عمليات اختيارية يتم إضافتها دفعة واحدة عند إتمام الإصلاح إن لم تكن مضافة مسبقاً
        public List<AddTicketOperationRequest>? Operations { get; set; }
    }
}
