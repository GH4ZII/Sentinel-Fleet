using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using SentinelFleet.Infrastructure.Persistence;

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
            options.UseNpgsql(databaseConnection));

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

        services
            .AddHealthChecks()
            .AddDbContextCheck<SentinelFleetDbContext>(name: "postgres")
            .AddRedis(redisConnection, name: "redis")
            .AddRabbitMQ(name: "rabbitmq");

        return services;
    }
}

public sealed record RabbitMqConnectionSettings(string ConnectionString);
