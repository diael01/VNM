# Setup — Local, Stg, Prod
Environment-aware setup and bootstrap guide for VNM infrastructure and database initialization.

## Local flow through AppHost =>

Use this as the default local setup path.
- Requires: .NET 10 SDK, Docker Desktop running, Node.js 22+, PowerShell.
- Fast Docker check: `docker version`
- If Docker is missing: https://www.docker.com/products/docker-desktop/
- If running without Docker: install SQL + Erlang+RabbitMQ manually, then run setup in non-local mode.

Start options:

1. EasyRun (recommended for first-time setup)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup\easyrun.ps1
```

EasyRun prompts for missing secrets, runs prereq-check, and starts AppHost.

2. Aspire directly from VS Code or Visual Studio (developer path)

Set secrets once:

1. Set SQL password for AppHost parameter:
```powershell
dotnet user-secrets set "Parameters:sql-password" "YourPassword" --project Aspire/AppHost/AppHost.csproj
```
This password is used by AppHost to start the SQL container, but it is not shown as a separate resource row in the Aspire dashboard.

2. Set RabbitMQ password for AppHost parameter:
```powershell
dotnet user-secrets set "Parameters:res08-rabbitmq-password" "YourPassword" --project Aspire/AppHost/AppHost.csproj
```
This password is used by prereq setup to create/synchronize the local `vnm-rabbitmq` container credentials (`guest/<your password>`).
You can change it anytime by running the same command with a new value.

Then start AppHost:

3. Start AppHost:
```powershell
dotnet run --project Aspire/AppHost/AppHost.csproj
```
In Visual Studio, set `Aspire/AppHost/AppHost.csproj` as startup project and run.

By default, AppHost auto-opens the UI URL in the browser after the UI is reachable.
You can disable this in `Aspire/AppHost/appsettings.json`:

4. Open dashboard: `http://localhost:15000`
5. `res00-prereq-check` runs first and verifies Docker prerequisites.
6. `res01-initial-setup` runs after prerequisite check succeeds.

