using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AppHost.Extensions;

public static class PrereqHostingExtensions
{
    public static IResourceBuilder<ExecutableResource> AddPrereqCheck(this IDistributedApplicationBuilder builder)
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

        return builder.AddExecutable(
            "res00-prereq-check",
            shell,
            "../../../Setup",
            "-NoProfile",
            "-NonInteractive",
            "-File",
            "prereq-check.ps1",
            "-RequireDocker");
    }
}
