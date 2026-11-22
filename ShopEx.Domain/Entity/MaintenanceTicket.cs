using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShopEx.Domain.Entity
{
    public class MaintenanceTicket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;

        /// <summary>الفرع الذي استلم الجهاز (معرّف الفرع في RMS). null للتذاكر القديمة.</summary>
        public int? StoreId { get; set; }

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
        public DateTime ReceivedAt { get; set; } = DateTime.Now;
        public decimal ExpectedCost { get; set; }

        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Received;

       
        public DateTime? RepairedAt { get; set; }
        public string? RepairNotes { get; set; }
        public ICollection<MaintenanceTicketOperation> Operations { get; set; } = new List<MaintenanceTicketOperation>();

        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }
        public ICollection<MaintenancePayment> Payments { get; set; } = new List<MaintenancePayment>();

       
        public decimal TotalCost => Operations.Sum(x => x.Cost);
        public decimal PaidAmount => Payments.Sum(x => x.Amount);
        public decimal RemainingAmount => TotalCost - PaidAmount;
        public bool IsPaid => RemainingAmount <= 0 && TotalCost > 0;
    }
}
