using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// جهاز (موبايل مثلاً) اشتراه الـ HQ أو أحد الفروع بغرض إعادة البيع.
    /// يسجَّل سعر الشراء عند الإدخال، وسعر البيع عند البيع.
    /// </summary>
    public class Device
    {
        public int Id { get; set; }

        /// <summary>
        /// الفرع الذي اشترى الجهاز (null = الإدارة العامة / HQ)
        /// </summary>
        public int? StoreId { get; set; }

        // ----- تفاصيل الجهاز -----
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Color { get; set; }
        /// <summary>السعة / المواصفات (مثال: 128GB)</summary>
        public string? Storage { get; set; }
        /// <summary>IMEI أو الرقم التسلسلي</summary>
        public string? Imei { get; set; }
        /// <summary>صحة البطارية % (0-100) - اختياري</summary>
        public int? BatteryHealth { get; set; }
        /// <summary>هل الجهاز معه العلبة؟</summary>
        public bool HasBox { get; set; }
        public string? Notes { get; set; }

        // ----- الشراء -----
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        /// <summary>اشتُري من مين (مورد / شخص)</summary>
        public string? SellerName { get; set; }

        // ----- البيع -----
        public DeviceStatus Status { get; set; } = DeviceStatus.InStock;
        public decimal? SalePrice { get; set; }
        public DateTime? SoldAt { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerPhone { get; set; }
        public string? SaleNotes { get; set; }
        public string? SoldBy { get; set; }
        /// <summary>الفرع الذي باع الجهاز (null = الـ HQ). قد يختلف عن الفرع الذي اشتراه</summary>
        public int? SoldStoreId { get; set; }

        // ----- تتبّع -----
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
