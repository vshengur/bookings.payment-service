using FluentAssertions;
using FluentValidation.TestHelper;

using Microsoft.Extensions.Options;

using PaymentService.Application.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Validators;

namespace Domain.Tests.Validators;

[TestFixture]
public class CreatePaymentIntentRequestValidatorTests
{
    private CreatePaymentIntentRequestValidator _sut = null!;

    [SetUp]
    public void Setup()
    {
        var options = Options.Create(new PaymentSettings
        {
            AllowedCurrencies = ["EUR", "USD", "GBP"],
            ReturnUrl = "https://example.com/return",
            CancelUrl = "https://example.com/cancel"
        });
        _sut = new CreatePaymentIntentRequestValidator(options);
    }

    [Test]
    public async Task ValidRequest_ShouldHaveNoErrors()
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), 25000, "EUR");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task EmptyBookingId_ShouldFail()
    {
        var request = new CreatePaymentIntentRequest(Guid.Empty, 25000, "EUR");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.BookingId);
    }

    [Test]
    public async Task ZeroAmount_ShouldFail()
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), 0, "EUR");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public async Task NegativeAmount_ShouldFail()
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), -100, "EUR");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public async Task EmptyCurrency_ShouldFail()
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), 25000, "");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Test]
    public async Task UnsupportedCurrency_ShouldFail()
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), 25000, "XYZ");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [TestCase("eur")]
    [TestCase("Eur")]
    [TestCase("EUR")]
    public async Task CurrencyIsCaseInsensitive(string currency)
    {
        var request = new CreatePaymentIntentRequest(Guid.NewGuid(), 25000, currency);

        var result = await _sut.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Test]
    public async Task MultipleViolations_ShouldReportAll()
    {
        var request = new CreatePaymentIntentRequest(Guid.Empty, -1, "");

        var result = await _sut.TestValidateAsync(request);

        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
