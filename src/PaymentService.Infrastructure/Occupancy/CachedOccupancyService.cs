using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using PaymentService.Application.Occupancy;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Occupancy;

public class CachedOccupancyService : IOccupancyService
{
    private readonly IOccupancyService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedOccupancyService> _log;
    private readonly TimeSpan _ttl;

    public CachedOccupancyService(
        IOccupancyService inner,
        IMemoryCache cache,
        ILogger<CachedOccupancyService> log,
        TimeSpan? ttl = null)
    {
        _inner = inner;
        _cache = cache;
        _log = log;
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
    }

    public async Task<int> GetOccupancyAsync(Guid roomId, DateOnly date, CancellationToken ct = default)
    {
        var key = $"occ:{roomId}:{date:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out int cached))
            return cached;

        var value = await _inner.GetOccupancyAsync(roomId, date, ct);
        _cache.Set(key, value, _ttl);
        _log.LogDebug("Cached occupancy {Value}% for {RoomId} {Date}", value, roomId, date);
        return value;
    }
}