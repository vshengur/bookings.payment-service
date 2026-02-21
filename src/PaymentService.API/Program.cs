using Hangfire;

using Microsoft.EntityFrameworkCore;

using PaymentService.Application;
using PaymentService.Application.Configuration;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext());

var connStr = builder.Configuration.GetConnectionString("Db")
           ?? builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException(
        "Connection string is not configured. " +
        "Please set 'ConnectionStrings:Db' in appsettings.json or environment variable 'ConnectionStrings__Db'");
}

var availabilityService = builder.Configuration["AvailabilityGrpc:Address"] ?? "http://availability:5001";

builder.Services.Configure<PaymentSettings>(
    builder.Configuration.GetSection(PaymentSettings.SectionName));

builder.Services.Configure<PspSettings>(
    builder.Configuration.GetSection(PspSettings.SectionName));

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

var rabbitMq = builder.Configuration
    .GetSection(RabbitMqSettings.SectionName)
    .Get<RabbitMqSettings>() ?? new RabbitMqSettings();

builder.Services
    .AddApplication()
    .AddInfrastructure(connStr, rabbitMq, availabilityService);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.MapControllers();
app.MapHangfireDashboard("/hangfire");

app.Run();
