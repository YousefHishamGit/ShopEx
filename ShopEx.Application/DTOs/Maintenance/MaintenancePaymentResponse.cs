using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class MaintenancePaymentResponse
    {
        public int Id { get; set; }
        public int MaintenanceTicketId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public MaintenancePaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName => PaymentMethod.ToString();
        public string? Notes { get; set; }
    }
}
