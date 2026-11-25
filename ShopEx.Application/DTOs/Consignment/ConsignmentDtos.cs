using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShopEx.Application.DTOs.Consignment
{
    public class CreateConsignmentRequest
    {
        public int DealerID { get; set; }
        public int? WarehouseID { get; set; }
        public int? AgreedDays { get; set; } = 15;
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateConsignmentItemRequest> Items { get; set; } = new();
    }

    public class CreateConsignmentItemRequest
    {
        public int RMSItemID { get; set; }
        public decimal QuantityIssued { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class ConsignmentItemResponse
    {
        public int ID { get; set; }
        public int ConsignmentID { get; set; }
        public int RMSItemID { get; set; }
        public string? ItemDescription { get; set; }
        public string? ItemLookupCode { get; set; }
        public decimal QuantityIssued { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }

        public decimal QuantitySold { get; set; }
        public decimal QuantityReturned { get; set; }
        public decimal QuantityOutstanding { get; set; }

        public decimal IssuedValue { get; set; }
        public decimal SoldValue { get; set; }
        public decimal OutstandingValue { get; set; }
        public decimal Profit { get; set; }
    }

    public class ConsignmentResponse
    {
        public int ID { get; set; }
        public string ConsignmentNumber { get; set; } = string.Empty;

        public int DealerID { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string DealerPhone { get; set; } = string.Empty;
        public int? WarehouseID { get; set; }
        public string? WarehouseName { get; set; }

        public DateTime IssueDate { get; set; }
        public int? AgreedDays { get; set; }
        public DateTime DueDate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsignmentStatus Status { get; set; }
        public string? Notes { get; set; }

        public decimal TotalIssuedValue { get; set; }
        public decimal TotalSoldValue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal OutstandingQuantity { get; set; }
        public decimal OutstandingValue { get; set; }

        public bool IsFullyAccountedFor { get; set; }
        public bool IsOverdue { get; set; }

        public List<ConsignmentItemResponse> Items { get; set; } = new();
        public List<ConsignmentSettlementResponse> Settlements { get; set; } = new();
        public List<ConsignmentPaymentResponse> Payments { get; set; } = new();
    }

    public class ConsignmentSummaryResponse
    {
        public int ID { get; set; }
        public string ConsignmentNumber { get; set; } = string.Empty;

        public int DealerID { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string DealerPhone { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }
        public int? AgreedDays { get; set; }
        public DateTime DueDate { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConsignmentStatus Status { get; set; }
        public int ItemsCount { get; set; }

        public decimal TotalIssuedValue { get; set; }
        public decimal TotalSoldValue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal OutstandingQuantity { get; set; }
        public decimal OutstandingValue { get; set; }

        public bool IsFullyAccountedFor { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>One page of consignments + dealer-wide stats (the stats ignore the status tab and search).</summary>
    public class ConsignmentsPageResponse
    {
        public List<ConsignmentSummaryResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize)) : 1;

        public int CountTotal { get; set; }
        public int CountOpen { get; set; }
        public int CountOverdue { get; set; }
        public int CountClosed { get; set; }
        public int CountCancelled { get; set; }
        /// <summary>Value (at dealer price) of goods still with dealers on open consignments.</summary>
        public decimal TotalOutstandingValue { get; set; }
        /// <summary>Sold value not yet collected on open consignments.</summary>
        public decimal TotalRemainingUnpaid { get; set; }
    }
}
