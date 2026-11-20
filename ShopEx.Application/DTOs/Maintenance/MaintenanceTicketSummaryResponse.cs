using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class MaintenanceTicketSummaryResponse
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public MaintenanceStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsPaid { get; set; }
    }
}
