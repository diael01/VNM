using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;
using EnergyManagement.Services.Analytics;

namespace EnergyManagement.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly VnmDbContext _db;
        private readonly IDailyBalanceCalculationService _service;

        public AnalyticsController(VnmDbContext db, IDailyBalanceCalculationService service)
        {
            _db = db;
            _service = service;
        }

        [HttpPost("calculate/{addressId}/{day}")]
        public async Task<IActionResult> Calculate(int addressId, DateOnly day)
            => Ok(await _service.CalculateDailyBalancesAsync(addressId, day));

        [HttpGet("balance/{addressId}/{day}")]
        public async Task<IActionResult> Get(int addressId, DateOnly day)
        {
            var result = await _db.DailyEnergyBalances
                .FirstOrDefaultAsync(x => x.LocationId == addressId && x.Day.HasValue && DateOnly.FromDateTime(x.Day.Value) == day);

            return result == null ? NotFound() : Ok(result);
        }
    }
}