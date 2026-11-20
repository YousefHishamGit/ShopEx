using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Stock;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IStockService _stockService;

        public WarehousesController(IStockService stockService)
        {
            _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        }

        // GET: api/Warehouses
        [HttpGet]
        [ProducesResponseType(typeof(List<WarehouseResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly, CancellationToken ct)
        {
            var warehouses = await _stockService.GetWarehousesAsync(activeOnly, ct);
            return Ok(warehouses);
        }

        // GET: api/Warehouses/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var warehouse = await _stockService.GetWarehouseByIdAsync(id, ct);
            if (warehouse == null)
            {
                return NotFound(new { message = $"المخزن برقم {id} غير موجود." });
            }

            return Ok(warehouse);
        }

        // POST: api/Warehouses
        [HttpPost]
        [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
        {
            try
            {
                var created = await _stockService.CreateWarehouseAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Warehouses/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(WarehouseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
        {
            try
            {
                var updated = await _stockService.UpdateWarehouseAsync(id, request, ct);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
