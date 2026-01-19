using PaymentService.Application.Occupancy;

using System;
using System.Threading;
using System.Threading.Tasks;

using Availability = AvailabilityService.Contracts.Grpc.V1.AvailabilityService;

namespace PaymentService.Infrastructure.Occupancy;

public class OccupancyAdapter : IOccupancyService
{
    private readonly Availability.AvailabilityServiceClient _client;

    public OccupancyAdapter(Availability.AvailabilityServiceClient client) { _client = client; }

    public async Task<int> GetOccupancyAsync(Guid roomId, DateOnly date, CancellationToken ct = default)
    {
        var req = new AvailabilityService.Contracts.Grpc.V1.GetOccupancyRequest { RoomId = roomId.ToString(), Date = date.ToString("yyyy-MM-dd") };
        var res = await _client.GetOccupancyAsync(req, cancellationToken: ct);
        return res.OccupancyPercent;
    }
}
