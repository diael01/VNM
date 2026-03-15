# AppHost

Aspire host for local development. Starts all services and the UI together.

## Run

From `BackEnd/Aspire/AppHost`:

```
dotnet run
```

The UI runs in Vite dev mode on port `5173` (configured in `appsettings.json` under `Ui:Ports:Dev`).

## Aspire Dashboard

Once running, open: http://localhost:15000

From there you can see all services, live logs, and resource status.

> stg and prod environments have their own deployment pipelines and dashboards — they are not managed from here.
