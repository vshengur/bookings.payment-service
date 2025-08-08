using Microsoft.EntityFrameworkCore;

using System;

namespace PaymentService.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> opts) : base(opts) { }

        public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
        public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    }

    public class PriceListItem
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public DateOnly Date { get; set; }
        public Decimal Amount { get; set; }
    }

    public class PaymentIntent
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Status { get; set; } = "pending";
    }
}
