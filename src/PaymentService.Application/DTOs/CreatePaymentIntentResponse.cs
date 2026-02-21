using System;

namespace PaymentService.Application.DTOs;

public sealed record CreatePaymentIntentResponse(
    Guid IntentId,
    Guid BookingId,
    long Amount,
    string Currency,
    string Status);
