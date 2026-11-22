using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class MaintenanceTicketResponse
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }

        // المرحلة الأولى: الاستلام وبيانات الجهاز
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public string? SerialNumber { get; set; }
        public string? IssueDescription { get; set; }
        public string? Accessories { get; set; }
        public string? Notes { get; set; }
        public DateTime ReceivedAt { get; set; }
        public decimal ExpectedCost { get; set; }

        // الحالة
        public MaintenanceStatus Status { get; set; }
        public string StatusName => Status.ToString();

        // المرحلة الثانية: الإصلاح
        public DateTime? RepairedAt { get; set; }
        public string? RepairNotes { get; set; }

        // المرحلة الثالثة: التسليم والدفع
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }

        // الحسابات المالية
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsPaid { get; set; }

        // العمليات والمدفوعات
        public List<MaintenanceTicketOperationResponse> Operations { get; set; } = new();
        public List<MaintenancePaymentResponse> Payments { get; set; } = new();
    }
}
