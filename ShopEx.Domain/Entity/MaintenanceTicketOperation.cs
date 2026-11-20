using System;

namespace ShopEx.Domain.Entity
{
    public class MaintenanceTicketOperation
    {
        public int Id { get; set; }

        public int MaintenanceTicketId { get; set; }
        public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

        public string OperationName { get; set; } = string.Empty; 
        public decimal Cost { get; set; }                          
        public string? Notes { get; set; }                        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
