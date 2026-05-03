using AdInsights.Api.Endpoints;
using AdInsights.Api.Middleware;
using AdInsights.Application.Common.Behaviors;
using AdInsights.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using System.Text;

// ─── Bootstrap Serilog Early ───────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AdInsights API...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog Structured Logging ────────────────────────────────────────
    builder.Host.UseSerilog((ctx, loggerConfig) =>
        loggerConfig
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "AdInsights.Api")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // ─── OpenTelemetry Distributed Tracing ─────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AdInsights.Api"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddJaegerExporter(options =>
                {
                    options.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
                    options.AgentPort = builder.Configuration.GetValue("Jaeger:Port", 6831);
                });
        });

    // ─── Infrastructure (Cassandra, Redis, ClickHouse, DI registrations) ───
    builder.Services.AddInfrastructure(builder.Configuration);

    // ─── MediatR + Pipeline Behaviors ──────────────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<GetAdClicksQueryPlaceholder>();
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // ─── FluentValidation ──────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssemblyContaining<GetAdClicksQueryPlaceholder>();

    // ─── JWT Authentication ─────────────────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                        ?? throw new InvalidOperationException("JWT key not configured.")))
            };
        });

    builder.Services.AddAuthorization();

    // ─── Rate Limiting ──────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();

    // ─── Swagger / OpenAPI ──────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "AdInsights API",
            Version = "v1",
            Description = "Real-time ad analytics API for retail advertising platforms."
        });
    });

    // ─── Middleware Registration ─────────────────────────────────────────────
    builder.Services.AddTransient<GlobalExceptionHandlerMiddleware>();
    builder.Services.AddTransient<TenantResolutionMiddleware>();

    // ─── Health Checks ───────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ─── Request Pipeline ────────────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "AdInsights API v1");
            options.RoutePrefix = string.Empty;
        });
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    app.UseRouting();
    app.UseHttpMetrics();          // Prometheus metrics middleware
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<TenantResolutionMiddleware>();

    // ─── Endpoint Mapping ────────────────────────────────────────────────────
    app.MapAdMetricsEndpoints();
    app.MapHealthChecks("/healthz");
    app.MapMetrics("/metrics");     // Prometheus scrape endpoint

    Log.Information("AdInsights API started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Type alias used for MediatR assembly scanning (avoids referencing internal handler directly)
internal abstract class GetAdClicksQueryPlaceholder { }
