using FluentValidation;

using MassTransit;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using PaymentService.Application.Builders;
using PaymentService.Application.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Messages;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.PSPClient;

using System.Text.Json;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("payment")]
public class PaymentController : ControllerBase
{
    private readonly PaymentServiceProviderClient _psp;
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly PaymentSettings _paymentSettings;
    private readonly IValidator<CreatePaymentIntentRequest> _validator;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        PaymentServiceProviderClient psp,
        PaymentDbContext db,
        IPublishEndpoint bus,
        IOptions<PaymentSettings> paymentOptions,
        IValidator<CreatePaymentIntentRequest> validator,
        ILogger<PaymentController> logger)
    {
        _psp = psp;
        _db = db;
        _bus = bus;
        _paymentSettings = paymentOptions.Value;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost("intent")]
    public async Task<IActionResult> CreateIntent(
        [FromBody] CreatePaymentIntentRequest request,
        CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var existing = await _db.PaymentIntents
            .FirstOrDefaultAsync(i => i.BookingId == request.BookingId, ct);

        if (existing is not null)
        {
            return Ok(new CreatePaymentIntentResponse(
                existing.Id, existing.BookingId, existing.Amount,
                existing.Currency, existing.Status));
        }

        var money = new Bookings.Common.ValueObjects.Money(
            request.Amount / 100m, request.Currency);

        var payload = new PaymentPayloadBuilder()
            .OrderId(request.BookingId)
            .Amount(money)
            .ReturnUrl(_paymentSettings.ReturnUrl)
            .Build();

        var pspJson = await _psp.CreateIntentAsync(payload, ct);

        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            BookingId = request.BookingId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PaymentIntent {IntentId} created for booking {BookingId}, amount {Amount} {Currency}",
            intent.Id, intent.BookingId, intent.Amount, intent.Currency);

        await _bus.Publish(new PaymentIntentCreated(
            intent.BookingId, intent.Id, intent.Amount,
            intent.Currency, pspJson), ct);

        return Accepted(new CreatePaymentIntentResponse(
            intent.Id, intent.BookingId, intent.Amount,
            intent.Currency, intent.Status));
    }

    [HttpPost("refund/{bookingId:guid}")]
    public async Task<IActionResult> Refund(Guid bookingId, CancellationToken ct)
    {
        var intent = await _db.PaymentIntents
            .FirstOrDefaultAsync(i => i.BookingId == bookingId, ct);

        if (intent is null)
            return NotFound();

        var ok = await _psp.RefundAsync(intent.Id, intent.Amount, ct);
        if (ok)
        {
            intent.Status = "refunded";
            await _db.SaveChangesAsync(ct);

            await _bus.Publish(new PaymentStatusChanged(bookingId, "Refunded"), ct);
        }

        return Ok(ok);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromHeader(Name = "X-Signature")] string sig,
        [FromBody] JsonElement body,
        CancellationToken ct)
    {
        // TODO(PAY-002): validate HMAC signature
        var succeeded = body.GetProperty("status").GetString() == "succeeded";
        var bookingId = Guid.Parse(body.GetProperty("metadata").GetProperty("bookingId").GetString()!);

        var status = succeeded ? "Succeeded" : "Failed";
        await _bus.Publish(new PaymentStatusChanged(bookingId, status), ct);

        return Ok();
    }
}
