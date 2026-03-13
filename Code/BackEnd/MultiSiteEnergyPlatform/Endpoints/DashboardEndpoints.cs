using DashboardBff.Services.Dashboard;

namespace DashboardBff.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", async (
            HttpContext httpContext,
            IDashboardService dashboardService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await dashboardService.GetDashboardAsync(httpContext, cancellationToken);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        }).RequireAuthorization();

        return endpoints;
    }
}