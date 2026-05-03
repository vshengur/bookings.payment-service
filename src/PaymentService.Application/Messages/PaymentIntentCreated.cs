using System;

namespace PaymentService.Application.Messages;

public sealed record PaymentIntentCreated(
    Guid BookingId,
    Guid IntentId,
    long Amount,
    string Currency,
    string PspPayload);
