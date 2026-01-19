using System;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentService.Application.Occupancy;

public interface IOccupancyService
{
    Task<int> GetOccupancyAsync(Guid roomId, DateOnly date, CancellationToken ct = default);
}
