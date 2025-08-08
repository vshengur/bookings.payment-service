using NUnit.Framework;
using PaymentService.Domain.Strategies;

namespace Domain.Tests
{
    public class PricingStrategyTests
    {
        [TestCase(100, 50, 100)]
        [TestCase(100, 85, 110)]
        public void OccupancyStrategy_Works(decimal baseRate, int occupancy, decimal expected)
        {
            var strat = new OccupancyBasedPricingStrategy();
            var price = strat.CalculatePrice(baseRate, 2, 0, occupancy);
            Assert.That(price, Is.EqualTo(expected));
        }

        [Test]
        public void FixedStrategy_ReturnsBase()
        {
            var strat = new FixedPricingStrategy();
            Assert.That(strat.CalculatePrice(150, 2, 0, 50), Is.EqualTo(150));
        }
    }
}
