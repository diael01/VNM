

## Features

- **.NET 10** ASP.NET Core Web API
- **OpenTelemetry** integration for traces, metrics, and logs
- **Serilog** with OpenTelemetry sink for structured logging
- **.NET Aspire** for local development orchestration with built-in observability dashboard
- **Scalar** API documentation UI for easy API testing and exploration
- All telemetry (logs, traces, metrics) automatically routed to Aspire Dashboard


## Project Structure

```
Code/
├── Aspire/
│   ├── AppHost/           # .NET Aspire local orchestrator and dashboard entry point
│   └── ServiceDefaults/   # Shared service defaults used by backend services
├── BackEnd/               # Deployable backend services and libraries
├── ReactUI/               # Frontend application
├── Database/              # Database scripts and assets
└── Setup/                 # Local setup and prerequisite scripts
```

### Deployment Boundary

- Deploy staging and production from the service projects under `BackEnd/`, the frontend under `ReactUI/`, and the required database artifacts.
- Do not deploy `Aspire/AppHost` as a runtime service to staging or production. It is the local orchestration entry point for development.
- `Aspire/ServiceDefaults` remains a shared library and is included indirectly through the backend service builds.

## Quick Start

### 1. Run the Application with .NET Aspire

```bash
cd ../Aspire/AppHost
dotnet run
```

This will:

- Start the Aspire Dashboard (automatically opens in browser)
- Launch the API project
- Configure all telemetry to flow to the Aspire Dashboard
- The API will be available at the port shown in the Aspire Dashboard

**Note**: When you see the error about missing environment variables, the launch settings file will configure them automatically on the next run.

### 2. Generate Telemetry Data

Use the provided `requests.http` file with VS Code REST Client extension or any HTTP client:

```bash
# Process with metrics
curl http://localhost:5000/Metrics/process?complexity=2
```

### 3. Access the API Documentation

Navigate to `/scalar/v1` on your API endpoint (check the Aspire Dashboard for the port) to access the Scalar API documentation UI. For example:

- `http://localhost:5089/scalar/v1`

Scalar provides:

- Interactive API documentation
- Built-in API testing capabilities
- Code generation for multiple languages
- Beautiful, modern UI

### 4. View Telemetry in Aspire Dashboard

The Aspire Dashboard automatically opens when you run the application. You can view:

- **Structured Logs**: All Serilog logs with trace correlation
- **Distributed Traces**: Full request traces with spans
- **Metrics**: Real-time metrics for your application
- **Resources**: Status of your running services

## Key Components

### OpenTelemetry Configuration

The `ServiceDefaults` project contains centralized OpenTelemetry configuration:

- **Traces**: ASP.NET Core and HTTP client instrumentation
- **Metrics**: Runtime, process, and custom metrics
- **Logs**: Serilog integration with OpenTelemetry sink

### Custom Instrumentation

The API includes examples of:

- Custom ActivitySource for distributed tracing
- Custom Meters for business metrics
- Structured logging with trace correlation
- Error handling with proper telemetry

### Endpoints

#### Metrics Demo

- `GET /Metrics/process` - Process with custom metrics
- `GET /Metrics/batch` - Batch processing example
- `GET /Metrics/status` - Application status
#### System

- `GET /health` - Health check
- `GET /scalar/v1` - Interactive API documentation

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development/Production)
- Aspire automatically configures `OTEL_EXPORTER_OTLP_ENDPOINT` when running

### Serilog Configuration

Serilog is configured to:

- Write to console with structured output
- Send logs to OpenTelemetry collector via OTLP
- Include trace and span IDs for correlation
- Enrich logs with environment and thread information

## Troubleshooting

### No data in Aspire Dashboard?

1. Ensure the application is running through the `Aspire/AppHost` project
2. Check the console output for any errors
3. Verify you're making requests to generate telemetry
4. The dashboard should automatically receive all telemetry data

## Advanced Usage

### Adding Custom Metrics

```csharp
var meter = new Meter("YourApp", "1.0.0");
var counter = meter.CreateCounter<long>("custom_metric");
counter.Add(1, new("tag", "value"));
```

### Adding Custom Traces

```csharp
using var activity = ActivitySource.StartActivity("CustomOperation");
activity?.SetTag("custom.tag", "value");
```
### Benefits of Using Aspire Dashboard

- **Zero Configuration**: Aspire automatically configures OTLP endpoints
- **Integrated Experience**: See logs, traces, and metrics in one place
- **Development Focused**: Optimized for local development scenarios
- **Real-time Updates**: Live streaming of telemetry data
- **Correlation**: Easy navigation between related logs and traces

## License

This is a demo project for educational purposes.

