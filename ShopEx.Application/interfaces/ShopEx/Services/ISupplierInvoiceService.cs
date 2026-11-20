using ShopEx.Application.DTOs.ShopEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface ISupplierInvoiceService
    {
        Task<SupplierInvoiceResponse> CreateAsync(CreateSupplierInvoiceRequest request, CancellationToken ct = default);
        Task<SupplierInvoiceResponse> AddPaymentAsync(int invoiceId, AddPaymentRequest request, CancellationToken ct = default);
        Task<SupplierInvoiceResponse?> GetByIdAsync(int invoiceId, CancellationToken ct = default);
        Task<List<SupplierInvoiceResponse>> GetAllAsync(CancellationToken ct = default);
    }
}
