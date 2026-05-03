using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using PaymentService.Application.DTOs;
using PaymentService.Application.Messages;
using PaymentService.Infrastructure.Persistence;

using NUnit.Framework;

namespace Integration.Tests;

[TestFixture]
public sealed class PaymentIntentApiTests
{
    private PaymentApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new PaymentApiWebApplicationFactory();
        _client = _factory.CreateClient();
        _factory.ResetHarness();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CreateIntent_HappyPath_ShouldReturnAccepted_AndPublishEvent()
    {
        var bookingId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest(bookingId, 25999, "eur");

        var response = await _client.PostAsJsonAsync("/payment/intent", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        var body = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        Assert.That(body, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(body!.BookingId, Is.EqualTo(bookingId));
            Assert.That(body.Amount, Is.EqualTo(25999));
            Assert.That(body.Currency, Is.EqualTo("EUR"));
            Assert.That(body.Status, Is.EqualTo("created"));
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var saved = await db.PaymentIntents.SingleAsync(x => x.BookingId == bookingId);
        Assert.Multiple(() =>
        {
            Assert.That(saved.Amount, Is.EqualTo(25999));
            Assert.That(saved.Currency, Is.EqualTo("EUR"));
            Assert.That(saved.Status, Is.EqualTo("created"));
            Assert.That(saved.ProviderRef, Is.EqualTo("psp_intent_test_1"));
        });

        Assert.That(_factory.PspHandler.CreateIntentCalls, Is.EqualTo(1));
        Assert.That(_factory.PublishedMessages.Count, Is.EqualTo(1));
        Assert.That(_factory.PublishedMessages.Single(), Is.TypeOf<PaymentIntentCreated>());
    }

    [Test]
    public async Task CreateIntent_SameBookingIdTwice_ShouldReturnExistingIntentWith200()
    {
        var bookingId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest(bookingId, 12000, "EUR");

        var first = await _client.PostAsJsonAsync("/payment/intent", request);
        var second = await _client.PostAsJsonAsync("/payment/intent", request);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        var firstBody = await first.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        Assert.That(firstBody, Is.Not.Null);
        Assert.That(secondBody, Is.Not.Null);
        Assert.That(secondBody!.IntentId, Is.EqualTo(firstBody!.IntentId));
        Assert.That(_factory.PspHandler.CreateIntentCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateIntent_InvalidPayload_ShouldReturnBadRequest()
    {
        var request = new CreatePaymentIntentRequest(Guid.Empty, 0, "XXX");

        var response = await _client.PostAsJsonAsync("/payment/intent", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateIntent_WhenPspUnavailable_ShouldReturn503_AndMarkIntentFailed()
    {
        _factory.PspHandler.ThrowOnCreateIntent = true;
        var bookingId = Guid.NewGuid();
        var request = new CreatePaymentIntentRequest(bookingId, 3400, "EUR");

        var response = await _client.PostAsJsonAsync("/payment/intent", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        var retryAfter = json.RootElement.TryGetProperty("retryAfterSeconds", out var camel)
            ? camel.GetInt32()
            : json.RootElement.GetProperty("RetryAfterSeconds").GetInt32();
        Assert.That(retryAfter, Is.EqualTo(5));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var saved = await db.PaymentIntents.SingleAsync(x => x.BookingId == bookingId);
        Assert.That(saved.Status, Is.EqualTo("failed"));
        Assert.That(_factory.PublishedMessages.Count, Is.EqualTo(0));
    }
}
