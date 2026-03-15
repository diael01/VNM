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
            "../../Setup",
            "-NoProfile",
            "-NonInteractive",
            "-File",
            "prereq-check.ps1",
            "-RequireDocker",
            "-EnsureContainer",
            "vnm-sqlserver,vnm-rabbitmq")
            .WithEnvironment(context =>
            {
                var sqlPassword = builder.Configuration["Parameters:sql-password"]
                    ?? builder.Configuration["SA_PASSWORD"];

                if (!string.IsNullOrWhiteSpace(sqlPassword))
                {
                    context.EnvironmentVariables["APPHOST_SQL_PASSWORD"] = sqlPassword;
                }
            });
    }
}
