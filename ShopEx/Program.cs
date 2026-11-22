
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.RMS.Service;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Application.Services.RMS;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Infrastructure.Persistence.RMS;
using ShopEx.Infrastructure.Persistence.ShopDb;
using ShopEx.Infrastructure.Repositories.RMS;
using ShopEx.Infrastructure.Repositories.ShopEx;
using System;

namespace ShopEx
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<RMSContext>(opt =>
               opt.UseSqlServer(builder.Configuration.GetConnectionString("RMSConnection"),
                  // RMS database has an older compatibility level (no OPENJSON): translate Contains() to IN (...)
                  sql => sql.UseCompatibilityLevel(120))
            );

            builder.Services.AddDbContext<ShopContext>(opt =>
               opt.UseSqlServer(builder.Configuration.GetConnectionString("ShopEXConnection"),
                  sql => sql.UseCompatibilityLevel(120))
            );

            // Add services to the container.
            // RMS
            builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
            builder.Services.AddScoped<IWorksheetRepository,WorksheetRepository>();
            builder.Services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
  
            builder.Services.AddScoped<ISupplierService, SupplierService>();
            builder.Services.AddScoped<IWorksheetService, WorksheetService>();
            builder.Services.AddScoped<ICustomerStatementService, CustomerStatementService>();

            // ShopEx
            builder.Services.AddScoped<ISupplierInvoiceRepository, SupplierInvoiceRepository>();
            builder.Services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
            builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            builder.Services.AddScoped<IStockRepository, StockRepository>();
            builder.Services.AddScoped<ISupplierReturnRepository, SupplierReturnRepository>();
            builder.Services.AddScoped<IDealerRepository, DealerRepository>();
            builder.Services.AddScoped<IConsignmentRepository, ConsignmentRepository>();
            builder.Services.AddScoped<IBalanceRepository, BalanceRepository>();
            builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
            builder.Services.AddScoped<IMaintenanceTicketService, MaintenanceTicketService>();
            builder.Services.AddScoped<IStockService, StockService>();
            builder.Services.AddScoped<ISupplierReturnService, SupplierReturnService>();
            builder.Services.AddScoped<IDealerService, DealerService>();
            builder.Services.AddScoped<IConsignmentService, ConsignmentService>();
            builder.Services.AddScoped<IBalanceService, BalanceService>();
            builder.Services.AddScoped<IExpenseService, ExpenseService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            // Seed default data if needed
            using (var scope = app.Services.CreateScope())
            {
                var shopContext = scope.ServiceProvider.GetRequiredService<ShopContext>();
                await ShopDbSeeder.SeedAsync(shopContext);
            }

            await app.RunAsync();
        }
    }
}
