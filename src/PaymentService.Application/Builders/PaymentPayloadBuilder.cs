using System;
using System.Collections.Generic;
using System.Text.Json;
using Bookings.Common.ValueObjects;

namespace PaymentService.Application.Builders
{
    public class PaymentPayloadBuilder
    {
        private readonly Dictionary<string, object?> _payload = [];

        public PaymentPayloadBuilder Amount(Money money)
        {
            _payload["amount"] = money.Amount;
            _payload["currency"] = money.Currency;
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
