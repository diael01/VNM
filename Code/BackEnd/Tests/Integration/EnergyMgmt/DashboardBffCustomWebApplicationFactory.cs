extern alias dashboardbff;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.DTOs;
using Services.Redirect;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EnergyManagementWeb.IntegrationTests;

public sealed class DashboardBffCustomWebApplicationFactory : WebApplicationFactory<dashboardbff::Program>
{
    public FakeDashboardTransferWorkflowRedirectService RedirectService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:VnmDb", "Server=localhost,1433;Database=master;User Id=sa;Password=Temp123456!;TrustServerCertificate=True");
        builder.UseSetting("Authentication:Authority", "https://localhost:5001");
        builder.UseSetting("Authentication:ClientId", "test-client");
        builder.UseSetting("Authentication:ClientSecret", "test-secret");
        builder.UseSetting("ServiceEndpoints:InverterApi", "https://localhost:5101/");
        builder.UseSetting("ServiceEndpoints:MeterApi", "https://localhost:5102/");
        builder.UseSetting("RabbitMQ:Password", "guest");
        builder.UseSetting("RabbitMQ:Host", "localhost");
        builder.UseSetting("RabbitMQ:Username", "guest");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://localhost:5001",
                ["Authentication:ClientId"] = "test-client",
                ["Authentication:ClientSecret"] = "test-secret",
                ["ServiceEndpoints:InverterApi"] = "https://localhost:5101/",
                ["ServiceEndpoints:MeterApi"] = "https://localhost:5102/",
                ["ConnectionStrings:VnmDb"] = "Server=localhost,1433;Database=master;User Id=sa;Password=Temp123456!;TrustServerCertificate=True",
                ["RabbitMQ:Password"] = "guest",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IDashboardTransferWorkflowRedirectService>();
            services.AddSingleton(RedirectService);
            services.AddSingleton<IDashboardTransferWorkflowRedirectService>(sp => sp.GetRequiredService<FakeDashboardTransferWorkflowRedirectService>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                options.DefaultScheme = TestAuthHandler.TestScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.TestScheme, _ => { });
        });
    }

    public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string TestScheme = "Test";

        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "integration-user"),
                new Claim("permission", "dashboard:read")
            };

            var identity = new ClaimsIdentity(claims, TestScheme);
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties();
            properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = "integration-access-token" }
            });

            var ticket = new AuthenticationTicket(principal, properties, TestScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

public sealed class FakeDashboardTransferWorkflowRedirectService : IDashboardTransferWorkflowRedirectService
{
    public int? LastExecuteId { get; private set; }
    public string? LastExecuteNote { get; private set; }
    public int? LastSettleId { get; private set; }
    public string? LastSettleNote { get; private set; }

    public Task<List<TransferWorkflowDto>> GetTransferWorkflowsAsync(string accessToken, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransferWorkflowDto>());

    public Task<List<TransferWorkflowStatusHistoryDto>> GetTransferWorkflowHistoryAsync(string accessToken, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransferWorkflowStatusHistoryDto>
        {
            new()
            {
                Id = 1,
                TransferWorkflowId = 123,
                FromStatus = 0,
                ToStatus = 1,
                Note = "Approved from integration test",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = "integration-test"
            }
        });

    public Task<TransferWorkflowDto?> GetTransferWorkflowByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default)
        => Task.FromResult<TransferWorkflowDto?>(null);

    public Task<TransferWorkflowDto> ApproveTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildWorkflow(id, 1, note));

    public Task<TransferWorkflowDto> RejectTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildWorkflow(id, 4, note));

    public Task<TransferWorkflowDto> ExecuteTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        LastExecuteId = id;
        LastExecuteNote = note;
        return Task.FromResult(BuildWorkflow(id, 2, note));
    }

    public Task<TransferWorkflowDto> SettleTransferWorkflowAsync(string accessToken, int id, string? note = null, CancellationToken cancellationToken = default)
    {
        LastSettleId = id;
        LastSettleNote = note;
        return Task.FromResult(BuildWorkflow(id, 3, note));
    }

    private static TransferWorkflowDto BuildWorkflow(int id, int status, string? note)
    {
        return new TransferWorkflowDto
        {
            Id = id,
            EffectiveAtUtc = DateTime.UtcNow,
            BalanceDayUtc = DateTime.UtcNow.Date,
            SourceAddressId = 1,
            DestinationAddressId = 2,
            SourceSurplusKwhAtWorkflow = 10,
            DestinationDeficitKwhAtWorkflow = 8,
            RemainingSourceSurplusKwhAfterWorkflow = 2,
            RemainingDestinationDeficitKwhAfterWorkflow = 0,
            AmountKwh = 8,
            TriggerType = 0,
            Status = status,
            Notes = note,
            CreatedAtUtc = DateTime.UtcNow,
            AppliedDistributionMode = 0
        };
    }
}
