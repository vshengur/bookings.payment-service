using System;

namespace PaymentService.Application.Messages;

public sealed record PaymentStatusChanged(
    Guid BookingId,
    string Status);
