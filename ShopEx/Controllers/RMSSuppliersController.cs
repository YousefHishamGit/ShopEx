using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.interfaces.RMS.Service;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMSSuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public RMSSuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // GET: api/RMSSuppliers
        [HttpGet]
        public async Task<IActionResult> GetAllSuppliers(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null)
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            if (suppliers == null)
            {
                return Ok(new List<ShopEx.Application.DTOs.RMS.SupplierResponse>());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                suppliers = suppliers.Where(s =>
                    (!string.IsNullOrEmpty(s.SupplierName) && s.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Code) && s.Code.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    s.ID.ToString().Contains(term));
            }

            if (page.HasValue && pageSize.HasValue && pageSize.Value > 0)
            {
                var totalCount = suppliers.Count();
                var p = Math.Max(1, page.Value);
                var size = pageSize.Value;
                var items = suppliers.Skip((p - 1) * size).Take(size).ToList();

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = p,
                    pageSize = size,
                    totalPages = (int)Math.Ceiling((double)totalCount / size)
                });
            }

            return Ok(suppliers);
        }

        // GET: api/RMSSuppliers/{id}/items
        [HttpGet("{id}/items")]
        public async Task<IActionResult> GetItemsBySupplier(int id)
        {
            var items = await _supplierService.GetItemsBySupplierAsync(id);


            if (items == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} was not found." });
            }

            return Ok(items);
        }

        [HttpGet("{id}/statement")]
        public async Task<IActionResult> GetSupplierStatement(int id)
        {
            var statement = await _supplierService.GetSupplierStatementAsync(id);

            if (statement == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} was not found." });
            }

            return Ok(statement);
        }
    }
}
