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
using VNM.Infrastructure.Auth;
using IAuthenticationService = Services.Auth.IAuthenticationService;

namespace DashboardBFF.Controllers
{
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly IUserPermissionResolver _permissionResolver;
        private readonly IDashboardRedirectService _dashboardService;
        private readonly string _frontendBaseUrl;

        public DashboardController(
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory,
            IAuthenticationService authService,
            IUserPermissionResolver permissionResolver,
            IDashboardRedirectService dashboardService,
            IOptions<FrontendOptions> frontendOptions)
        {
            _publishEndpoint = publishEndpoint;
            _httpClientFactory = httpClientFactory;
            _authenticationService = authService;
            _permissionResolver = permissionResolver;
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
            return Ok(new { Service = "DashboardBFF", Status = "Running" });
        }

        [AllowAnonymous]
        [HttpGet("/api/system/ready")]
        public async Task<IActionResult> GetBackendReadiness(CancellationToken cancellationToken)
        {
            var meterReady = await ProbeServiceAsync("meter-api", cancellationToken);
            var inverterReady = await ProbeServiceAsync("inverter-api", cancellationToken);
            var ready = meterReady && inverterReady;

            return Ok(new
            {
                ready,
                meterReady,
                inverterReady
            });
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
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return Unauthorized();
            }

            var currentUser = _authenticationService.GetCurrentUser(User);

            // Ensure local role sync runs and permission claims are reflected in the response.
            var permissions = await _permissionResolver.GetPermissionsAsync(User, cancellationToken);
            var existingPermissionValues = new HashSet<string>(
                currentUser.Claims
                    .Where(c => string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (existingPermissionValues.Add(permission))
                {
                    currentUser.Claims =
                    [
                        .. currentUser.Claims,
                        new Models.Auth.ClaimDto { Type = "permission", Value = permission }
                    ];
                }
            }

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

        private async Task<bool> ProbeServiceAsync(string clientName, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(clientName);
                using var response = await client.GetAsync("health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

