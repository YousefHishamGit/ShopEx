using System;

namespace ShopEx.Application.DTOs.Consignment
{
    public class CreateDealerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateDealerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DealerResponse
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ActiveConsignmentsCount { get; set; }
        public decimal TotalOutstandingValue { get; set; }
        public decimal TotalUnpaidAmount { get; set; }
    }
}
