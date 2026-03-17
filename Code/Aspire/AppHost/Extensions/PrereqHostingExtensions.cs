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

        var script = OperatingSystem.IsWindows() ? "prereq-check.ps1" : "prereqCheckMac.sh";
        var args = OperatingSystem.IsWindows()
            ? new[] { "-NoProfile", "-NonInteractive", "-File", script, "-RequireDocker", "-EnsureContainer", "vnm-sqlserver,vnm-rabbitmq" }
            : new[] { script };
        return builder.AddExecutable(
            "res00-prereq-check",
            shell,
            "../../Setup",
            args)
            .WithEnvironment(context =>
            {
                var sqlPassword = builder.Configuration["Parameters:sql-password"]
                    ?? builder.Configuration["SA_PASSWORD"];
                var rabbitPassword = builder.Configuration["Parameters:res08-rabbitmq-password"]
                    ?? builder.Configuration["RABBITMQ_DEFAULT_PASS"];

                if (!string.IsNullOrWhiteSpace(sqlPassword))
                {
                    context.EnvironmentVariables["APPHOST_SQL_PASSWORD"] = sqlPassword;
                }

                if (!string.IsNullOrWhiteSpace(rabbitPassword))
                {
                    context.EnvironmentVariables["APPHOST_RABBITMQ_PASSWORD"] = rabbitPassword;
                }
            });
    }
}
