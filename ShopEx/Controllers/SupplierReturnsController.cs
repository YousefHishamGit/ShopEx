using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.SupplierReturn;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierReturnsController : ControllerBase
    {
        private readonly ISupplierReturnService _returnService;

        public SupplierReturnsController(ISupplierReturnService returnService)
        {
            _returnService = returnService ?? throw new ArgumentNullException(nameof(returnService));
        }

        // GET: api/SupplierReturns
        [HttpGet]
        [ProducesResponseType(typeof(List<SupplierReturnSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? supplierId,
            [FromQuery] SupplierReturnStatus? status,
            [FromQuery] int? warehouseId,
            CancellationToken ct)
        {
            var returns = await _returnService.GetAllAsync(supplierId, status, warehouseId, ct);
            return Ok(returns);
        }

        // GET: api/SupplierReturns/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _returnService.GetByIdAsync(id, ct);
            if (result == null)
            {
                return NotFound(new { message = $"مرتجع المورد برقم {id} غير موجود." });
            }

            return Ok(result);
        }

        // GET: api/SupplierReturns/by-number/{returnNumber}
        [HttpGet("by-number/{returnNumber}")]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByNumber(string returnNumber, CancellationToken ct)
        {
            var result = await _returnService.GetByReturnNumberAsync(returnNumber, ct);
            if (result == null)
            {
                return NotFound(new { message = $"مرتجع المورد برقم {returnNumber} غير موجود." });
            }

            return Ok(result);
        }

        // POST: api/SupplierReturns
        [HttpPost]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateSupplierReturnRequest request, CancellationToken ct)
        {
            try
            {
                var created = await _returnService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/SupplierReturns/{id}/confirm
        [HttpPost("{id:int}/confirm")]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirm(int id, CancellationToken ct)
        {
            try
            {
                var result = await _returnService.ConfirmReturnAsync(id, ct);
                return Ok(result);
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

        // POST: api/SupplierReturns/{id}/cancel
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            try
            {
                var result = await _returnService.CancelReturnAsync(id, ct);
                return Ok(result);
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

        // POST: api/SupplierReturns/{id}/refunds
        [HttpPost("{id:int}/refunds")]
        [ProducesResponseType(typeof(SupplierReturnResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddRefund(int id, [FromBody] AddSupplierRefundRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _returnService.AddRefundAsync(id, request, ct);
                return Ok(result);
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
