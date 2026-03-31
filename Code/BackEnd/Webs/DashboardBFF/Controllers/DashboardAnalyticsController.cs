using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Redirect;
using System.Threading;
using System.Threading.Tasks;

namespace DashboardBFF.Controllers
{
	[ApiController]
	[Route("api/v1/dashboard/analytics")]
	public class DashboardAnalyticsController : ControllerBase
	{
		private readonly IDashboardDailyBalanceRedirectService _dailyBalanceRedirectService;
		public DashboardAnalyticsController(IDashboardDailyBalanceRedirectService dailyBalanceRedirectService)
		{
			_dailyBalanceRedirectService = dailyBalanceRedirectService;
		}

		  [HttpGet("dailybalance")]
		[Authorize]
		public async Task<IActionResult> GetDailyBalance(CancellationToken cancellationToken)
		{
			var accessToken = await Infrastructure.Utils.HttpContextAccessTokenHelper.GetAccessTokenOrThrowAsync(HttpContext, cancellationToken);
			var balances = await _dailyBalanceRedirectService.GetDailyBalanceAsync(accessToken, cancellationToken);
			return Ok(balances);
		}
	}
}
