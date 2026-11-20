using System;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.ShopEx
{
    public class CreateSupplierInvoiceItemRequest
    {
        [Required(ErrorMessage = "RMS Item ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "RMS Item ID must be a valid positive integer.")]
        public int RMSItemID { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required.")]
        [Range(0.0, double.MaxValue, ErrorMessage = "Unit price must be greater than or equal to 0.")]
        public decimal UnitPrice { get; set; }
    }
}

