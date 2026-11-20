using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.interfaces.RMS.Service;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerStatementsController : ControllerBase
    {
        private readonly ICustomerStatementService _statementService;

        public CustomerStatementsController(ICustomerStatementService statementService)
        {
            _statementService = statementService ?? throw new ArgumentNullException(nameof(statementService));
        }

        // GET: api/CustomerStatements/customers?search=...&minDaysOld=15&page=1&pageSize=20
        [HttpGet("customers")]
        [ProducesResponseType(typeof(PagedResponse<CustomerSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] int? minDaysOld,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _statementService.GetPagedCustomersAsync(search, minDaysOld, page, pageSize, ct);
            return Ok(result);
        }

        // GET: api/CustomerStatements/open-invoices?customerId=...&storeId=...&minDaysOld=15&search=...&page=1&pageSize=20
        [HttpGet("open-invoices")]
        [ProducesResponseType(typeof(PagedResponse<CustomerOpenInvoiceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOpenInvoices(
            [FromQuery] int? customerId,
            [FromQuery] int? storeId,
            [FromQuery] int? minDaysOld,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _statementService.GetPagedOpenInvoicesAsync(customerId, storeId, minDaysOld, search, page, pageSize, ct);
            return Ok(result);
        }

        // GET: api/CustomerStatements/customers/{customerId}?minDaysOld=15
        [HttpGet("customers/{customerId:int}")]
        [ProducesResponseType(typeof(CustomerStatementDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerStatement(
            int customerId,
            [FromQuery] int? minDaysOld,
            CancellationToken ct = default)
        {
            var statement = await _statementService.GetCustomerStatementAsync(customerId, minDaysOld, ct);
            if (statement == null)
            {
                return NotFound(new { message = $"العميل برقم {customerId} غير موجود." });
            }

            return Ok(statement);
        }

        // GET: api/CustomerStatements/by-account/{accountNumber}?minDaysOld=15
        [HttpGet("by-account/{accountNumber}")]
        [ProducesResponseType(typeof(CustomerStatementDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerStatementByAccount(
            string accountNumber,
            [FromQuery] int? minDaysOld,
            CancellationToken ct = default)
        {
            var statement = await _statementService.GetCustomerStatementByAccountAsync(accountNumber, minDaysOld, ct);
            if (statement == null)
            {
                return NotFound(new { message = $"لا يوجد عميل برقم الحساب ({accountNumber})." });
            }

            return Ok(statement);
        }

        // GET: api/CustomerStatements/invoices/{orderId}
        [HttpGet("invoices/{orderId:int}")]
        [ProducesResponseType(typeof(CustomerInvoiceDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceDetail(int orderId, CancellationToken ct = default)
        {
            var invoice = await _statementService.GetInvoiceDetailAsync(orderId, ct);
            if (invoice == null)
            {
                return NotFound(new { message = $"الفاتورة رقم {orderId} غير موجودة." });
            }

            return Ok(invoice);
        }
    }
}
