using System;

namespace PaymentService.Application.DTOs
{
    public record QuoteRequest(Guid RoomId, DateOnly CheckIn, DateOnly CheckOut, int Adults, int Children);
}
