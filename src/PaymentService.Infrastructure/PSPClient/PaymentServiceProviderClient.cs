using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using PaymentService.Application.Configuration;

namespace PaymentService.Infrastructure.PSPClient
{
    public class PaymentServiceProviderClient
    {
        private readonly HttpClient _httpClient;

        public PaymentServiceProviderClient(HttpClient http, IOptions<PspSettings> pspOptions)
        {
            _httpClient = http;

            var settings = pspOptions.Value;
            if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
                _httpClient.BaseAddress = new Uri(settings.BaseUrl);

            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
        }

        public async Task<string> CreateIntentAsync(string payloadJson, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync("/intents", JsonDocument.Parse(payloadJson), ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync(ct);
        }

        public async Task<bool> RefundAsync(Guid intentId, long amount, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync($"/intents/{intentId}/refund", new { amount }, ct);
            return res.IsSuccessStatusCode;
        }
    }
}