See [res01-initial-setup-follow](#res01-initial-setup-follow).


Quick links (collapsed topics):

- [EasyRun](#easyrun)
- [Reset user-secrets for fresh install testing](#reset-user-secrets-for-fresh-install-testing)
- [RabbitMQ password troubleshooting](#rabbitmq-password-troubleshooting)
- [What res01-initial-setup does](#what-res01-initial-setup-does)
- [res01-initial-setup-follow](#res01-initial-setup-follow)
- [Run setup without Aspire](#run-setup-without-aspire)
- [SQL and RabbitMQ images/containers](#sql-and-rabbitmq-imagescontainers)
- [Coverage dashboard](#coverage-dashboard)
- [Logging retention and Application Insights](#logging-retention-and-application-insights)
- [SSMS access after setup](#ssms-access-after-setup)
- [About local SQL password](#about-local-sql-password)
- [User-secrets on Mac](#user-secrets-on-mac)

## EasyRun

EasyRun is the fastest local bootstrap command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup\easyrun.ps1
```

What EasyRun does:

1. Checks local AppHost user-secrets.
2. If an old RabbitMQ secret key exists (`Parameters:rabbitmq-password`) and the new key is missing, EasyRun auto-migrates it to `Parameters:res08-rabbitmq-password`.
3. Prompts for missing values:
	- `Parameters:sql-password`
	- `Parameters:res08-rabbitmq-password`
4. Runs `res00` equivalent prereq checks via `Setup/prereq-check.ps1`.
5. Starts AppHost (`dotnet run --project Aspire/AppHost/AppHost.csproj`).

Useful option:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup\easyrun.ps1 -SkipStart
```

Use `-SkipStart` when you only want prerequisites and secrets set, without launching AppHost.

## Reset user-secrets for fresh install testing

To simulate a first-time local setup, clear AppHost user-secrets:

```powershell
dotnet user-secrets clear --project Aspire/AppHost/AppHost.csproj
```

You can also remove only selected keys:

```powershell
dotnet user-secrets remove "Parameters:sql-password" --project Aspire/AppHost/AppHost.csproj
dotnet user-secrets remove "Parameters:res08-rabbitmq-password" --project Aspire/AppHost/AppHost.csproj
```

Verify current secrets:

```powershell
dotnet user-secrets list --project Aspire/AppHost/AppHost.csproj
```

After reset, run EasyRun again and it will prompt you for missing secrets.

## RabbitMQ password troubleshooting

Symptom:

- `DashboardBFF` / `MeterIngestion` stop with:
	`RabbitMQ password is missing. Set RabbitMQ:Password via user-secrets or environment variable RabbitMQ__Password.`

Quick fix:

1. Stop all running .NET processes.
2. Ensure the canonical AppHost RabbitMQ secret exists:

```powershell
dotnet user-secrets set "Parameters:res08-rabbitmq-password" "YourPassword" --project Aspire/AppHost/AppHost.csproj
```

3. Run EasyRun again (it also migrates legacy `Parameters:rabbitmq-password` automatically):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup\easyrun.ps1
```

4. Verify current secrets if needed:

```powershell
dotnet user-secrets list --project Aspire/AppHost/AppHost.csproj
```

## What res01-initial-setup does

1. Waits for SQL Server readiness
2. If databases are already initialized, exits successfully (no re-run)
3. Otherwise drops/recreates `VNM` from `Database/VNM.sql`
4. Drops/recreates `VNM_TEST` from `Database/VNM_TEST.sql`
5. Seeds both `VNM` and `VNM_TEST` from `Database/Seed.sql`

## res01-initial-setup-follow

After `res01-initial-setup` succeeds, all services waiting on setup will start.

If Docker is installed but not running, `res00-prereq-check` attempts to start Docker Desktop and waits for daemon readiness.
If Docker is missing (or cannot be started), `res00-prereq-check` fails with a clear error and dependent resources do not start.
If required containers are missing, `res00-prereq-check` now creates them (`vnm-sqlserver` and `vnm-rabbitmq`) before continuing.
For SQL container creation, a password must be available from `Parameters:sql-password` (AppHost secret), or from `APPHOST_SQL_PASSWORD` / `SA_PASSWORD` when running the script directly.
For RabbitMQ container creation/sync, a password must be available from `Parameters:res08-rabbitmq-password` (AppHost secret), or from `APPHOST_RABBITMQ_PASSWORD` / `RABBITMQ_DEFAULT_PASS` when running the script directly.


## Run setup without Aspire

Local Docker SQL mode:

```powershell
powershell -NoProfile -NonInteractive -File .\Setup\setup.ps1 -Mode local -ContainerName vnm-sqlserver -SaPassword "YourPassword"
```

Force a full reset even if already initialized:

```powershell
powershell -NoProfile -NonInteractive -File .\Setup\setup.ps1 -Mode local -ContainerName vnm-sqlserver -SaPassword "YourPassword" -ForceRecreate
```

Remote SQL mode (stg/prod style):

```powershell
powershell -NoProfile -NonInteractive -File .\Setup\setup.ps1 -Mode stg -SqlHost "your-sql-host" -SqlPort 1433 -SqlUser "sa" -SaPassword "YourPassword"
```

In `stg/prod` mode the script uses host `sqlcmd` (no `docker exec`).

## SQL and RabbitMQ images/containers

- `res00-prereq-check` ensures required local containers exist and are running.
- Missing containers are created automatically (`vnm-sqlserver` from `mcr.microsoft.com/mssql/server:2022-latest`, `vnm-rabbitmq` from `rabbitmq:3-management`).
- If image is missing, Docker pulls it automatically.
- `res01-initial-setup` initializes databases; it does not create RabbitMQ container.

## Coverage dashboard

Generate combined coverage reports either from Aspire or manually:

- Aspire resource: `res09-testcoverage-dashboard`
- Script: `Setup/coverage-dashboard.ps1`

What it generates:

- Backend coverage report from unit test projects (HTML via ReportGenerator)
- UI coverage report from Vitest (`--coverage`)
- Summary page at `TestCoverage/index.html` linking both reports

How to run:

From Aspire:

1. Start AppHost
2. In Aspire dashboard, run `res09-testcoverage-dashboard`

From terminal:

```powershell
powershell -NoProfile -NonInteractive -File .\Setup\coverage-dashboard.ps1
```

The script auto-opens the dashboard and also prints the exact `file:///...` URI.

Where to open it:

- Local file: `TestCoverage/index.html`
- Absolute example (Windows): `D:\projects\VNM\Code\TestCoverage\index.html`
- URI example: `file:///D:/projects/VNM/Code/TestCoverage/index.html`

From terminal at repo root:

```powershell
start .\TestCoverage\index.html
```

## Logging retention and Application Insights

For net10 web services using `AddServiceDefaults()` (`DashboardBFF`, `MeterIngestion`, `InverterSimulator`):

- Logs are written to rolling files under `Logs/<ApplicationName>/log-<date>.txt`
- File logs are recyclable by default:
	- daily rolling
	- roll on size limit
	- size limit: `10 MB` per file
	- retained files: `3`

Environment behavior for file logs:

- Development: enabled by default
- Stg/Prod: disabled by default (to prefer platform log collection / App Insights)
- Override with `Logging:File:Enabled=true|false`

Optional settings (appsettings or environment variables):

- `Logging:File:RootPath` (default: `Logs`)
- `Logging:File:Enabled` (default: `true` in Development, `false` otherwise)
- `Logging:File:FileSizeLimitBytes` (default: `10485760`)
- `Logging:File:RetainedFileCountLimit` (default: `3`)

Application Insights provision:

- Set `ApplicationInsights:ConnectionString` (or `APPLICATIONINSIGHTS_CONNECTION_STRING`) to enable Serilog trace export to App Insights.
- If not set, App Insights sink stays disabled.

## SSMS access after setup

Connect with:

- Server: `localhost,<mapped-port>`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: the same `Parameters:sql-password` value

How to find `<mapped-port>`:

1. Aspire Dashboard:
	- Open `http://localhost:15000`
	- Open the `res07-sqlserver` resource
	- Check endpoint/port details for SQL Server and use that host port in SSMS

2. Docker Desktop:
	- Open Containers and find `vnm-sqlserver`
	- In the Ports column, read mapping in the form `127.0.0.1:59513 -> 1433`
	- Use `localhost,59513` in SSMS

3. Terminal fallback:

```powershell
docker port vnm-sqlserver 1433
```

If the output is `0.0.0.0:59513` (or `127.0.0.1:59513`), connect with `localhost,59513`.

### SSMS troubleshooting

- If SSMS shows network error 1225 / "connection refused", use `tcp:127.0.0.1,<mapped-port>` instead of `localhost,<mapped-port>`.
- `localhost` can resolve to IPv6 first on some Windows setups, while SQL port forwarding is available on IPv4.
- Quick checks:

```powershell
docker ps --filter "name=vnm-sqlserver" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
docker port vnm-sqlserver 1433
Test-NetConnection -ComputerName 127.0.0.1 -Port <mapped-port>
```

`TcpTestSucceeded` must be `True` before SSMS can connect.

### Why `InverterReadings` may not be empty

`res01-initial-setup` recreates databases, so `InverterReadings` starts empty.
After setup finishes, services start and `MeterIngestion` polling can insert new readings quickly.
So seeing rows there shortly after startup is expected behavior.

## About local SQL password

`Parameters:sql-password` is still the local secret source for AppHost, but it is not added as a separate visible dashboard resource.

- AppHost uses it to start the SQL container.
- `res01-initial-setup` receives `ConnectionStrings__VnmDb` from Aspire at runtime and extracts the SQL credentials from that connection string.
- You can change it via user-secrets.
- Important with persistent containers: changing the secret later does not automatically rotate SQL login inside an already-initialized SQL container.

## User-secrets on Mac

User-secrets works on macOS exactly the same as Windows/Linux.

Example:

```powershell
dotnet user-secrets set "Parameters:sql-password" "YourPassword" --project Aspire/AppHost/AppHost.csproj
```

macOS storage location:

- `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

User-secrets is for local development only.
For stg/prod, inject secrets through your deployment platform (Key Vault, pipeline secrets, Kubernetes secrets, etc.).

To apply a new SA password safely in local dev:

1. Stop AppHost
2. Remove SQL container: `docker rm -f vnm-sqlserver`
3. Set new secret
4. Start AppHost and run `res01-initial-setup` again

(Alternative advanced path: connect and run `ALTER LOGIN sa WITH PASSWORD = 'NewPassword';`.)
