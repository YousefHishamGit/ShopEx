using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Maintenance;
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
    public class MaintenanceTicketsController : ControllerBase
    {
        private readonly IMaintenanceTicketService _ticketService;

        public MaintenanceTicketsController(IMaintenanceTicketService ticketService)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        // GET: api/MaintenanceTickets
        [HttpGet]
        [ProducesResponseType(typeof(List<MaintenanceTicketSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] MaintenanceStatus? status,
            [FromQuery] string? customerPhone,
            CancellationToken ct)
        {
            var tickets = await _ticketService.GetAllAsync(status, customerPhone, ct);
            return Ok(tickets);
        }

        // GET: api/MaintenanceTickets/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var ticket = await _ticketService.GetByIdAsync(id, ct);
            if (ticket == null)
            {
                return NotFound(new { message = $"تذكرة الصيانة برقم {id} غير موجودة." });
            }

            return Ok(ticket);
        }

        // GET: api/MaintenanceTickets/by-number/{ticketNumber}
        [HttpGet("by-number/{ticketNumber}")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByTicketNumber(string ticketNumber, CancellationToken ct)
        {
            try
            {
                var ticket = await _ticketService.GetByTicketNumberAsync(ticketNumber, ct);
                if (ticket == null)
                {
                    return NotFound(new { message = $"تذكرة الصيانة ذات الرقم '{ticketNumber}' غير موجودة." });
                }

                return Ok(ticket);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets (المرحلة الأولى: استلام من العميل)
        [HttpPost]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMaintenanceTicketRequest request, CancellationToken ct)
        {
            try
            {
                var createdTicket = await _ticketService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = createdTicket.Id }, createdTicket);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets/{id}/operations (المرحلة الثانية: تسجيل الإصلاحات وتكلفتها)
        [HttpPost("{id:int}/operations")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddOperation(int id, [FromBody] AddTicketOperationRequest request, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.AddOperationAsync(id, request, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/MaintenanceTickets/{id}/operations/{operationId}
        [HttpDelete("{id:int}/operations/{operationId:int}")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOperation(int id, int operationId, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.DeleteOperationAsync(id, operationId, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets/{id}/complete-repair (المرحلة الثانية: تأكيد إتمام الإصلاح)
        [HttpPost("{id:int}/complete-repair")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteRepair(int id, [FromBody] CompleteRepairRequest? request, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.CompleteRepairAsync(id, request, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets/{id}/payments (تسجيل دفعة نقدية)
        [HttpPost("{id:int}/payments")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPayment(int id, [FromBody] AddTicketPaymentRequest request, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.AddPaymentAsync(id, request, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets/{id}/deliver (المرحلة الثالثة: تسليم الجهاز ودفع التكلفة)
        [HttpPost("{id:int}/deliver")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deliver(int id, [FromBody] DeliverTicketRequest? request, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.DeliverTicketAsync(id, request, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/MaintenanceTickets/{id}/status (تحديث يدوي للحالة)
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] MaintenanceStatus status, CancellationToken ct)
        {
            try
            {
                var updatedTicket = await _ticketService.UpdateStatusAsync(id, status, ct);
                return Ok(updatedTicket);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
