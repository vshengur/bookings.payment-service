using System.Text.Json;

using FluentAssertions;

using Bookings.Common.ValueObjects;

using PaymentService.Application.Builders;

namespace Domain.Tests.Builders;

[TestFixture]
public class PaymentPayloadBuilderTests
{
    [Test]
    public void Build_ShouldContainAllFields()
    {
        var bookingId = Guid.NewGuid();
        var money = new Money(250.00m, "EUR");
        var returnUrl = "https://example.com/return";

        var json = new PaymentPayloadBuilder()
            .OrderId(bookingId)
            .Amount(money)
            .ReturnUrl(returnUrl)
            .Build();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("orderId").GetGuid().Should().Be(bookingId);
        root.GetProperty("amount").GetDecimal().Should().Be(250.00m);
        root.GetProperty("currency").GetString().Should().Be("EUR");
        root.GetProperty("returnUrl").GetString().Should().Be(returnUrl);
    }

    [Test]
    public void Build_WithDifferentCurrency_ShouldSerializeCorrectly()
    {
        var money = new Money(99.99m, "USD");

        var json = new PaymentPayloadBuilder()
            .OrderId(Guid.NewGuid())
            .Amount(money)
            .ReturnUrl("https://test.com")
            .Build();

        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("amount").GetDecimal().Should().Be(99.99m);
        doc.RootElement.GetProperty("currency").GetString().Should().Be("USD");
    }

    [Test]
    public void Build_ShouldProduceValidJson()
    {
        var json = new PaymentPayloadBuilder()
            .OrderId(Guid.NewGuid())
            .Amount(new Money(100m, "GBP"))
            .ReturnUrl("https://example.com")
            .Build();

        var act = () => JsonDocument.Parse(json);

        act.Should().NotThrow();
    }
}
