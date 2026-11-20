using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Domain.Entity
{
    public class MaintenancePayment
    {
        public int Id { get; set; }

        public int MaintenanceTicketId { get; set; }
        public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public MaintenancePaymentMethod PaymentMethod { get; set; } = MaintenancePaymentMethod.Cash;
        public string? Notes { get; set; }
    }
}
