using Hangfire;
using Microsoft.EntityFrameworkCore;

using PaymentService.Application;
using PaymentService.Infrastructure;

using PaymentService.Infrastructure.Persistence;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext());

// ───── переменные окружения / .env ─────
var connStr = builder.Configuration.GetConnectionString("Db") 
           ?? builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException(
        "Connection string is not configured. " +
        "Please set 'ConnectionStrings:Db' in appsettings.json or environment variable 'ConnectionStrings__Db'");
}

var rabbitMqHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
var availabilityService = builder.Configuration["AvailabilityGrpc:Address"] ?? "http://availability:5001";

// ───── Слои ─────
builder.Services
        .AddApplication()
        .AddInfrastructure(connStr, rabbitMqHost, availabilityService);

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

// ───── применяем миграции ─────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.MapControllers();
app.MapHangfireDashboard("/hangfire");

app.Run();
