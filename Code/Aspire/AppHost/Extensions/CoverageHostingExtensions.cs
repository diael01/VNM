using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public static class CoverageHostingExtensions
{
    public static IResourceBuilder<ExecutableResource> AddCoverageDashboard(this IDistributedApplicationBuilder builder)
    {
        var shell = "pwsh";
        if (OperatingSystem.IsWindows())
        {
            var pwshPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PowerShell",
                "7",
                "pwsh.exe");

            shell = File.Exists(pwshPath) ? pwshPath : "powershell";
        }

        // Utility resource: start manually from Aspire to generate/open TestCoverage reports.
            var script = OperatingSystem.IsWindows() ? "coverage-dashboard.ps1" : "coverage-dashboardMac.sh";
            var args = OperatingSystem.IsWindows()
                ? new[] { "-NoProfile", "-NonInteractive", "-File", script }
                : new[] { script };
            return builder.AddExecutable(
                "res09-testcoverage-dashboard",
                shell,
                "../../Setup",
                args);
    }
}
