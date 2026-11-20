using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Expense;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.RMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        }

        // ================================================================
        // 0. الفروع من RMS (Stores from RMS)
        // ================================================================
        /// <summary>
        /// استرجاع قائمة الفروع لاختيار الفرع التابع له المصروف
        /// </summary>
        [HttpGet("stores")]
        public async Task<IActionResult> GetStores([FromServices] RMSContext rmsContext, CancellationToken ct)
        {
            var stores = await rmsContext.Stores
                .AsNoTracking()
                .OrderBy(s => s.ID)
                .Select(s => new { id = s.ID, name = s.Name ?? $"فرع {s.ID}" })
                .ToListAsync(ct);
            return Ok(stores);
        }

        // ================================================================
        // 1. بنود المصروفات (Categories)
        // ================================================================

        /// <summary>
        /// استرجاع كل بنود المصروفات المحفوظة
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<ExpenseCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories([FromQuery] bool includeInactive, CancellationToken ct)
        {
            var categories = await _expenseService.GetCategoriesAsync(includeInactive, ct);
            return Ok(categories);
        }

        /// <summary>
        /// إضافة بند مصروف جديد
        /// </summary>
        [HttpPost("categories")]
        [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _expenseService.CreateCategoryAsync(request, ct);
                return CreatedAtAction(nameof(GetCategories), new { id = result.Id }, result);
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

        /// <summary>
        /// تعديل بيانات بند مصروف
        /// </summary>
        [HttpPut("categories/{id:int}")]
        [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateExpenseCategoryRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _expenseService.UpdateCategoryAsync(id, request, ct);
                return Ok(result);
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

        /// <summary>
        /// تفعيل أو تعطيل بند مصروف
        /// </summary>
        [HttpPatch("categories/{id:int}/toggle-status")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleCategoryStatus(int id, CancellationToken ct)
        {
            try
            {
                var result = await _expenseService.ToggleCategoryStatusAsync(id, ct);
                return Ok(new { isActive = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        /// <summary>
        /// استرجاع قائمة المصروفات مع الفلاتر
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ExpenseResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? storeId,
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ExpensePaymentMethod? paymentMethod,
            CancellationToken ct)
        {
            var list = await _expenseService.GetExpensesAsync(storeId, categoryId, fromDate, toDate, paymentMethod, ct);
            return Ok(list);
        }

        /// <summary>
        /// استرجاع ملخص المصروفات والإحصائيات مقسمة حسب كل بند
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ExpenseSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int? storeId,
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ExpensePaymentMethod? paymentMethod,
            CancellationToken ct)
        {
            var summary = await _expenseService.GetExpensesSummaryAsync(storeId, categoryId, fromDate, toDate, paymentMethod, ct);
            return Ok(summary);
        }

        /// <summary>
        /// استرجاع تفاصيل مصروف معين بالـ ID
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id, ct);
            if (expense == null)
            {
                return NotFound(new { message = $"المصروف برقم {id} غير موجود." });
            }

            return Ok(expense);
        }

        /// <summary>
        /// تسجيل مصروف جديد (مع إمكانية اختيار بند أو كتابة بند جديد يتم حفظه تلقائياً)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _expenseService.CreateExpenseAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// تعديل حركة مصروف
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _expenseService.UpdateExpenseAsync(id, request, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// حذف حركة مصروف
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _expenseService.DeleteExpenseAsync(id, ct);
            if (!deleted)
            {
                return NotFound(new { message = $"المصروف برقم {id} غير موجود." });
            }

            return NoContent();
        }
    }
}
