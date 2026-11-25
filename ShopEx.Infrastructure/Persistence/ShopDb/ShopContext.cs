using Microsoft.EntityFrameworkCore;
using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb
{
    public class ShopContext: DbContext
    {
        public ShopContext(DbContextOptions<ShopContext> options) : base(options)
        {
        }

        public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
        public DbSet<SupplierInvoiceItem> SupplierInvoiceItems => Set<SupplierInvoiceItem>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

        public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
        public DbSet<MaintenanceTicketOperation> MaintenanceTicketOperations => Set<MaintenanceTicketOperation>();
        public DbSet<MaintenancePayment> MaintenancePayments => Set<MaintenancePayment>();

        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<StockBalance> StockBalances => Set<StockBalance>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<SupplierReturn> SupplierReturns => Set<SupplierReturn>();
        public DbSet<SupplierReturnItem> SupplierReturnItems => Set<SupplierReturnItem>();
        public DbSet<SupplierRefund> SupplierRefunds => Set<SupplierRefund>();

        public DbSet<Dealer> Dealers => Set<Dealer>();
        public DbSet<Consignment> Consignments => Set<Consignment>();
        public DbSet<ConsignmentItem> ConsignmentItems => Set<ConsignmentItem>();
        public DbSet<ConsignmentSettlement> ConsignmentSettlements => Set<ConsignmentSettlement>();
        public DbSet<ConsignmentSettlementItem> ConsignmentSettlementItems => Set<ConsignmentSettlementItem>();
        public DbSet<ConsignmentPayment> ConsignmentPayments => Set<ConsignmentPayment>();

        public DbSet<Balance> Balances => Set<Balance>();
        public DbSet<BalanceClosure> BalanceClosures => Set<BalanceClosure>();

        public DbSet<User> Users => Set<User>();

        // حساب المطوّر والاشتراك (منفصلان عن Users فلا يظهران للعميل)
        public DbSet<DevUser> DevUsers => Set<DevUser>();
        public DbSet<License> Licenses => Set<License>();

        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Expense> Expenses => Set<Expense>();

        // أجهزة (موبايلات) مشتراة بغرض البيع: HQ والفروع
        public DbSet<Device> Devices => Set<Device>();

        // شركاء الفروع ونسب توزيع الأرباح
        public DbSet<BranchPartner> BranchPartners => Set<BranchPartner>();
        public DbSet<BranchPartnerSetting> BranchPartnerSettings => Set<BranchPartnerSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
