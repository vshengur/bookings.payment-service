using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PaymentService.Application.Builders
{
    public class PaymentPayloadBuilder
    {
        private readonly Dictionary<string, object?> _payload = new();

        public PaymentPayloadBuilder Amount(decimal amount, string currency)
        {
            _payload["amount"] = amount;
            _payload["currency"] = currency;
            return this;
        }

        public PaymentPayloadBuilder OrderId(Guid bookingId)
        {
            _payload["orderId"] = bookingId;
            return this;
        }

        public PaymentPayloadBuilder ReturnUrl(string url)
        {
            _payload["returnUrl"] = url;
            return this;
        }

        public string Build() => JsonSerializer.Serialize(_payload);
    }
}
