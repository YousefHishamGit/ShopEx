using Microsoft.EntityFrameworkCore;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.RMS
{
    public class RMSContext : DbContext
    {
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Worksheet> Worksheet { get; set; }
        public DbSet<Worksheet_PurchaseOrder> Worksheet_PurchaseOrders { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderEntry> OrderEntries { get; set; }
        public DbSet<Store> Stores { get; set; }
        public RMSContext(DbContextOptions<RMSContext> options) : base(options)
        {
        }
    }
}
