using Hangfire;
using Hangfire.PostgreSql;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using PaymentService.Application.Configuration;
using PaymentService.Application.Occupancy;
using PaymentService.Infrastructure.Occupancy;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.PSPClient;

using System;

using Availability = AvailabilityService.Contracts.Grpc.V1.AvailabilityService;
using PaymentRetryPolicy = PaymentService.Infrastructure.PSPClient.PaymentRetryPolicy;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        RabbitMqSettings rabbitMq,
        string availabilityService)
    {
        services.AddDbContext<PaymentDbContext>(o => o
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services
            .AddHttpClient<PaymentServiceProviderClient>()
            .AddPolicyHandler(PaymentRetryPolicy.Get());

        services.AddGrpcClient<Availability.AvailabilityServiceClient>(o =>
        {
            o.Address = new Uri(availabilityService);
        });

        services.AddSingleton<IOccupancyService>(sp =>
        {
            var client = sp.GetRequiredService<Availability.AvailabilityServiceClient>();
            var core = new OccupancyAdapter(client);
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedOccupancyService>>();
            return new CachedOccupancyService(core, cache, logger, TimeSpan.FromMinutes(5));
        });

        services.AddMassTransit(m =>
        {
            m.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, h =>
                {
                    h.Username(rabbitMq.Username);
                    h.Password(rabbitMq.Password);
                });
            });
        });

        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    cf => cf.UseNpgsqlConnection(connectionString),
                    options: new PostgreSqlStorageOptions
                    {
                        QueuePollInterval = TimeSpan.FromSeconds(3),
                        PrepareSchemaIfNecessary = true,
                        SchemaName = "Hangfire"
                    })
                .WithJobExpirationTimeout(TimeSpan.FromHours(1000));
        }).AddHangfireServer(option =>
        {
            option.SchedulePollingInterval = TimeSpan.FromSeconds(1);
        });

        return services;
    }
}
