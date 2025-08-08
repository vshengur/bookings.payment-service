using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Handlers;
using PaymentService.Application.Builders;
using PaymentService.Infrastructure.PSPClient;
using PaymentService.Infrastructure.Persistence;
using MassTransit;
using System.Text.Json;

namespace PaymentService.API.Controllers
{
    [ApiController]
    [Route("payment")]
    public class PaymentController : ControllerBase
    {
        private readonly QuoteHandler _quoteHandler;
        private readonly PaymentServiceProviderClient _psp;
        private readonly PaymentDbContext _db;
        private readonly IPublishEndpoint _bus;

        public PaymentController(
            QuoteHandler quoteHandler,
            PaymentServiceProviderClient psp,
            PaymentDbContext db,
            IPublishEndpoint bus)
        {
            _quoteHandler = quoteHandler;
            _psp = psp;
            _db = db;
            _bus = bus;
        }

        [HttpGet("quote")]
        public ActionResult<QuoteResponse> Quote([FromQuery] Guid roomId, [FromQuery] DateOnly checkIn,
                                                 [FromQuery] DateOnly checkOut)
        {
            // demo values for baseRate and occupancy
            var resp = _quoteHandler.Handle(new QuoteRequest(roomId, checkIn, checkOut, 2, 0), 100, 85);
            return Ok(resp);
        }

        [HttpPost("intent")]
        public async Task<IActionResult> CreateIntent([FromBody] Guid bookingId)
        {
            var payload = new PaymentPayloadBuilder()
                          .OrderId(bookingId)
                          .Amount(250, "EUR")
                          .ReturnUrl("https://example.com/return")
                          .Build();
            var json = await _psp.CreateIntentAsync(payload);
            await _bus.Publish(new { BookingId = bookingId, Payload = json }, HttpContext.RequestAborted);
            return Accepted();
        }

        [HttpPost("refund/{bookingId:guid}")]
        public async Task<IActionResult> Refund(Guid bookingId)
        {
            var intent = _db.PaymentIntents.First(i => i.BookingId == bookingId);
            var ok = await _psp.RefundAsync(intent.Id, intent.Amount);
            if (ok)
                await _bus.Publish(new { BookingId = bookingId, Status = "Refunded" });
            return Ok(ok);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromHeader(Name = "X-Signature")] string sig,
                                                 [FromBody] JsonElement body)
        {
            // TODO: validate HMAC signature
            var succeeded = body.GetProperty("status").GetString() == "succeeded";
            var bookingId = Guid.Parse(body.GetProperty("metadata").GetProperty("bookingId").GetString()!);
            await _bus.Publish(new { BookingId = bookingId, Status = succeeded ? "Succeeded" : "Failed" });
            return Ok();
        }
    }
}
