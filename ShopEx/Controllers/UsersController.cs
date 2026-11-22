using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Security;

namespace ShopEx.Controllers
{
    /// <summary>إدارة المستخدمين: للـ Admin فقط.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/Users
        [HttpGet]
        [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            return Ok(await _userService.GetAllAsync(ct));
        }

        // POST: api/Users  (إضافة مستخدم)
        [HttpPost]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        {
            return await Run(async () =>
            {
                var created = await _userService.CreateAsync(request, ct);
                return StatusCode(StatusCodes.Status201Created, created);
            });
        }

        // PUT: api/Users/{id}/password  (تغيير كلمة المرور)
        [HttpPut("{id:int}/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] UpdatePasswordRequest request, CancellationToken ct)
        {
            return await Run(async () =>
            {
                await _userService.UpdatePasswordAsync(id, request, ct);
                return NoContent();
            });
        }

        // POST: api/Users/{id}/disable  (تعطيل)
        [HttpPost("{id:int}/disable")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Disable(int id, CancellationToken ct)
        {
            return await Run(async () => Ok(await _userService.SetActiveAsync(id, false, User.UserId(), ct)));
        }

        // POST: api/Users/{id}/enable  (إعادة تفعيل)
        [HttpPost("{id:int}/enable")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Enable(int id, CancellationToken ct)
        {
            return await Run(async () => Ok(await _userService.SetActiveAsync(id, true, User.UserId(), ct)));
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            return await Run(async () =>
            {
                await _userService.DeleteAsync(id, User.UserId(), ct);
                return NoContent();
            });
        }

        private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
