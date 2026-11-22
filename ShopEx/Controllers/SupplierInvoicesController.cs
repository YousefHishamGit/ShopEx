using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierInvoicesController : ControllerBase
    {
        private readonly ISupplierInvoiceService _invoiceService;

        public SupplierInvoicesController(ISupplierInvoiceService invoiceService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        }

        // GET: api/SupplierInvoices
        [HttpGet]
        [ProducesResponseType(typeof(List<SupplierInvoiceResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int? supplierId,
            CancellationToken ct)
        {
            // Paged + filtered in SQL when paging params are supplied
            if (page.HasValue || pageSize.HasValue)
            {
                var paged = await _invoiceService.GetPagedAsync(page ?? 1, pageSize ?? 10, search, status, supplierId, ct);
                return Ok(paged);
            }

            var invoices = await _invoiceService.GetAllAsync(ct);
            return Ok(invoices);
        }

        // GET: api/SupplierInvoices/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SupplierInvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var invoice = await _invoiceService.GetByIdAsync(id, ct);
            if (invoice == null)
            {
                return NotFound(new { message = $"Supplier invoice with ID {id} was not found." });
            }

            return Ok(invoice);
        }

        // POST: api/SupplierInvoices
        [HttpPost]
        [ProducesResponseType(typeof(SupplierInvoiceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateSupplierInvoiceRequest request, CancellationToken ct)
        {
            try
            {
                var createdInvoice = await _invoiceService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = createdInvoice.ID }, createdInvoice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/SupplierInvoices/{id}/payments
        [HttpPost("{id:int}/payments")]
        [ProducesResponseType(typeof(SupplierInvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPayment(int id, [FromBody] AddPaymentRequest request, CancellationToken ct)
        {
            try
            {
                var updatedInvoice = await _invoiceService.AddPaymentAsync(id, request, ct);
                return Ok(updatedInvoice);
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
    }
}

