using Services.Redirect;
using EventBusCore.Events;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Auth;
using VNM.Infrastructure.Configuration;
using IAuthenticationService = Services.Auth.IAuthenticationService;

namespace DashboardBFFWeb.Controllers
{
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDashboardRedirectService _dashboardService;
        private readonly string _frontendBaseUrl;

        public DashboardController(
            IPublishEndpoint publishEndpoint,
            IAuthenticationService authService,
            IDashboardRedirectService dashboardService,
            IOptions<FrontendOptions> frontendOptions)
        {
            _publishEndpoint = publishEndpoint;
            _authenticationService = authService;
            _dashboardService = dashboardService;
            _frontendBaseUrl = frontendOptions.Value.BaseUrl;
        }

        [HttpGet("status")]
        [Route("api/dashboard/status")]
        public async Task<IActionResult> GetStatus()
        {
            var evt = new DashboardStatusEvent
            {
                Message = "Service is running"
            };

            await _publishEndpoint.Publish(evt);
            return Ok(new { Service = "DashboardBFFWeb", Status = "Running" });
        }

        [HttpGet("/login")]
        public async Task Login(string? returnUrl)
        {
            var redirect = string.IsNullOrWhiteSpace(returnUrl) ? _frontendBaseUrl : returnUrl;
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirect
            };

            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }

        [HttpGet("/logout")]
        public async Task Logout()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = _frontendBaseUrl
            };

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }

        [HttpGet("/api/auth/me")]
        public IActionResult GetCurrentUser()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return Unauthorized();
            }

            var currentUser = _authenticationService.GetCurrentUser(User);
            return Ok(currentUser);
        }

        //todo: use exception middleware  instea dof try-catch in controller
        [HttpGet("/api/dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new UnauthorizedAccessException("Access token is missing.");
                }

                var result = await _dashboardService.GetDashboardAsync(accessToken, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}

