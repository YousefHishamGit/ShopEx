using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.interfaces.RMS.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMSItemsController : ControllerBase
    {
        private readonly IItemRepository _itemRepository;

        public RMSItemsController(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        // GET: api/RMSItems/search?query=...&limit=50
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] int limit = 50)
        {
            var items = await _itemRepository.SearchItemsAsync(query, limit);
            var result = items.Select(i => new ItemResponse
            {
                ID = i.ID,
                Description = i.Description ?? $"صنف #{i.ID}",
                ItemLookupCode = i.ItemLookupCode,
                Price = i.Price,
                PriceA = i.PriceA,
                Cost = i.Cost,
                LastCost = i.LastCost,
                Quantity = i.Quantity,
                SupplierID = i.SupplierID
            });
            return Ok(result);
        }

        // GET: api/RMSItems/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"الصنف برقم {id} غير موجود في قاعدة بيانات RMS." });
            }

            return Ok(new ItemResponse
            {
                ID = item.ID,
                Description = item.Description ?? $"صنف #{item.ID}",
                ItemLookupCode = item.ItemLookupCode,
                Price = item.Price,
                PriceA = item.PriceA,
                Cost = item.Cost,
                LastCost = item.LastCost,
                Quantity = item.Quantity,
                SupplierID = item.SupplierID
            });
        }

        // GET: api/RMSItems/barcode/{barcode}
        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var item = await _itemRepository.GetByLookupCodeAsync(barcode);
            if (item == null)
            {
                return NotFound(new { message = $"الصنف بالباركود '{barcode}' غير موجود في قاعدة بيانات RMS." });
            }

            return Ok(new ItemResponse
            {
                ID = item.ID,
                Description = item.Description ?? $"صنف #{item.ID}",
                ItemLookupCode = item.ItemLookupCode,
                Price = item.Price,
                PriceA = item.PriceA,
                Cost = item.Cost,
                LastCost = item.LastCost,
                Quantity = item.Quantity,
                SupplierID = item.SupplierID
            });
        }
    }
}
