using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;
using EnergyManagement.Services.Analytics;
using Services.Analytics;

namespace EnergyManagement.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IDailyBalanceDBService _dbService;
        private readonly IDailyBalanceCalculationService _service;

        public AnalyticsController(IDailyBalanceDBService dbService, IDailyBalanceCalculationService service)
        {
            _dbService = dbService;
            _service = service;
        }

        [HttpPost("calculate/{addressId}/{day}")]
        public async Task<IActionResult> Calculate(int addressId, DateOnly day)
            => Ok(await _service.CalculateDailyBalancesAsync(addressId, day));

        [HttpGet("dailybalance")]
        public async Task<IActionResult> Get()
        {
            var result = await _dbService.GetAllAsync();
            return Ok(result);
        }
    }
}