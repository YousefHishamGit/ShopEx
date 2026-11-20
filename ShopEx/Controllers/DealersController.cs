using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Consignment;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DealersController : ControllerBase
    {
        private readonly IDealerService _dealerService;

        public DealersController(IDealerService dealerService)
        {
            _dealerService = dealerService ?? throw new ArgumentNullException(nameof(dealerService));
        }

        // GET: api/Dealers
        [HttpGet]
        [ProducesResponseType(typeof(List<DealerResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool? activeOnly,
            [FromQuery] string? search,
            CancellationToken ct)
        {
            var dealers = await _dealerService.GetAllAsync(activeOnly, search, ct);
            return Ok(dealers);
        }

        // GET: api/Dealers/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DealerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var dealer = await _dealerService.GetByIdAsync(id, ct);
            if (dealer == null)
            {
                return NotFound(new { message = $"التاجر برقم {id} غير موجود." });
            }

            return Ok(dealer);
        }

        // POST: api/Dealers
        [HttpPost]
        [ProducesResponseType(typeof(DealerResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateDealerRequest request, CancellationToken ct)
        {
            try
            {
                var created = await _dealerService.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Dealers/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(DealerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDealerRequest request, CancellationToken ct)
        {
            try
            {
                var updated = await _dealerService.UpdateAsync(id, request, ct);
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

        // DELETE: api/Dealers/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _dealerService.DeleteAsync(id, ct);
                return NoContent();
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
