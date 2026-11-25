using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Device
{
    /// <summary>تسجيل شراء جهاز. الفرع يُحدَّد من السيرفر للموظف ويُقبل من الطلب للـ Admin فقط.</summary>
    public class CreateDeviceRequest
    {
        /// <summary>للـ Admin فقط: الفرع الذي اشترى الجهاز (null = HQ)</summary>
        public int? StoreId { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Storage { get; set; }
        public string? Imei { get; set; }
        /// <summary>صحة البطارية % (0-100) - اختياري</summary>
        public int? BatteryHealth { get; set; }
        /// <summary>معه العلبة؟</summary>
        public bool HasBox { get; set; }
        public decimal PurchasePrice { get; set; }
        /// <summary>اختياري - افتراضياً الآن</summary>
        public DateTime? PurchaseDate { get; set; }
        public string? SellerName { get; set; }
        public string? Notes { get; set; }
        /// <summary>يحدده السيرفر من الـ Token</summary>
        public string? CreatedBy { get; set; }
    }

    public class UpdateDeviceRequest
    {
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Storage { get; set; }
        public string? Imei { get; set; }
        /// <summary>صحة البطارية % (0-100) - اختياري</summary>
        public int? BatteryHealth { get; set; }
        /// <summary>معه العلبة؟</summary>
        public bool HasBox { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? SellerName { get; set; }
        public string? Notes { get; set; }
    }

    public class SellDeviceRequest
    {
        public decimal SalePrice { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerPhone { get; set; }
        public string? Notes { get; set; }
        /// <summary>يحدده السيرفر من الـ Token</summary>
        public string? SoldBy { get; set; }
        /// <summary>الفرع الذي باع (null = HQ). الموظف: يحدده السيرفر دائماً من الـ Token؛ الأدمن: يختاره</summary>
        public int? SoldStoreId { get; set; }
    }

    public class DeviceResponse
    {
        public int Id { get; set; }
        public int? StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Storage { get; set; }
        public string? Imei { get; set; }
        public int? BatteryHealth { get; set; }
        public bool HasBox { get; set; }
        public string? Notes { get; set; }

        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? SellerName { get; set; }

        public DeviceStatus Status { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? SoldAt { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerPhone { get; set; }
        public string? SaleNotes { get; set; }
        public string? SoldBy { get; set; }
        public int? SoldStoreId { get; set; }
        public string? SoldStoreName { get; set; }
        /// <summary>الربح = سعر البيع - سعر الشراء (للمباع فقط)</summary>
        public decimal? Profit { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeviceFilter
    {
        /// <summary>0 = الـ HQ فقط، أي رقم آخر = ذلك الفرع، null = الكل</summary>
        public int? StoreId { get; set; }
        public DeviceStatus? Status { get; set; }
        /// <summary>بحث في الماركة / الموديل / IMEI / المشتري / البائع</summary>
        public string? Search { get; set; }
        /// <summary>من تاريخ الشراء (شامل)</summary>
        public DateTime? FromDate { get; set; }
        /// <summary>حتى تاريخ الشراء (شامل)</summary>
        public DateTime? ToDate { get; set; }
    }

    public class DeviceStatsDto
    {
        public int TotalCount { get; set; }
        public int InStockCount { get; set; }
        public int SoldCount { get; set; }
        /// <summary>إجمالي سعر شراء كل الأجهزة المطابقة</summary>
        public decimal TotalPurchase { get; set; }
        /// <summary>قيمة (تكلفة) الأجهزة المتاحة</summary>
        public decimal InStockValue { get; set; }
        /// <summary>إجمالي مبيعات الأجهزة المباعة</summary>
        public decimal TotalSales { get; set; }
        /// <summary>ربح الأجهزة المباعة (مبيعات - تكلفتها)</summary>
        public decimal TotalProfit { get; set; }
    }

    public class DeviceSummaryDto
    {
        public DeviceStatsDto Stats { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<DeviceResponse> Items { get; set; } = new();
    }
}
