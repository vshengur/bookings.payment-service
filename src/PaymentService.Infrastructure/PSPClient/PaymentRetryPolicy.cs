using Polly;
using Polly.Contrib.WaitAndRetry;

using System;
using System.Net.Http;

namespace PaymentService.Infrastructure.PSPClient
{
    public static class PaymentRetryPolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> Get() =>
            Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(200), retryCount: 3));
    }
}
