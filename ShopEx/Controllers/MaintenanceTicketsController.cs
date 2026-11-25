using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Maintenance;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using ShopEx.Security;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // الافتراضي: Admin فقط (FallbackPolicy). الموظف مسموح له بخمس عمليات فقط:
    // عرض (قائمة/تفاصيل/بحث برقم التذكرة) واستلام جهاز وتسليم جهاز جاهز، وكلها مقيدة بفرعه.
    public class MaintenanceTicketsController : ControllerBase
    {
        private readonly IMaintenanceTicketService _ticketService;

        public MaintenanceTicketsController(IMaintenanceTicketService ticketService)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        /// <summary>الموظف يرى تذاكر فرعه فقط؛ الـ Admin يرى الكل.</summary>
        private int? ScopeStoreId => User.IsAdmin() ? null : (User.StoreId() ?? -1);

        private bool CanAccess(int? ticketStoreId)
            => User.IsAdmin() || (ticketStoreId.HasValue && ticketStoreId == User.StoreId());

        /// <summary>تكلفة القطع والربح للإدارة فقط: تُخفى عن الموظف في أي رد.</summary>
        private MaintenanceTicketResponse HideCosts(MaintenanceTicketResponse ticket)
        {
            if (User.IsAdmin()) return ticket;
            ticket.TotalPartCost = null;
            ticket.Profit = null;
            foreach (var op in ticket.Operations) op.PartCost = null;
            return ticket;
        }

        // GET: api/MaintenanceTickets
        [HttpGet]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(List<MaintenanceTicketSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] MaintenanceStatus? status,
            [FromQuery] string? customerPhone,
            CancellationToken ct)
        {
            var tickets = await _ticketService.GetAllAsync(status, customerPhone, ScopeStoreId, ct);
            return Ok(tickets);
        }

        // GET: api/MaintenanceTickets/paged
        [HttpGet("paged")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(PagedMaintenanceTicketsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] MaintenanceStatus? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? storeId = null,
            CancellationToken ct = default)
        {
            // الأدمن يقدر يفلتر بفرع؛ الموظف دايماً مقيد بفرعه من التوكن (يتجاهل storeId)
            var scope = User.IsAdmin() ? storeId : ScopeStoreId;
            var result = await _ticketService.GetPagedAsync(status, search, scope, page, pageSize, ct);

            if (!User.IsAdmin())
            {
                // التكلفة والربح للإدارة فقط
                result.TotalPartCost = null;
                result.TotalProfit = null;
            }

            return Ok(result);
        }

        // GET: api/MaintenanceTickets/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var ticket = await _ticketService.GetByIdAsync(id, ct);
            if (ticket == null || !CanAccess(ticket.StoreId))
            {
                return NotFound(new { message = $"تذكرة الصيانة برقم {id} غير موجودة." });
            }

            return Ok(HideCosts(ticket));
        }

        // GET: api/MaintenanceTickets/by-number/{ticketNumber}
        [HttpGet("by-number/{ticketNumber}")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByTicketNumber(string ticketNumber, CancellationToken ct)
        {
            try
            {
                var ticket = await _ticketService.GetByTicketNumberAsync(ticketNumber, ct);
                if (ticket == null || !CanAccess(ticket.StoreId))
                {
                    return NotFound(new { message = $"تذكرة الصيانة ذات الرقم '{ticketNumber}' غير موجودة." });
                }

                return Ok(HideCosts(ticket));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/MaintenanceTickets (المرحلة الأولى: استلام من العميل)
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMaintenanceTicketRequest request, CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(new { message = "بيانات التذكرة مطلوبة." });
            }

            if (User.IsAdmin())
            {
                request.StoreId ??= User.StoreId();
            }
            else
            {
                // الموظف: الفرع واسم المستلم من التوكن وليس من الطلب
                request.StoreId = User.StoreId();
                request.ReceivedBy = User.UserName();
            }

            try
            {
                var createdTicket = await _ticketService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = createdTicket.Id }, HideCosts(createdTicket));
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
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(MaintenanceTicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deliver(int id, [FromBody] DeliverTicketRequest? request, CancellationToken ct)
        {
            try
            {
                if (!User.IsAdmin())
                {
                    // الموظف يسلّم فقط الأجهزة الجاهزة التي تخص فرعه
                    var existing = await _ticketService.GetByIdAsync(id, ct);
                    if (existing == null || !CanAccess(existing.StoreId))
                    {
                        return NotFound(new { message = $"تذكرة الصيانة برقم {id} غير موجودة." });
                    }

                    if (existing.Status != MaintenanceStatus.Completed)
                    {
                        return BadRequest(new { message = "لا يمكن تسليم الجهاز قبل أن يصبح جاهزاً." });
                    }

                    // الموظف لا يسلّم الجهاز قبل تحصيل كامل المتبقي (لو لم يُحدد مبلغ يُحصّل المتبقي كاملاً)
                    if (request?.PaymentAmount is decimal paid && paid < existing.RemainingAmount)
                    {
                        return BadRequest(new { message = $"يجب تحصيل كامل المبلغ المتبقي ({existing.RemainingAmount:N2}) قبل التسليم." });
                    }
                }

                var updatedTicket = await _ticketService.DeliverTicketAsync(id, request, ct);
                return Ok(HideCosts(updatedTicket));
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
