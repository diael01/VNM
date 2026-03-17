using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public static class SetupHostingExtensions
{
    public static IResourceBuilder<ExecutableResource> AddInitialSetup(
        this IDistributedApplicationBuilder builder,
        VnmDatabaseResources db)
    {
        var setupShell = "pwsh";
        if (OperatingSystem.IsWindows())
        {
            var pwshPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PowerShell",
                "7",
                "pwsh.exe");

            setupShell = File.Exists(pwshPath) ? pwshPath : "powershell";
        }

        // Click Start in Aspire dashboard. This script initializes VNM and VNM_TEST.
        var script = OperatingSystem.IsWindows() ? "setup.ps1" : "setupMac.sh";
        var args = OperatingSystem.IsWindows()
            ? new[] { "-NoProfile", "-NonInteractive", "-File", script, "-Mode", "local", "-SkipIfInitialized" }
            : new[] { script, "-Mode", "local" };
        return builder.AddExecutable(
            "res01-initial-setup",
                setupShell,
                "../../Setup",
                args)
            .WithReference(db.VnmDb)
            .WithEnvironment("CONTAINER_NAME", "vnm-sqlserver")
            .WaitFor(db.SqlServer);
    }
}
