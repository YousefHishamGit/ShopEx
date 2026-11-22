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
            if (page.HasValue && pageSize.HasValue && pageSize.Value > 0)
            {
                var p = Math.Max(1, page.Value);
                var size = Math.Min(pageSize.Value, 100);
                var (items, totalCount) = await _supplierService.GetSuppliersPagedAsync(search, p, size);

                return Ok(new
                {
                    items,
                    totalCount,
                    pageNumber = p,
                    pageSize = size,
                    totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / size))
                });
            }

            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return Ok(suppliers ?? new List<ShopEx.Application.DTOs.RMS.SupplierResponse>());
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
        public async Task<IActionResult> GetSupplierStatement(
            int id,
            [FromQuery] string? status = null,
            [FromQuery] string? section = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var statement = await _supplierService.GetSupplierStatementAsync(id, status, section, page, pageSize, search);

            if (statement == null)
            {
                return NotFound(new { message = $"Supplier with ID {id} was not found." });
            }

            return Ok(statement);
        }
    }
}
