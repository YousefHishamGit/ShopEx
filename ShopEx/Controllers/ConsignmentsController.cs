using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Consignment;
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
    public class ConsignmentsController : ControllerBase
    {
        private readonly IConsignmentService _consignmentService;

        public ConsignmentsController(IConsignmentService consignmentService)
        {
            _consignmentService = consignmentService ?? throw new ArgumentNullException(nameof(consignmentService));
        }

        // GET: api/Consignments
        [HttpGet]
        [ProducesResponseType(typeof(List<ConsignmentSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? dealerId,
            [FromQuery] ConsignmentStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            var list = await _consignmentService.GetAllAsync(dealerId, status, fromDate, toDate, ct);
            return Ok(list);
        }

        // GET: api/Consignments/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _consignmentService.GetByIdAsync(id, ct);
            if (result == null)
            {
                return NotFound(new { message = $"إذن الأمانة برقم {id} غير موجود." });
            }

            return Ok(result);
        }

        // GET: api/Consignments/by-number/{consignmentNumber}
        [HttpGet("by-number/{consignmentNumber}")]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByNumber(string consignmentNumber, CancellationToken ct)
        {
            var result = await _consignmentService.GetByNumberAsync(consignmentNumber, ct);
            if (result == null)
            {
                return NotFound(new { message = $"إذن الأمانة برقم {consignmentNumber} غير موجود." });
            }

            return Ok(result);
        }

        // POST: api/Consignments
        [HttpPost]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateConsignmentRequest request, CancellationToken ct)
        {
            var created = await _consignmentService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
        }

        // POST: api/Consignments/{id}/settle
        [HttpPost("{id:int}/settle")]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSettlement(int id, [FromBody] CreateConsignmentSettlementRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _consignmentService.AddSettlementAsync(id, request, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/Consignments/{id}/payments
        [HttpPost("{id:int}/payments")]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPayment(int id, [FromBody] AddConsignmentPaymentRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _consignmentService.AddPaymentAsync(id, request, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/Consignments/{id}/cancel
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(ConsignmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            try
            {
                var result = await _consignmentService.CancelConsignmentAsync(id, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
