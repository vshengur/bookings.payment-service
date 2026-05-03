using System.Collections.Concurrent;
using System.Net;
using System.Text;

using MassTransit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Moq;

using PaymentService.API.Controllers;
using PaymentService.Application.Configuration;
using PaymentService.Application.Messages;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.PSPClient;

namespace Integration.Tests;

public sealed class PaymentApiWebApplicationFactory : WebApplicationFactory<PaymentController>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public Mock<IPublishEndpoint> PublishEndpointMock { get; } = new();
    public ConcurrentQueue<object> PublishedMessages { get; } = new();
    public MutablePspMessageHandler PspHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Db"] = "Host=localhost;Database=ignored;Username=ignored;Password=ignored",
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["Psp:ApiKey"] = "test-key",
                ["Psp:BaseUrl"] = "https://psp.test",
                ["Payment:ReturnUrl"] = "https://example.test/return",
                ["Payment:CancelUrl"] = "https://example.test/cancel",
                ["Payment:AllowedCurrencies:0"] = "EUR",
                ["Payment:AllowedCurrencies:1"] = "USD",
                ["Payment:AllowedCurrencies:2"] = "GBP"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            _connection.Open();

            // Remove Npgsql EF provider registrations added by production DI.
            var npgsqlDescriptors = services
                .Where(d =>
                    (d.ServiceType.Assembly.FullName?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.ImplementationType?.Assembly.FullName?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.ImplementationInstance?.GetType().Assembly.FullName?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            foreach (var descriptor in npgsqlDescriptors)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<PaymentDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<PaymentDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<PaymentDbContext>>>();
            services.RemoveAll<IPostConfigureOptions<DbContextOptions<PaymentDbContext>>>();

            // Some AddDbContext overloads register extra DbContextOptions configurators.
            var dbOptionsDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GenericTypeArguments.Any(a => a == typeof(PaymentDbContext)) &&
                    d.ServiceType.FullName?.Contains("DbContextOptions", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            foreach (var descriptor in dbOptionsDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<PaymentDbContext>(o => o.UseSqlite(_connection));

            var hostedDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .Where(d => d.ImplementationType is not null
                            && (d.ImplementationType.FullName?.Contains("Hangfire", StringComparison.OrdinalIgnoreCase) == true
                                || d.ImplementationType.FullName?.Contains("MassTransit", StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

            foreach (var descriptor in hostedDescriptors)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<IPublishEndpoint>();
            services.AddSingleton(PublishEndpointMock.Object);

            PublishEndpointMock.Reset();
            PublishEndpointMock
                .Setup(x => x.Publish(It.IsAny<PaymentIntentCreated>(), It.IsAny<CancellationToken>()))
                .Callback<PaymentIntentCreated, CancellationToken>((msg, _) => PublishedMessages.Enqueue(msg))
                .Returns(Task.CompletedTask);

            services.RemoveAll<PaymentServiceProviderClient>();
            services.AddSingleton(PspHandler);
            services.AddSingleton(sp =>
            {
                var http = new HttpClient(sp.GetRequiredService<MutablePspMessageHandler>())
                {
                    BaseAddress = new Uri("https://psp.test")
                };

                return new PaymentServiceProviderClient(
                    http,
                    Options.Create(new PspSettings
                    {
                        ApiKey = "test-key",
                        BaseUrl = "https://psp.test"
                    }));
            });

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public void ResetHarness()
    {
        PspHandler.Reset();
        PublishedMessages.Clear();
        PublishEndpointMock.Invocations.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public sealed class MutablePspMessageHandler : HttpMessageHandler
{
    public bool ThrowOnCreateIntent { get; set; }
    public int CreateIntentCalls { get; private set; }
    public string ResponseJson { get; set; } = """{"id":"psp_intent_test_1","status":"created"}""";

    public void Reset()
    {
        ThrowOnCreateIntent = false;
        CreateIntentCalls = 0;
        ResponseJson = """{"id":"psp_intent_test_1","status":"created"}""";
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/intents")
        {
            CreateIntentCalls++;

            if (ThrowOnCreateIntent)
            {
                throw new HttpRequestException("PSP is unavailable.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
