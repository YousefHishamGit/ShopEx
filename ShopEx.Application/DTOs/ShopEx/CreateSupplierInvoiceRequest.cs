using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class CreateSupplierInvoiceRequest
    {
        [Required(ErrorMessage = "RMS Supplier ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "RMS Supplier ID must be a valid positive integer.")]
        public int RMSSupplierID { get; set; }

        [Required(ErrorMessage = "Invoice number is required.")]
        [StringLength(50, ErrorMessage = "Invoice number cannot exceed 50 characters.")]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int? WarehouseID { get; set; }

        public DateTime? InvoiceDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Payment type is required.")]
        public SupplierPaymentType PaymentType { get; set; } = SupplierPaymentType.Cash;

        [Range(1, int.MaxValue, ErrorMessage = "Credit days must be greater than 0.")]
        public int? CreditDays { get; set; }

        /// <summary>None / Percent / Amount. The discount applies to the invoice as a whole.</summary>
        public SupplierDiscountType DiscountType { get; set; } = SupplierDiscountType.None;

        /// <summary>Percentage (0-100) when DiscountType is Percent, a fixed amount when Amount.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "Discount value cannot be negative.")]
        public decimal? DiscountValue { get; set; }

        public decimal? InitialPayment { get; set; }
        public string? InitialPaymentMethod { get; set; }

        public DateTime? DueDate { get; set; }

        [Required(ErrorMessage = "Invoice must contain at least one item.")]
        [MinLength(1, ErrorMessage = "Invoice must contain at least one item.")]
        public List<CreateSupplierInvoiceItemRequest> Items { get; set; } = new();
    }
}
