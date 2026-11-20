using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Consignment
{
    public class CreateConsignmentSettlementRequest
    {
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public List<SettlementItemRequest> Items { get; set; } = new();
        public AddConsignmentPaymentRequest? Payment { get; set; }
    }

    public class SettlementItemRequest
    {
        public int ConsignmentItemID { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal QuantityReturned { get; set; }
    }

    public class ConsignmentSettlementResponse
    {
        public int ID { get; set; }
        public int ConsignmentID { get; set; }
        public DateTime SettlementDate { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public decimal AmountDue { get; set; }
        public List<ConsignmentSettlementItemResponse> Items { get; set; } = new();
    }

    public class ConsignmentSettlementItemResponse
    {
        public int ID { get; set; }
        public int ConsignmentItemID { get; set; }
        public int RMSItemID { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal QuantityReturned { get; set; }
        public decimal SoldValue { get; set; }
    }
}
