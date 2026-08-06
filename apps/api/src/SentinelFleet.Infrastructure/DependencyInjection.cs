using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using SentinelFleet.Application.Assets;
using SentinelFleet.Application.Devices;
using SentinelFleet.Application.Identity;
using SentinelFleet.Application.Organizations;
using SentinelFleet.Application.Security;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Infrastructure.Assets;
using SentinelFleet.Infrastructure.Devices;
using SentinelFleet.Infrastructure.Identity;
using SentinelFleet.Infrastructure.Organizations;
using SentinelFleet.Infrastructure.Persistence;
using SentinelFleet.Infrastructure.Security;
using SentinelFleet.Infrastructure.Telemetry;

namespace SentinelFleet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseConnection = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Connection string 'RabbitMq' is not configured.");

        services.AddDbContext<SentinelFleetDbContext>(options =>
            options.UseNpgsql(
                databaseConnection,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "sentinel")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
        });

        services.AddSingleton(new RabbitMqConnectionSettings(rabbitMqConnection));

        services.AddSingleton<IConnection>(sp =>
        {
            var settings = sp.GetRequiredService<RabbitMqConnectionSettings>();
            var factory = new ConnectionFactory
            {
                Uri = new Uri(settings.ConnectionString)
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<ITelemetryQueuePublisher, TelemetryQueuePublisher>();
        services.AddScoped<ITelemetryIngestService, TelemetryIngestService>();
        services.AddScoped<ITelemetryQueryService, TelemetryQueryService>();
        services.AddScoped<ITelemetryProcessor, TelemetryProcessor>();
        services.AddSingleton<IFleetRealtimePublisher, FleetRealtimePublisher>();
        services.AddHostedService<TelemetryWorker>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured and at least 32 characters.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.AddSingleton<PasswordHasher<User>>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationContext, OrganizationContext>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IDeviceService, DeviceService>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<SentinelFleetDbContext>(name: "postgres")
            .AddRedis(redisConnection, name: "redis")
            .AddRabbitMQ(name: "rabbitmq");

        return services;
    }
}

public sealed record RabbitMqConnectionSettings(string ConnectionString);
