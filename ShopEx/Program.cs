
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.RMS.Service;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Application.Services.RMS;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Infrastructure.Persistence.RMS;
using ShopEx.Infrastructure.Persistence.ShopDb;
using ShopEx.Infrastructure.Repositories.RMS;
using ShopEx.Infrastructure.Repositories.ShopEx;
using ShopEx.Infrastructure.Security;
using ShopEx.Security;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

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
            builder.Services.AddScoped<IStoreRepository, StoreRepository>();

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

            // Users & Auth
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IUserAuthStateProvider, CachedUserAuthStateProvider>();
            builder.Services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();

            // JWT
            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwtOptions.Key) || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
            {
                if (!builder.Environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "Jwt:Key غير مضبوط أو أقصر من 32 حرف. اضبطه من متغير بيئة Jwt__Key أو من الـ Secret Store.");
                }

                // في التطوير فقط: مفتاح مؤقت (تسجيل الدخول يُلغى عند إعادة تشغيل الـ API).
                // لمفتاح ثابت: dotnet user-secrets set "Jwt:Key" "<32+ chars>" --project ShopEx
                jwtOptions.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            }
            builder.Services.AddSingleton(Options.Create(jwtOptions));
            builder.Services.AddSingleton<ITokenService, JwtTokenService>();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false; // نستخدم أسماء الـ Claims كما هي (sub, role, storeId ...)
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        NameClaimType = AppClaims.Name,
                        RoleClaimType = AppClaims.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        // بعد التحقق من التوقيع: نتأكد أن المستخدم ما زال موجوداً وفعالاً ولم تتغير كلمة مروره
                        OnTokenValidated = async context =>
                        {
                            var principal = context.Principal!;
                            var userId = principal.UserId();
                            var role = principal.FindFirstValue(AppClaims.Role);

                            var validRole = role == AppRoles.Admin || (role == AppRoles.Employee && principal.StoreId().HasValue);
                            if (userId <= 0 || !validRole)
                            {
                                context.Fail("Invalid token claims.");
                                return;
                            }

                            var provider = context.HttpContext.RequestServices.GetRequiredService<IUserAuthStateProvider>();
                            var state = await provider.GetAsync(userId, context.HttpContext.RequestAborted);
                            var stamp = principal.FindFirstValue(AppClaims.SecurityStamp);

                            if (state is null || !state.IsActive ||
                                !string.Equals(state.SecurityStamp.ToString("N"), stamp, StringComparison.OrdinalIgnoreCase))
                            {
                                context.Fail("Session is no longer valid.");
                            }
                        }
                    };
                });

            // أي Endpoint بدون [Authorize]/[AllowAnonymous] يتطلب Admin (آمن افتراضياً).
            // الموظف لا يصل إلا للـ Endpoints المعلّمة صراحةً بـ AppRoles.AdminOrEmployee.
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireRole(AppRoles.Admin)
                    .Build();
            });

            // حد لمحاولات تسجيل الدخول لكل IP
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                options.OnRejected = async (context, ct) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"message\":\"محاولات كثيرة. حاول مرة أخرى بعد قليل.\"}", ct);
                };
            });

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    if (allowedOrigins.Length > 0)
                        policy.WithOrigins(allowedOrigins);
                    else if (builder.Environment.IsDevelopment())
                        policy.AllowAnyOrigin();

                    policy.AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "الصق التوكن الناتج من POST /api/Auth/login"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

            }

            // رؤوس أمان أساسية، ومنع تخزين ردود الـ API (بيانات مستخدمين/أرصدة) في الـ Cache
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers["X-Content-Type-Options"] = "nosniff";
                    headers["X-Frame-Options"] = "DENY";
                    headers["Referrer-Policy"] = "no-referrer";
                    if (context.Request.Path.StartsWithSegments("/api"))
                        headers["Cache-Control"] = "no-store";
                    return Task.CompletedTask;
                });
                await next();
            });

            app.UseCors("Frontend");

            app.UseHttpsRedirection();

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Seed default data if needed
            using (var scope = app.Services.CreateScope())
            {
                var shopContext = scope.ServiceProvider.GetRequiredService<ShopContext>();
                await ShopDbSeeder.SeedAsync(shopContext);
                await UserSeeder.SeedInitialAdminAsync(
                    shopContext,
                    builder.Configuration["InitialAdmin:Username"],
                    builder.Configuration["InitialAdmin:Password"],
                    app.Logger);
            }

            await app.RunAsync();
        }
    }
}
