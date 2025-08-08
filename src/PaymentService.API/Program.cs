using Hangfire;
using Hangfire.PostgreSql;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using PaymentService.Application.Handlers;
using PaymentService.Domain.Pricing;
using PaymentService.Domain.Pricing.Strategies;
using PaymentService.Infrastructure.Jobs;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.PSPClient;

using Serilog;

using PaymentRetryPolicy = PaymentService.Infrastructure.PSPClient.PaymentRetryPolicy;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext());

// Db
builder.Services.AddDbContext<PaymentDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Db")));

// Strategies
builder.Services.AddSingleton<IPricingStrategy, FixedPricingStrategy>();
builder.Services.AddSingleton<IPricingStrategy, OccupancyBasedPricingStrategy>();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<QuoteHandler>();

// PSP client
builder.Services.AddHttpClient<PaymentServiceProviderClient>()
       .AddPolicyHandler(PaymentRetryPolicy.Get());

// MassTransit
builder.Services.AddMassTransit(m =>
{
    m.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var connStr = builder.Configuration.GetConnectionString("Default");
// Hangfire
builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(
            cf => cf.UseNpgsqlConnection(connStr),
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

builder.Services.AddTransient<NightlyTariffJob>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.MapHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<NightlyTariffJob>("nightly-tariffs", job => job.ExecuteAsync(), Cron.Daily);

app.Run();
