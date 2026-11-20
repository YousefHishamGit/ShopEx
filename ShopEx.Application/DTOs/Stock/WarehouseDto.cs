using System;

namespace ShopEx.Application.DTOs.Stock
{
    public class CreateWarehouseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWarehouseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }

    public class WarehouseResponse
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int TotalItemsCount { get; set; }
    }
}
