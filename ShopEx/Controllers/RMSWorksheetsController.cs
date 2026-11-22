using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.interfaces.RMS.Service;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMSWorksheetsController : ControllerBase
    {
        private readonly IWorksheetService _worksheetService;

        public RMSWorksheetsController(IWorksheetService worksheetService)
        {
            _worksheetService = worksheetService ?? throw new ArgumentNullException(nameof(worksheetService));
        }

        // GET: api/RMSWorksheets
        [HttpGet]
        [ProducesResponseType(typeof(List<WorksheetResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var worksheets = await _worksheetService.GetAllAsync(cancellationToken);
            return Ok(worksheets);
        }

        // GET: api/RMSWorksheets/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(WorksheetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var worksheet = await _worksheetService.GetByIdAsync(id, cancellationToken);
            if (worksheet == null)
            {
                return NotFound(new { message = $"Worksheet with ID {id} was not found." });
            }

            return Ok(worksheet);
        }

        // POST: api/RMSWorksheets (Open Worksheet - Status 1)
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> OpenWorksheet([FromBody] OpenWorksheetDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var worksheetId = await _worksheetService.OpenWorksheetAsync(dto, cancellationToken);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = worksheetId },
                    new
                    {
                        worksheetId,
                        status = 1,
                        message = "Worksheet opened successfully with status 1 (PendingApproval)."
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/RMSWorksheets/with-purchases (Open Worksheet with Purchase Lines)
        [HttpPost("with-purchases")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> OpenWorksheetWithPurchases([FromBody] OpenWorksheetWithPurchasesRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = new OpenWorksheetDto { Title = request.Title, Notes = request.Notes };
                var worksheetId = await _worksheetService.OpenWorksheetWithPurchasesAsync(dto, request.Items, cancellationToken);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = worksheetId },
                    new
                    {
                        worksheetId,
                        status = 1,
                        message = "Worksheet opened with purchase lines successfully with status 1 (PendingApproval)."
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/RMSWorksheets/transfer (create approved worksheet, status 2, sending items to a branch)
        [HttpPost("transfer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Transfer([FromBody] TransferWorksheetRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var worksheetId = await _worksheetService.CreateApprovedTransferAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = worksheetId },
                    new { worksheetId, status = 2, message = $"Worksheet {worksheetId} approved (status 2)." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/RMSWorksheets/{id}/purchases (Add Purchase Orders to existing open worksheet)
        [HttpPost("{id:int}/purchases")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddPurchases(int id, [FromBody] List<PurchaseLineDto> items, CancellationToken cancellationToken)
        {
            try
            {
                await _worksheetService.AddPurchasesAsync(id, items, cancellationToken);
                return Ok(new
                {
                    message = $"Purchase orders added successfully to Worksheet {id}.",
                    worksheetId = id
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/RMSWorksheets/{id}/approve (Approve Worksheet -> converts status to 2)
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveWorksheetRequest? request, CancellationToken cancellationToken)
        {
            try
            {
                await _worksheetService.ApproveWorksheetAsync(id, request?.DestinationStoreId, cancellationToken);
                return Ok(new
                {
                    worksheetId = id,
                    status = 2,
                    message = $"Worksheet {id} approved successfully and status converted to 2 (Approved)."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
