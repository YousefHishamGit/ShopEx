using System;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class MaintenanceTicketOperationResponse
    {
        public int Id { get; set; }
        public int MaintenanceTicketId { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
