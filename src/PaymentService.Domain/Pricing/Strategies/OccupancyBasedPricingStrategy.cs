using PaymentService.Domain.Pricing;

namespace PaymentService.Domain.Pricing.Strategies;

/// <summary>
/// Adds 10% per each occupancy percent above 80%.
/// Example heuristic for demo.
/// </summary>
public class OccupancyBasedPricingStrategy : IPricingStrategy
{
    public int Priority => 10;
    public bool IsApplicable(QuoteContext ctx) => ctx.Occupancy > 80;

    public decimal CalculatePrice(QuoteContext ctx)
    {
        var surcharge = 1 + ((ctx.Occupancy - 80) * 0.10m / 100);
        return ctx.BaseRate * surcharge;
    }
}