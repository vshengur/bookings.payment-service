using PaymentService.Domain.Pricing;

namespace PaymentService.Domain.Pricing.Strategies;

/// <summary>
/// Returns the base rate untouched.
/// </summary>
public class FixedPricingStrategy : IPricingStrategy
{
    public int Priority => 1;
    public bool IsApplicable(QuoteContext _) => true;
    public decimal CalculatePrice(QuoteContext ctx) => ctx.BaseRate;
}