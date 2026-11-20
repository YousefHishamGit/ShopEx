using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Domain.Entity
{
    public class Consignment
    {
        public int ID { get; set; }
        public string ConsignmentNumber { get; set; } = string.Empty;

        public int DealerID { get; set; }
        public Dealer Dealer { get; set; } = null!;

        
        

        public DateTime IssueDate { get; set; } = DateTime.Now;
        public int? AgreedDays { get; set; }            
        public DateTime DueDate { get; set; }           

        public ConsignmentStatus Status { get; set; } = ConsignmentStatus.Open;
        public string? Notes { get; set; }

        public ICollection<ConsignmentItem> Items { get; set; } = new List<ConsignmentItem>();
        public ICollection<ConsignmentSettlement> Settlements { get; set; } = new List<ConsignmentSettlement>();
        public ICollection<ConsignmentPayment> Payments { get; set; } = new List<ConsignmentPayment>();

       
        public decimal TotalIssuedValue => Items.Sum(i => i.IssuedValue);     
        public decimal TotalSoldValue => Items.Sum(i => i.SoldValue);         
        public decimal PaidAmount => Payments.Sum(p => p.Amount);             
        public decimal RemainingAmount => TotalSoldValue - PaidAmount;        
        public decimal OutstandingQuantity => Items.Sum(i => i.QuantityOutstanding); 
        public decimal OutstandingValue => Items.Sum(i => i.OutstandingValue);

        public bool IsFullyAccountedFor => Items.All(i => i.QuantityOutstanding <= 0);
        public bool IsOverdue => Status == ConsignmentStatus.Open
                                 && DueDate.Date < DateTime.Today
                                 && (OutstandingQuantity > 0 || RemainingAmount > 0);
    }
}
