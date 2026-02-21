using System;
using System.Linq;

using FluentValidation;

using Microsoft.Extensions.Options;

using PaymentService.Application.Configuration;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Validators;

public sealed class CreatePaymentIntentRequestValidator : AbstractValidator<CreatePaymentIntentRequest>
{
    public CreatePaymentIntentRequestValidator(IOptions<PaymentSettings> paymentOptions)
    {
        var allowed = paymentOptions.Value.AllowedCurrencies;

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("BookingId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(c => allowed.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage(x => $"Currency '{x.Currency}' is not supported. Allowed: {string.Join(", ", allowed)}.");
    }
}
