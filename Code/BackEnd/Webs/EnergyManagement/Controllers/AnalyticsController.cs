using Microsoft.AspNetCore.Mvc;
using Repositories.Models;
using Microsoft.EntityFrameworkCore;
using EnergyManagement.Services.Analytics;
using Repositories.CRUD.Repositories;

namespace EnergyManagement.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IDailyEnergyBalanceRepository _dailyEnergyBalanceRepository;
        private readonly IDailyBalanceCalculationService _service;

        public AnalyticsController(IDailyEnergyBalanceRepository dailyEnergyBalanceRepository, IDailyBalanceCalculationService service)
        {
            _dailyEnergyBalanceRepository = dailyEnergyBalanceRepository;
            _service = service;
        }

        [HttpGet("dailybalance")]
        public async Task<IActionResult> Get()
        {
            var result = await _dailyEnergyBalanceRepository.GetAllAsync();
            return Ok(result);
        }
    }
}