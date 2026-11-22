using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEx.Infrastructure.Persistence.RMS;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoresController : ControllerBase
    {
        private readonly RMSContext _rmsContext;

        public StoresController(RMSContext rmsContext)
        {
            _rmsContext = rmsContext;
        }

        // GET: api/Stores
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var stores = await _rmsContext.Stores
                .OrderBy(s => s.ID)
                .Select(s => new
                {
                    storeId = s.ID,
                    storeName = s.Name ?? $"فرع #{s.ID}",
                    code = s.StoreCode,
                    city = s.City,
                    address = s.Address1,
                    phone = s.PhoneNumber
                })
                .ToListAsync(ct);

            return Ok(stores);
        }
    }
}
