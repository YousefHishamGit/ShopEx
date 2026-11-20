using ShopEx.Domain.Enum;

namespace ShopEx.Domain.Entity
{
    
    public class Balance
    {
        public int Id { get; set; }

        
        public int StoreId { get; set; }

        public BalanceType BalanceType { get; set; }

    
        public decimal Amount { get; set; }

       
        public DateTime ChargedAt { get; set; } = DateTime.UtcNow;

     
        public string? Notes { get; set; }

        
        public ICollection<BalanceClosure> Closures { get; set; } = new List<BalanceClosure>();
    }
}
