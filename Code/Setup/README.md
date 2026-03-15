# Setup — Local, Stg, Prod

Environment-aware setup and bootstrap guide for VNM infrastructure and database initialization.

## What lives where

- `Setup/setup.ps1`: orchestration script (local/stg/prod modes)
- `Database/*.sql`: SQL artifacts applied by setup
- `Aspire/AppHost`: one caller/orchestrator for local development

## Prerequisites

- .NET 10 SDK
- Docker Desktop running (local mode)
- Node.js 22+ (for UI when running AppHost)
- PowerShell available (`pwsh` preferred; Windows PowerShell `powershell` also works)

## Docker Desktop is required for local mode

For local mode, Docker Desktop must be installed and running.
The setup script and AppHost cannot install Docker Desktop automatically.

Quick check:

```powershell
docker version
```

If Docker is not installed, install Docker Desktop first, then reopen terminal/VS Code.

### Why not auto-install Docker from setup?

Short answer: reliability, permissions, and security.

- Installing Docker requires admin privileges and often a machine restart/logoff.
- Corporate laptops may block package managers or require IT approval.
- Silent auto-install from app startup can be surprising and fragile.

So the current approach is intentional:

- Keep setup deterministic.
- Fail fast with a clear prereq message when Docker is missing.

### Explored options (not enabled yet)

If you want a separate bootstrap step in the future, these are viable options:

1. Windows (winget):

```powershell
winget install -e --id Docker.DockerDesktop
```

2. Windows (Chocolatey):

```powershell
choco install docker-desktop -y
```

3. macOS (Homebrew cask):

```bash
brew install --cask docker
```

4. Linux:

- Install Docker Engine using distro-specific package docs.

Recommendation: keep these as explicit, user-run onboarding commands (or a separate admin bootstrap script), not inside `setup.ps1`.

## Alternative: run without Docker Desktop

If a user does not want Docker Desktop, install services directly on the machine:

- SQL Server (Developer or Express): https://www.microsoft.com/sql-server/sql-server-downloads
- SQL Server Management Studio (SSMS): https://aka.ms/ssmsfullsetup
- Erlang/OTP (required by RabbitMQ): https://www.erlang.org/downloads
- RabbitMQ: https://www.rabbitmq.com/download.html

Important notes for this mode:

- AppHost local orchestration in this repo is currently Docker-based for SQL Server and RabbitMQ.
- Without Docker, run setup script in non-local mode and target your installed SQL Server endpoint.
- RabbitMQ must be started and managed manually (or via service manager) outside Aspire.

## STG/PROD SQL endpoint and connection string

Local and STG/PROD are different:

- Local Docker: host/port is discovered from container port mapping.
- STG/PROD: SQL endpoint is provided by infrastructure (DNS/host + usually port 1433).

How to get server name in STG/PROD:

1. Ask platform/infrastructure for the SQL endpoint (for example `sql-stg.company.local` or managed SQL endpoint).
2. Use that endpoint and port in your connection string.
3. Do not try to infer it from Docker commands unless the environment is actually Docker-based.

Configuration priority (recommended):

1. Environment variable or secret store (recommended):

```text
ConnectionStrings__VnmDb=Server=tcp:sql-stg.company.local,1433;Database=VNM;User ID=app_user;Password=***;Encrypt=True;TrustServerCertificate=False;
```

2. `appsettings.{Environment}.json` (acceptable):

```json
{
	"ConnectionStrings": {
		"VnmDb": "Server=tcp:sql-stg.company.local,1433;Database=VNM;User ID=app_user;Password=***;Encrypt=True;TrustServerCertificate=False;"
	}
}
```

Do you need code changes?

- No code change is needed if the key stays `ConnectionStrings:VnmDb`.
- Code change is needed only if you rename the key (`VnmDb`) to a different name.

## Local flow through AppHost

1. Set SQL password for AppHost parameter:

```powershell
dotnet user-secrets set "Parameters:sql-password" "YourPassword" --project Aspire/AppHost/AppHost.csproj
```

This password is used by AppHost to start the SQL container, but it is not shown as a separate resource row in the Aspire dashboard.

2. Start AppHost:

```powershell
dotnet run --project Aspire/AppHost/AppHost.csproj
```

By default, AppHost auto-opens the UI URL in the browser after the UI is reachable.
You can disable this in `Aspire/AppHost/appsettings.json`:

```json
{
	"AppHost": {
		"AutoOpenUi": false
	}
}
```

3. Open dashboard: `http://localhost:15000`
4. `res00-prereq-check` runs first and verifies Docker prerequisites.
5. `res01-initial-setup` runs after prerequisite check succeeds.

After `res01-initial-setup` succeeds, all services waiting on setup will start.

If Docker is installed but not running, `res00-prereq-check` attempts to start Docker Desktop and waits for daemon readiness.
If Docker is missing (or cannot be started), `res00-prereq-check` fails with a clear error and dependent resources do not start.
If required containers are missing, `res00-prereq-check` now creates them (`vnm-sqlserver` and `vnm-rabbitmq`) before continuing.
For SQL container creation, a password must be available from `Parameters:sql-password` (AppHost secret), or from `APPHOST_SQL_PASSWORD` / `SA_PASSWORD` when running the script directly.

## What res01-initial-setup does

1. Waits for SQL Server readiness
2. If databases are already initialized, exits successfully (no re-run)
3. Otherwise drops/recreates `VNM` from `Database/VNM.sql`
4. Drops/recreates `VNM_TEST` from `Database/VNM_TEST.sql`
5. Seeds both `VNM` and `VNM_TEST` from `Database/Seed.sql`

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
