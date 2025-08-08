namespace PaymentService.Application.DTOs
{
    public record QuoteResponse(decimal Amount, string Currency, string StrategyUsed);
}
