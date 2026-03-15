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
        return builder.AddExecutable(
            "res01-initial-setup",
                setupShell,
                "../../../Setup",
                "-NoProfile",
                "-NonInteractive",
                "-File",
                "setup.ps1",
                "-Mode",
            "local",
            "-SkipIfInitialized")
            .WithReference(db.VnmDb)
            .WithEnvironment("CONTAINER_NAME", "vnm-sqlserver")
            .WaitFor(db.SqlServer);
    }
}
