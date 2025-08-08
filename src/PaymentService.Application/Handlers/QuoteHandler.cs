using Microsoft.Extensions.Caching.Memory;

using PaymentService.Application.DTOs;
using PaymentService.Domain.Pricing;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PaymentService.Application.Handlers
{
    public class QuoteHandler
    {
        private readonly IReadOnlyList<IPricingStrategy> _strategies;
        private readonly IMemoryCache _cache;

        public QuoteHandler(IReadOnlyList<IPricingStrategy> strategies, IMemoryCache cache)
        {
            _strategies = strategies;
            _cache = cache;
        }

        public QuoteResponse Handle(QuoteRequest req, decimal baseRate, int occupancy)
        {
            var key = $"{req.RoomId}-{req.CheckIn}-{req.CheckOut}";
            if (_cache.TryGetValue(key, out QuoteResponse cached))
                return cached;

            var ctx = new QuoteContext(req.RoomId, req.CheckIn, req.CheckOut,
                                       req.Adults, req.Children, baseRate, occupancy);

            var strat = _strategies
                        .Where(s => s.IsApplicable(ctx))
                        .OrderByDescending(s => s.Priority)
                        .FirstOrDefault() ?? throw new InvalidOperationException(
                            "Нет подходящей стратегии ценообразования");

            var amount = strat.CalculatePrice(ctx);
            var resp = new QuoteResponse(amount, "EUR", strat.GetType().Name);
            _cache.Set(key, resp, TimeSpan.FromHours(1));
            return resp;
        }
    }
}
