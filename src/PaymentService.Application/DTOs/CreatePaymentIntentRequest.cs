using System;

namespace PaymentService.Application.DTOs;

public sealed record CreatePaymentIntentRequest(
    Guid BookingId,
    long Amount,
    string Currency);
