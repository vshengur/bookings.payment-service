using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using PaymentService.Application.Builders;

namespace PaymentService.Infrastructure.PSPClient
{
    public class PaymentServiceProviderClient
    {
        private readonly HttpClient _httpClient;

        public PaymentServiceProviderClient(HttpClient http)
        {
            _httpClient = http;
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", "demo-key");
        }

        public async Task<string> CreateIntentAsync(string payloadJson, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync("/intents", JsonDocument.Parse(payloadJson), ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync(ct);
            return json;
        }

        public async Task<bool> RefundAsync(Guid intentId, decimal amount, CancellationToken ct = default)
        {
            var res = await _httpClient.PostAsJsonAsync($"/intents/{intentId}/refund", new { amount }, ct);
            return res.IsSuccessStatusCode;
        }
    }
}
