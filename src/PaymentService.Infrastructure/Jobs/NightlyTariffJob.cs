using Dapper;
using Hangfire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

using System;
using System.Threading.Tasks;

namespace PaymentService.Infrastructure.Jobs
{
    public class NightlyTariffJob
    {
        private readonly string _connString;
        private readonly ILogger<NightlyTariffJob> _logger;

        public NightlyTariffJob(IConfiguration config, ILogger<NightlyTariffJob> logger)
        {
            _connString = config.GetConnectionString("Db")!;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteAsync()
        {
            var start = DateTime.UtcNow;
            await using var conn = new NpgsqlConnection(_connString);
            const string sql = @"""UPDATE price_list_items
                                   SET amount = amount * 1.02
                                   WHERE date BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '30 day'"""; // demo
            var rows = await conn.ExecuteAsync(sql);
            _logger.LogInformation("NightlyTariffJob updated {Rows} rows in {Elapsed}ms", rows, (DateTime.UtcNow - start).TotalMilliseconds);
            // TODO: publish PriceListUpdated via MassTransit
        }
    }
}
