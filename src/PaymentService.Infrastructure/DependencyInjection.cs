using Hangfire;
using Hangfire.PostgreSql;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        string rabbitMqHost,
        string availabilityService)
    {
        // Db
        services.AddDbContext<PaymentDbContext>(o => o
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        // PSP client
        services
            .AddHttpClient<PaymentServiceProviderClient>()            
            .AddPolicyHandler(PaymentRetryPolicy.Get());

        // gRPC client to availability-service
        services.AddGrpcClient<Availability.AvailabilityServiceClient>(o =>
        {
            o.Address = new Uri(availabilityService);
        });

        // IOccupancyService with caching decorator
        services.AddSingleton<IOccupancyService>(sp =>
        {
            var client = sp.GetRequiredService<Availability.AvailabilityServiceClient>();
            var core = new OccupancyAdapter(client);
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedOccupancyService>>();
            return new CachedOccupancyService(core, cache, logger, TimeSpan.FromMinutes(5));
        });

        //services.AddGrpc<PaymentV1.PaymentService>(o =>
        //{
        //    o.Address = new Uri();
        //});

        // MassTransit
        services.AddMassTransit(m =>
        {
            m.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
            });
        });

        // Hangfire
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
