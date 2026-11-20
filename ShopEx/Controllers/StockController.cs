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
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        }

        // GET: api/Stock/balances?warehouseId=1&lowStockOnly=false
        [HttpGet("balances")]
        [ProducesResponseType(typeof(List<StockBalanceResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBalances(
            [FromQuery] int warehouseId,
            [FromQuery] bool lowStockOnly,
            CancellationToken ct)
        {
            var balances = await _stockService.GetBalancesAsync(warehouseId, lowStockOnly, ct);
            return Ok(balances);
        }

        // GET: api/Stock/balances/{warehouseId}/{rmsItemId}
        [HttpGet("balances/{warehouseId:int}/{rmsItemId:int}")]
        [ProducesResponseType(typeof(StockBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItemBalance(int warehouseId, int rmsItemId, CancellationToken ct)
        {
            var balance = await _stockService.GetItemBalanceAsync(warehouseId, rmsItemId, ct);
            if (balance == null)
            {
                return NotFound(new { message = $"لا يوجد رصيد مسجل للصنف {rmsItemId} في المخزن {warehouseId}." });
            }

            return Ok(balance);
        }

        // GET: api/Stock/movements
        [HttpGet("movements")]
        [ProducesResponseType(typeof(List<StockMovementResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMovements(
            [FromQuery] int? warehouseId,
            [FromQuery] int? rmsItemId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            var movements = await _stockService.GetMovementsAsync(warehouseId, rmsItemId, fromDate, toDate, ct);
            return Ok(movements);
        }

        // POST: api/Stock/adjust
        [HttpPost("adjust")]
        [ProducesResponseType(typeof(StockBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _stockService.AdjustStockAsync(request, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
