using System;

namespace ShopEx.Domain.Entity
{
    public class MaintenanceTicketOperation
    {
        public int Id { get; set; }

        public int MaintenanceTicketId { get; set; }
        public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

        public string OperationName { get; set; } = string.Empty; 
        /// <summary>سعر العملية على العميل (هو اللي يظهر في الفاتورة)</summary>
        public decimal Cost { get; set; }
        /// <summary>تكلفة القطعة/العملية علينا (للإدارة فقط: لا تظهر للموظف ولا في الفاتورة)</summary>
        public decimal PartCost { get; set; }
        public string? Notes { get; set; }                        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
