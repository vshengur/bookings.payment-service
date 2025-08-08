using System;

namespace PaymentService.Domain.Pricing;

public record QuoteContext(
    Guid RoomId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    decimal BaseRate,
    int Occupancy);